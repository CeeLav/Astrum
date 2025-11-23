using System;
using System.Collections.Generic;
using System.Linq;
using Astrum.CommonBase;
using Astrum.Generated;
using Astrum.LogicCore.FrameSync;
using Astrum.LogicCore.Managers;
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
                if (Worlds == null || Worlds.Count == 0)
                {
                    return null;
                }
                return Worlds[0];
            }
            set
            {
                if (Worlds == null)
                {
                    Worlds = new List<World>(1);
                }
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
        /// 怪物列表
        /// </summary>
        public List<long> Monsters { get; private set; } = new List<long>();

        /// <summary>
        /// 帧同步控制器（基础接口，客户端和服务器通用）
        /// </summary>
        public ILSControllerBase? LSController { get; set; }

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
            var player = MainWorld.CreateEntity(1003); // 创建玩家实体，EntityConfigId=1001 (需要根据实际配置表确定)
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
        /// 添加怪物
        /// </summary>
        /// <param name="monsterConfigId">怪物配置ID（从EntityBaseTable中读取）</param>
        /// <returns>怪物实体ID，失败返回-1</returns>
        public long AddMonster(int monsterConfigId)
        {
            if (MainWorld == null)
            {
                ASLogger.Instance.Error($"Room.AddMonster: MainWorld不存在");
                return -1;
            }

            // 创建怪物实体
            var monster = MainWorld.CreateEntity(monsterConfigId);
            if (monster == null)
            {
                ASLogger.Instance.Error($"Room.AddMonster: 创建怪物失败，ConfigId={monsterConfigId}");
                return -1;
            }

            // 添加到怪物列表
            Monsters.Add(monster.UniqueId);
            ASLogger.Instance.Info($"Room.AddMonster: 怪物创建成功，EntityId={monster.UniqueId}, ConfigId={monsterConfigId}");
                
            return monster.UniqueId;
        }

        /// <summary>
        /// 移除怪物
        /// </summary>
        /// <param name="monsterId">怪物实体ID</param>
        /// <returns>是否移除成功</returns>
        public bool RemoveMonster(long monsterId)
        {
            if (Monsters.Remove(monsterId))
            {
                // 从世界中销毁实体
                var monster = MainWorld?.GetEntity(monsterId);
                if (monster != null)
                {
                    MainWorld.DestroyEntity(monsterId);
                    ASLogger.Instance.Info($"Room.RemoveMonster: 怪物已移除，EntityId={monsterId}");
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取怪物实体
        /// </summary>
        /// <param name="monsterId">怪物实体ID</param>
        /// <returns>怪物实体，不存在返回null</returns>
        public Entity? GetMonster(long monsterId)
        {
            if (!Monsters.Contains(monsterId))
            {
                return null;
            }
            return MainWorld?.GetEntity(monsterId);
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
            // 保存状态（SaveState 内部会处理 FrameBuffer 的准备和帧号选择）
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
                else 
                {
                    ASLogger.Instance.Info($"Room.FrameTick: 未找到实体，EntityId={pairs.Key}");
                }
            }
            
            // 更新所有世界
            foreach (var world in Worlds)
            {
                world.Update();
            }
            
            // 更新所有世界的 SkillEffectSystem
            TickSystems();
        }
        
        /// <summary>
        /// 已同步的实体ID集合（用于跟踪哪些实体已经发布过创建事件）
        /// </summary>
        private HashSet<long> _syncedEntityIds = new HashSet<long>();
        

        
        /// <summary>
        /// 更新所有世界的系统
        /// </summary>
        public void TickSystems()
        {
            foreach (var world in Worlds)
            {
                world.SkillEffectSystem?.Update();
            }
        }

        /// <summary>
        /// 初始化房间
        /// </summary>
        /// <param name="controllerType">LSController 类型（"client" 或 "server"），默认为 "client"</param>
        /// <param name="worldSnapshot">World 快照数据（可选），如果提供则反序列化并替换 MainWorld</param>
        public virtual void Initialize(string controllerType = "client", byte[] worldSnapshot = null)
        {
            TotalTime = 0f;
            
            // 如果提供了 World 快照，反序列化并替换 MainWorld
            if (worldSnapshot != null && worldSnapshot.Length > 0)
            {
                LoadWorldFromSnapshot(worldSnapshot);
            }
            else
            {
                MainWorld = new World();
                MainWorld.Initialize(0);
                MainWorld.RoomId = RoomId;
            }
            
            if (MainWorld == null)
            {
                ASLogger.Instance.Error($"Room {RoomId} has no MainWorld defined.");
            }
            
            // 根据类型创建对应的 LSController
            if (LSController == null)
            {
                if (controllerType == "server")
                {
                    LSController = new ServerLSController { Room = this };
                }
                else
                {
                    LSController = new ClientLSController { Room = this };
                }
            }
        }
        
        /// <summary>
        /// 从快照数据加载 World（参考 Rollback 逻辑）
        /// </summary>
        /// <param name="worldSnapshot">World 快照数据</param>
        /// <returns>反序列化后的 World，失败返回 null</returns>
        public World LoadWorldFromSnapshot(byte[] worldSnapshot)
        {
            if (worldSnapshot == null || worldSnapshot.Length == 0)
            {
                ASLogger.Instance.Warning($"World 快照数据为空，无法加载", "Room.LoadWorld");
                return null;
            }
            
            // 记录接收到的数据信息
            ASLogger.Instance.Info($"LoadWorldFromSnapshot: 开始反序列化，数据大小: {worldSnapshot.Length} bytes", "Room.LoadWorld");
            
            // 验证数据有效性（MemoryPack 格式检查）
            if (worldSnapshot.Length < 4)
            {
                ASLogger.Instance.Error($"LoadWorldFromSnapshot: 数据太小，无法包含有效的 MemoryPack 数据 (大小: {worldSnapshot.Length})", "Room.LoadWorld");
                return null;
            }
            
            // 打印前几个字节用于调试
            int previewBytes = Math.Min(16, worldSnapshot.Length);
            string preview = string.Join(" ", worldSnapshot.Take(previewBytes).Select(b => b.ToString("X2")));
            ASLogger.Instance.Debug($"LoadWorldFromSnapshot: 数据前 {previewBytes} 字节: {preview}", "Room.LoadWorld");
            
            try
            {
                // 验证索引范围
                if (worldSnapshot.Length < 0)
                {
                    ASLogger.Instance.Error($"LoadWorldFromSnapshot: 数据长度无效: {worldSnapshot.Length}", "Room.LoadWorld");
                    return null;
                }
                
                World world = MemoryPackHelper.Deserialize(typeof(World), worldSnapshot, 0, worldSnapshot.Length) as World;
                
                if (world == null)
                {
                    ASLogger.Instance.Error("World 快照反序列化结果为空", "Room.LoadWorld");
                    return null;
                }
                
                ASLogger.Instance.Info($"World 快照反序列化成功，实体数量: {world.Entities?.Count ?? 0}", "Room.LoadWorld");
                
                // 参考 Rollback 逻辑：清理旧世界，设置新世界
                MainWorld?.Cleanup();
                MainWorld = world;
                
                // 重建 World 的引用关系
                world.RoomId = (long)RoomId; // 显式类型转换：Room.RoomId (int) -> World.RoomId (long)
                // 注意：World 的 Systems 等引用会在反序列化后自动重建（通过 MemoryPackConstructor）
                
                ASLogger.Instance.Info($"World 已加载并替换 MainWorld，RoomId: {RoomId}", "Room.LoadWorld");
                
                return world;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                ASLogger.Instance.Error($"LoadWorldFromSnapshot: 索引越界错误 - 数据大小: {worldSnapshot.Length}, 错误: {ex.Message}", "Room.LoadWorld");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
                return null;
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Error($"LoadWorldFromSnapshot: World 快照反序列化失败 - 数据大小: {worldSnapshot.Length}, 错误: {ex.Message}", "Room.LoadWorld");
                ASLogger.Instance.LogException(ex, LogLevel.Error);
                return null;
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
        
        /// <summary>
        /// 销毁房间，清理所有资源
        /// </summary>
        public virtual void Shutdown()
        {
            ASLogger.Instance.Info($"Room: 销毁房间 {Name} (ID: {RoomId})");
            
            // 清理所有世界资源
            foreach (var world in Worlds)
            {
                world?.Cleanup();
            }
            
            // 清理玩家列表
            Players.Clear();
            
            // 清理帧同步控制器
            LSController = null;
            
            // 重置状态
            IsActive = false;
            Worlds.Clear();
        }
        
    }
}
