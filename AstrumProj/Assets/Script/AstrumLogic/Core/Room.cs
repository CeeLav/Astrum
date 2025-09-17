using System;
using System.Collections.Generic;
using System.Linq;
using Astrum.CommonBase;
using Astrum.Generated;
using Astrum.LogicCore.FrameSync;
using UnityEngine.EventSystems;
using EventSystem = Astrum.CommonBase.EventSystem;

namespace Astrum.LogicCore.Core
{
    /// <summary>
    /// 房间类，管理游戏房间和世界
    /// </summary>
    public class Room
    {
        /// <summary>
        /// 房间ID
        /// </summary>
        public int RoomId { get; set; }

        /// <summary>
        /// 房间名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 房间是否激活
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 管理的世界列表
        /// </summary>
        public List<World> Worlds { get; private set; } = new List<World>();
        public World MainWorld => Worlds.FirstOrDefault(); // 默认返回第一个世界，如果没有则返回一个新的空世界

        /// <summary>
        /// 玩家列表
        /// </summary>
        public List<long> Players { get; private set; } = new List<long>();

        /// <summary>
        /// 最大玩家数
        /// </summary>
        public int MaxPlayers { get; set; } = 8;

        /// <summary>
        /// 帧同步控制器
        /// </summary>
        public LSController? LSController { get; set; }

        /// <summary>
        /// 房间创建时间
        /// </summary>
        public long CreationTime { get; private set; }

        /// <summary>
        /// 房间总运行时间
        /// </summary>
        public float TotalTime { get; private set; } = 0f;

        public long MainPlayerId { get; set; } = -1;

        public Room()
        {
            CreationTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public Room(int roomId, string name) : this()
        {
            RoomId = roomId;
            Name = name;
        }

        /// <summary>
        /// 添加世界
        /// </summary>
        /// <param name="world">世界对象</param>
        public void AddWorld(World world)
        {
            if (world != null && !Worlds.Contains(world))
            {
                Worlds.Add(world);
                world.Initialize();
            }
        }

        /// <summary>
        /// 移除世界
        /// </summary>
        /// <param name="world">世界对象</param>
        public void RemoveWorld(World world)
        {
            if (world != null && Worlds.Contains(world))
            {
                world.Cleanup();
                Worlds.Remove(world);
            }
        }

        /// <summary>
        /// 添加玩家
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>是否添加成功</returns>
        public long AddPlayer()
        {
            if (Players.Count >= MaxPlayers)
            {
                return -1;
            }
            var player = MainWorld.CreateEntity<Unit>("Player"); // 创建玩家实体
            Players.Add(player.UniqueId);
            return player.UniqueId;
        }

        /// <summary>
        /// 移除玩家
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>是否移除成功</returns>
        public bool RemovePlayer(int playerId)
        {
            return Players.Remove(playerId);
        }

        /// <summary>
        /// 更新房间
        /// </summary>
        /// <param name="deltaTime">时间差</param>
        public void Update(float deltaTime)
        {
            if (!IsActive) return;

            TotalTime += deltaTime;

            // 更新帧同步控制器
            LSController?.Tick();
            
        }
        
        public void FrameTick(OneFrameInputs oneFrameInputs)
        {
            foreach (var pairs in oneFrameInputs.Inputs)
            {
                var input = pairs.Value;
                var entity = MainWorld.GetEntity(pairs.Key);
                if (entity != null)
                {
                    // 更新实体输入组件
                    var inputComponent = entity.GetComponent<LSInputComponent>();
                    if (inputComponent != null)
                    {
                        inputComponent.SetInput(input);
                    }
                }
                else if (pairs.Value.BornInfo != 0)
                {
                    ASLogger.Instance.Info($"新玩家创建 BornInfo: {pairs.Value.BornInfo} ");
                    
                    var playerId = AddPlayer();
                    var newPlayerEventData = new NewPlayerEventData
                    (
                        playerId,pairs.Value.BornInfo
                    );
                    pairs.Value.BornInfo = 0;
                    EventSystem.Instance.Publish(newPlayerEventData);
                }
            }
            
            // 更新所有世界
            foreach (var world in Worlds)
            {
                world.Update();
            }
            
        }

        /// <summary>
        /// 初始化房间
        /// </summary>
        public virtual void Initialize()
        {
            CreationTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            TotalTime = 0f;

            // 初始化帧同步控制器
            if (LSController == null)
            {
                LSController = new LSController
                {
                    Room = this
                };
                LSController.CreationTime = CreationTime;
                LSController.Start();
            }

            // 初始化所有世界
            foreach (var world in Worlds)
            {
                world.Initialize();
            }
        }

        /// <summary>
        /// 清理房间
        /// </summary>
        public virtual void Cleanup()
        {
            IsActive = false;

            // 停止帧同步控制器
            LSController?.Stop();

            // 清理所有世界
            foreach (var world in Worlds)
            {
                world.Cleanup();
            }

            // 清空数据
            Worlds.Clear();
            Players.Clear();
        }

        /// <summary>
        /// 根据ID获取世界
        /// </summary>
        /// <param name="worldId">世界ID</param>
        /// <returns>世界对象，如果不存在返回null</returns>
        public World? GetWorldById(int worldId)
        {
            return Worlds.FirstOrDefault(w => w.WorldId == worldId);
        }

        /// <summary>
        /// 获取玩家数量
        /// </summary>
        /// <returns>当前玩家数量</returns>
        public int GetPlayerCount()
        {
            return Players.Count;
        }

        /// <summary>
        /// 检查玩家是否在房间中
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>是否在房间中</returns>
        public bool HasPlayer(int playerId)
        {
            return Players.Contains(playerId);
        }

        /// <summary>
        /// 检查房间是否已满
        /// </summary>
        /// <returns>是否已满</returns>
        public bool IsFull()
        {
            return Players.Count >= MaxPlayers;
        }

        /// <summary>
        /// 检查房间是否为空
        /// </summary>
        /// <returns>是否为空</returns>
        public bool IsEmpty()
        {
            return Players.Count == 0;
        }

        /// <summary>
        /// 获取所有实体
        /// </summary>
        /// <returns>所有实体列表</returns>
        public List<Entity> GetAllEntities()
        {
            var entities = new List<Entity>();
            
            foreach (var world in Worlds)
            {
                entities.AddRange(world.Entities.Values);
            }

            return entities;
        }

        /// <summary>
        /// 根据玩家ID获取实体
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>玩家实体列表</returns>
        public List<Entity> GetPlayerEntities(int playerId)
        {
            var entities = new List<Entity>();

            foreach (var world in Worlds)
            {
                var playerEntities = world.Entities.Values
                    .Where(e => e.GetComponent<LSInputComponent>()?.PlayerId == playerId)
                    .ToList();
                
                entities.AddRange(playerEntities);
            }

            return entities;
        }
    }
}
