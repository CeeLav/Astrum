using System;
using System.Collections.Generic;
using System.Linq;
using Astrum.CommonBase;
using Astrum.Generated;
using Astrum.LogicCore.FrameSync;
using Astrum.LogicCore.Managers;
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
        public List<World> Worlds { get; private set; } = new List<World>(1);

        public World MainWorld
        {
            get
            {
                return Worlds[0];
            }
            set
            {
                if (Worlds.Count < 1)
                {
                    Worlds.Add(value);
                    return;
                }
                Worlds[0] = value;
            }
        } // 默认返回第一个世界，如果没有则返回空

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
        }

        public Room(int roomId, string name) : this()
        {
            RoomId = roomId;
            Name = name;
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
            var player = MainWorld.CreateEntity(1001); // 创建玩家实体，EntityConfigId=1001 (需要根据实际配置表确定)
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
            LSController.SaveState();
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
                    pairs.Value.PlayerId = playerId;
                    EventSystem.Instance.Publish(newPlayerEventData);
                }
            }
            
            // 更新所有世界
            foreach (var world in Worlds)
            {
                world.Update();
            }
            
            TickManagers();
        }
        
        public void TickManagers()
        {
            SkillEffectManager.Instance.Update();
        }

        /// <summary>
        /// 初始化房间
        /// </summary>
        public virtual void Initialize()
        {
            TotalTime = 0f;
            if (MainWorld == null)
            {
                ASLogger.Instance.Error($"Room {RoomId} has no MainWorld defined.");
            }
            MainWorld?.Initialize(0);
            // 初始化帧同步控制器
            if (LSController == null)
            {
                LSController = new LSController
                {
                    Room = this
                };
            }

            // 初始化所有世界
            foreach (var world in Worlds)
            {
                world?.Initialize(0);
            }
        }

        /// <summary>
        /// 应用服务器下发的创建时间（服务器本地时间）
        /// </summary>
        /// <param name="serverCreationTime">服务器时间戳（毫秒）</param>
        public void SetServerCreationTime(long serverCreationTime)
        {
            CreationTime = serverCreationTime;
            if (LSController != null)
            {
                LSController.CreationTime = serverCreationTime;
            }
        }
        
    }
}
