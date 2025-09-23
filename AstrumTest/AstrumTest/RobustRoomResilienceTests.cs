using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Astrum.CommonBase;
using Astrum.Generated;
using AstrumServer.Network;
using AstrumServer.Managers;
using Astrum.Network;

namespace AstrumTest
{
    /// <summary>
    /// 单进程健壮性测试：大量创建/加入房间以及异常断开后，房间应被正确清理
    /// </summary>
    public class RobustRoomResilienceTests : IDisposable
    {
        private class TestServerHarness
        {
            public readonly InMemoryServerNetwork Network;
            public readonly UserManager Users;
            public readonly RoomManager Rooms;
            // 不在此用例中使用帧同步

            public TestServerHarness()
            {
                ASLogger.Instance.MinLevel = LogLevel.Error;
                Network = new InMemoryServerNetwork();
                Users = new UserManager();
                Rooms = new RoomManager();
                // 帧同步相关在健壮性验证中不强依赖，先不初始化以降低耦合

                // 事件绑定（复用 GameServer 的处理语义的最小子集）
                Network.OnClientConnected += client =>
                {
                    var resp = ConnectResponse.Create();
                    resp.success = true;
                    resp.message = "连接成功";
                    resp.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    // 仅模拟流程，不需要真正回发
                };

                Network.OnClientDisconnected += client =>
                {
                    var user = Users.GetUserBySessionId(client.Id.ToString());
                    if (user != null && !string.IsNullOrEmpty(user.CurrentRoomId))
                    {
                        var roomId = user.CurrentRoomId;
                        Rooms.LeaveRoom(roomId, user.Id);
                        Users.UpdateUserRoom(user.Id, "");
                        Rooms.CleanupEmptyRooms();
                    }
                    Users.RemoveUser(client.Id.ToString());
                };

                Network.OnMessageReceived += (client, message) =>
                {
                    switch (message)
                    {
                        case LoginRequest login:
                        {
                            Users.AssignUserId(client.Id.ToString(), login.DisplayName);
                            break;
                        }
                        case CreateRoomRequest create:
                        {
                            var user = Users.GetUserBySessionId(client.Id.ToString());
                            if (user == null) break;
                            if (!string.IsNullOrEmpty(user.CurrentRoomId))
                            {
                                Rooms.LeaveRoom(user.CurrentRoomId, user.Id);
                                Users.UpdateUserRoom(user.Id, "");
                            }
                            var room = Rooms.CreateRoom(user.Id, create.RoomName, create.MaxPlayers);
                            if (room != null)
                            {
                                Users.UpdateUserRoom(user.Id, room.Id);
                            }
                            break;
                        }
                        case JoinRoomRequest join:
                        {
                            var user = Users.GetUserBySessionId(client.Id.ToString());
                            if (user == null) break;
                            if (!string.IsNullOrEmpty(user.CurrentRoomId))
                            {
                                Rooms.LeaveRoom(user.CurrentRoomId, user.Id);
                                Users.UpdateUserRoom(user.Id, "");
                            }
                            if (Rooms.JoinRoom(join.RoomId, user.Id))
                            {
                                Users.UpdateUserRoom(user.Id, join.RoomId);
                            }
                            break;
                        }
                        case LeaveRoomRequest leave:
                        {
                            var user = Users.GetUserBySessionId(client.Id.ToString());
                            if (user == null) break;
                            if (Rooms.LeaveRoom(leave.RoomId, user.Id))
                            {
                                Users.UpdateUserRoom(user.Id, "");
                            }
                            break;
                        }
                    }
                };
            }

            public void Cleanup()
            {
                Rooms.CleanupEmptyRooms();
                Users.CleanupDisconnectedUsers();
            }
        }

        private readonly TestServerHarness _h;

        public RobustRoomResilienceTests()
        {
            _h = new TestServerHarness();
        }

        [Fact]
        public async Task MassiveCreateJoin_ThenAbruptDisconnect_ShouldCleanupRooms()
        {
            const int clientCount = 50;
            const int roomSize = 4;

            // 连接并登录
            var sessionIds = new List<long>();
            for (int i = 0; i < clientCount; i++)
            {
                var sid = _h.Network.SimulateConnect();
                sessionIds.Add(sid);
                var login = LoginRequest.Create();
                login.DisplayName = $"U_{i}";
                _h.Network.SimulateReceive(sid, login);
            }

            // 前半创建房间，后半加入房间
            var createdRoomIds = new List<string>();
            for (int i = 0; i < clientCount / 2; i++)
            {
                var sid = sessionIds[i];
                var req = CreateRoomRequest.Create();
                req.RoomName = $"R_{i}";
                req.MaxPlayers = roomSize;
                req.Timestamp = TimeInfo.Instance.ClientNow();
                _h.Network.SimulateReceive(sid, req);
                var user = _h.Users.GetUserBySessionId(sid.ToString());
                if (user != null && !string.IsNullOrEmpty(user.CurrentRoomId))
                {
                    createdRoomIds.Add(user.CurrentRoomId);
                }
            }

            // 让后半用户加入轮询的房间
            for (int i = clientCount / 2; i < clientCount; i++)
            {
                var targetRoom = createdRoomIds[(i - clientCount / 2) % createdRoomIds.Count];
                var sid = sessionIds[i];
                var join = JoinRoomRequest.Create();
                join.RoomId = targetRoom;
                join.Timestamp = TimeInfo.Instance.ClientNow();
                _h.Network.SimulateReceive(sid, join);
            }

            // 随机一半异常断开
            for (int i = 0; i < clientCount; i += 2)
            {
                _h.Network.SimulateDisconnect(sessionIds[i], abrupt: true);
            }

            // 清理一次
            _h.Cleanup();

            // 断言：不存在空房间（实现应在玩家离开到0时删除）
            var stats = _h.Rooms.GetRoomStatistics();
            Assert.True(stats.emptyRooms == 0, $"存在空房间: {stats.emptyRooms}");

            // 尝试强制所有人离开并再次清理后，房间数应为0
            foreach (var sid in sessionIds)
            {
                var user = _h.Users.GetUserBySessionId(sid.ToString());
                if (user != null && !string.IsNullOrEmpty(user.CurrentRoomId))
                {
                    var leave = LeaveRoomRequest.Create();
                    leave.RoomId = user.CurrentRoomId;
                    leave.Timestamp = TimeInfo.Instance.ClientNow();
                    _h.Network.SimulateReceive(sid, leave);
                }
            }

            _h.Cleanup();
            var stats2 = _h.Rooms.GetRoomStatistics();
            Assert.True(stats2.totalRooms == 0, $"房间未清空: {stats2.totalRooms}");
        }

        public void Dispose()
        {
        }
    }
}


