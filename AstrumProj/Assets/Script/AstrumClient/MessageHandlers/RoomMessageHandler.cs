using System.Threading.Tasks;
using Astrum.CommonBase;
using Astrum.Generated;
using Astrum.Network;
using Astrum.Client.Managers;

namespace Astrum.Client.MessageHandlers
{
    /// <summary>
    /// 创建房间响应消息处理器
    /// </summary>
    [MessageHandler(typeof(CreateRoomResponse))]
    public class CreateRoomResponseHandler : MessageHandlerBase<CreateRoomResponse>
    {
        public override async Task HandleMessageAsync(CreateRoomResponse message)
        {
            try
            {
                ASLogger.Instance.Info($"CreateRoomResponseHandler: 处理创建房间响应 - Success: {message.Success}, Message: {message.Message}");
                
                if (message.Success)
                {
                    // 直接调用RoomSystemManager处理创建房间响应
                    RoomSystemManager.Instance?.HandleCreateRoomResponse(message);
                    ASLogger.Instance.Info("CreateRoomResponseHandler: 创建房间成功，已通知RoomSystemManager");
                }
                else
                {
                    ASLogger.Instance.Error($"CreateRoomResponseHandler: 创建房间失败 - {message.Message}");
                }
                
                await Task.CompletedTask;
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"CreateRoomResponseHandler: 处理创建房间响应时发生异常 - {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 加入房间响应消息处理器
    /// </summary>
    [MessageHandler(typeof(JoinRoomResponse))]
    public class JoinRoomResponseHandler : MessageHandlerBase<JoinRoomResponse>
    {
        public override async Task HandleMessageAsync(JoinRoomResponse message)
        {
            try
            {
                ASLogger.Instance.Info($"JoinRoomResponseHandler: 处理加入房间响应 - Success: {message.Success}, Message: {message.Message}");
                
                if (message.Success)
                {
                    // 直接调用RoomSystemManager处理加入房间响应
                    RoomSystemManager.Instance?.HandleJoinRoomResponse(message);
                    ASLogger.Instance.Info("JoinRoomResponseHandler: 加入房间成功，已通知RoomSystemManager");
                }
                else
                {
                    ASLogger.Instance.Error($"JoinRoomResponseHandler: 加入房间失败 - {message.Message}");
                }
                
                await Task.CompletedTask;
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"JoinRoomResponseHandler: 处理加入房间响应时发生异常 - {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 离开房间响应消息处理器
    /// </summary>
    [MessageHandler(typeof(LeaveRoomResponse))]
    public class LeaveRoomResponseHandler : MessageHandlerBase<LeaveRoomResponse>
    {
        public override async Task HandleMessageAsync(LeaveRoomResponse message)
        {
            try
            {
                ASLogger.Instance.Info($"LeaveRoomResponseHandler: 处理离开房间响应 - Success: {message.Success}, Message: {message.Message}");
                
                if (message.Success)
                {
                    // 直接调用RoomSystemManager处理离开房间响应
                    RoomSystemManager.Instance?.HandleLeaveRoomResponse(message);
                    ASLogger.Instance.Info("LeaveRoomResponseHandler: 离开房间成功，已通知RoomSystemManager");
                }
                else
                {
                    ASLogger.Instance.Error($"LeaveRoomResponseHandler: 离开房间失败 - {message.Message}");
                }
                
                await Task.CompletedTask;
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"LeaveRoomResponseHandler: 处理离开房间响应时发生异常 - {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 获取房间列表响应消息处理器
    /// </summary>
    [MessageHandler(typeof(GetRoomListResponse))]
    public class GetRoomListResponseHandler : MessageHandlerBase<GetRoomListResponse>
    {
        public override async Task HandleMessageAsync(GetRoomListResponse message)
        {
            try
            {
                ASLogger.Instance.Info($"GetRoomListResponseHandler: 处理获取房间列表响应 - Success: {message.Success}, Message: {message.Message}");
                
                if (message.Success)
                {
                    // 直接调用RoomSystemManager处理获取房间列表响应
                    RoomSystemManager.Instance?.HandleRoomListResponse(message);
                    ASLogger.Instance.Info("GetRoomListResponseHandler: 获取房间列表成功，已通知RoomSystemManager");
                }
                else
                {
                    ASLogger.Instance.Error($"GetRoomListResponseHandler: 获取房间列表失败 - {message.Message}");
                }
                
                await Task.CompletedTask;
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"GetRoomListResponseHandler: 处理获取房间列表响应时发生异常 - {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 房间更新通知消息处理器
    /// </summary>
    [MessageHandler(typeof(RoomUpdateNotification))]
    public class RoomUpdateNotificationHandler : MessageHandlerBase<RoomUpdateNotification>
    {
        public override async Task HandleMessageAsync(RoomUpdateNotification message)
        {
            try
            {
                ASLogger.Instance.Info($"RoomUpdateNotificationHandler: 处理房间更新通知");
                
                // 直接调用RoomSystemManager处理房间更新通知
                RoomSystemManager.Instance?.HandleRoomUpdateNotification(message);
                ASLogger.Instance.Info("RoomUpdateNotificationHandler: 房间更新通知已通知RoomSystemManager");
                
                await Task.CompletedTask;
            }
            catch (System.Exception ex)
            {
                ASLogger.Instance.Error($"RoomUpdateNotificationHandler: 处理房间更新通知时发生异常 - {ex.Message}");
            }
        }
    }
}
