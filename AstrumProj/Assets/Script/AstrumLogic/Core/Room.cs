using System;
using System.Collections.Generic;
using System.Linq;
using Astrum.CommonBase;
using Astrum.Generated;
using Astrum.LogicCore.FrameSync;
using Astrum.LogicCore.Managers;
using cfg;
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
        /// 影子世界（用于影子实体预测）
        /// </summary>
        public World ShadowWorld { get; private set; }

        /// <summary>
        /// 最大玩家数
        /// </summary>
        public int MaxPlayers { get; set; } = 8;

        /// <summary>
        /// 帧同步控制器（基础接口，客户端和服务器通用）
        /// </summary>
        public ILSControllerBase? LSController { get; set; }

        /// <summary>
        /// 房间创建时间
        /// </summary>
        public long CreationTime { get; private set; }

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
        /// 创建实体（统一接口，由上层决定创建什么类型的实体）
        /// </summary>
        /// <param name="entityConfigId">实体配置ID</param>
        /// <returns>创建的实体ID，失败返回-1</returns>
        public long CreateEntity(int entityConfigId)
        {
            if (MainWorld == null)
            {
                ASLogger.Instance.Error($"Room.CreateEntity: MainWorld不存在");
                return -1;
            }

            var entity = MainWorld.CreateEntity(entityConfigId);
            if (entity == null)
            {
                ASLogger.Instance.Error($"Room.CreateEntity: 创建实体失败，ConfigId={entityConfigId}");
                return -1;
            }

            ASLogger.Instance.Info($"Room.CreateEntity: 实体创建成功，EntityId={entity.UniqueId}, ConfigId={entityConfigId}");
            return entity.UniqueId;
        }

        /// <summary>
        /// 根据Archetype类型获取实体集合
        /// </summary>
        /// <param name="archetype">实体原型类型</param>
        /// <returns>匹配的实体集合</returns>
        public IEnumerable<Entity> GetEntitiesByArchetype(EArchetype archetype)
        {
            if (MainWorld == null) return Enumerable.Empty<Entity>();
            
            return MainWorld.Entities.Values
                .Where(e => e.Archetype == archetype);
        }

        /// <summary>
        /// 移除实体（统一接口）
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <param name="destroyEntity">是否销毁实体，默认true</param>
        /// <returns>是否移除成功</returns>
        public bool RemoveEntity(long entityId, bool destroyEntity = true)
        {
            if (MainWorld == null) return false;
            
            var entity = MainWorld.GetEntity(entityId);
            if (entity == null) return false;
            
            if (destroyEntity)
            {
                MainWorld.DestroyEntity(entityId);
                ASLogger.Instance.Info($"Room.RemoveEntity: 实体已移除并销毁，EntityId={entityId}");
            }
            
            return true;
        }

        /// <summary>
        /// 更新房间
        /// </summary>
        /// <param name="deltaTime">时间差</param>
        public void Update(float deltaTime)
        {
            if (!IsActive) return;

            using (new ProfileScope("Room.Update"))
            {
                // 更新帧同步控制器
                using (new ProfileScope("Room.LSController.Tick"))
                {
                    if (LSController != null)
                    {
                        // ReplayLSController 需要使用 Tick(float deltaTime) 方法
                        if (LSController is ReplayLSController replayController)
                        {
                            replayController.Tick(deltaTime);
                        }
                        else
                        {
                            // 其他控制器使用无参 Tick() 方法
                            LSController.Tick();
                        }
                    }
                }
            }
        }
        
        public void FrameTick(OneFrameInputs oneFrameInputs)
        {
            using (new ProfileScope("Room.FrameTick"))
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
                            
                            // 记录输入设置日志（排除输入为空的情况）
                            if (input != null && (input.MoveX != 0 || input.MoveY != 0))
                            {
                                ASLogger.Instance.Info($"[Room.FrameTick] 实体 {pairs.Key} 设置输入 | 帧={input.Frame} | MoveX={input.MoveX} | MoveY={input.MoveY}", "Room.FrameTick");
                            }
                        }
                    }
                    else 
                    {
                        ASLogger.Instance.Info($"Room.FrameTick: 未找到实体，EntityId={pairs.Key}");
                    }
                }
                
                // 更新所有世界
                using (new ProfileScope("Room.UpdateWorlds"))
                {
                    foreach (var world in Worlds)
                    {
                        // 记录 World.Update 调用（仅当有非空输入时）
                        if (oneFrameInputs?.Inputs != null && oneFrameInputs.Inputs.Any(kv => kv.Value != null && (kv.Value.MoveX != 0 || kv.Value.MoveY != 0)))
                        {
                            ASLogger.Instance.Info($"[Room.FrameTick] 调用 World.Update | World.CurFrame={world.CurFrame}", "Room.FrameTick");
                        }
                        world.Update();
                    }
                }
                
                // 更新所有世界的 System
                using (new ProfileScope("Room.TickSystems"))
                {
                    TickSystems();
                }
            }
        }

        /// <summary>
        /// 影子实体帧更新（本地预测用）
        /// </summary>
        /// <param name="playerId">本地玩家Id</param>
        /// <param name="frame">预测帧号</param>
        /// <param name="inputs">本地预测输入</param>
        public void FrameTickShadow(long playerId, int frame, OneFrameInputs inputs)
        {
            if (MainWorld == null || inputs == null) return;

            using (new ProfileScope("Room.FrameTickShadow"))
            {
                // 确保 ShadowWorld 存在
                EnsureShadowWorld();

                // 确保影子实体存在
                var shadow = EnsureShadowEntity(playerId);
                if (shadow == null) return;

                // 设置输入
                if (playerId > 0 && inputs.Inputs.TryGetValue(playerId, out var input))
                {
                    var inputComponent = shadow.GetComponent<LSInputComponent>();
                    inputComponent?.SetInput(input);
                }

                // 更新 ShadowWorld 的 CurFrame（设置为预测帧号）
                ShadowWorld.CurFrame = frame;

                // 更新 ShadowWorld 的 CapabilitySystem（只更新影子实体）
                if (ShadowWorld.CapabilitySystem != null)
                {
                    ShadowWorld.CapabilitySystem.Update(ShadowWorld);
                    ShadowWorld.CapabilitySystem.ProcessEntityEvents();
                }
            }
        }

        /// <summary>
        /// 确保 ShadowWorld 存在
        /// </summary>
        private void EnsureShadowWorld()
        {
            if (ShadowWorld != null) return;

            ShadowWorld = ObjectPool.Instance.Fetch<World>();
            ShadowWorld.Reset();
            ShadowWorld.Initialize(MainWorld.CurFrame);
            ShadowWorld.RoomId = RoomId;
            ShadowWorld.Name = "ShadowWorld";
        }

        /// <summary>
        /// 确保影子实体存在（在 ShadowWorld 中，使用与源实体相同的 ID）
        /// </summary>
        private Entity EnsureShadowEntity(long playerId)
        {
            if (playerId <= 0 || MainWorld == null) return null;

            EnsureShadowWorld();

            // 影子实体使用与源实体相同的 ID（因为在不同 World 中）
            long shadowId = playerId;
            var shadow = ShadowWorld.GetEntity(shadowId);
            if (shadow != null)
            {
                // 确保权威实体已标记
                var original1 = MainWorld.GetEntity(playerId);
                if (original1 != null)
                {
                    original1.HasShadow = true;
                }
                return shadow;
            }

            // 尝试基于原实体创建影子
            var original = MainWorld.GetEntity(playerId);
            if (original == null)
            {
                ASLogger.Instance.Warning($"FrameTickShadow: 未找到原实体，无法创建影子，playerId={playerId}");
                return null;
            }

            if (original.EntityConfigId == 0)
            {
                ASLogger.Instance.Warning($"FrameTickShadow: 原实体缺少 EntityConfigId，无法创建影子，playerId={playerId}");
                return null;
            }

            // 在 ShadowWorld 中创建影子实体
            var newShadow = ShadowWorld.CreateEntity(original.EntityConfigId);
            if (newShadow == null)
            {
                ASLogger.Instance.Warning($"FrameTickShadow: 创建影子失败，playerId={playerId}");
                return null;
            }

            // 重设ID为与源实体相同的 ID
            ShadowWorld.Entities.Remove(newShadow.UniqueId);
            newShadow.UniqueId = shadowId;
            ShadowWorld.Entities[shadowId] = newShadow;

            // 初始化时从原实体完全复制状态
            CopyEntityStateToShadow(playerId, shadowId);

            // 标记权威实体有影子实体
            original.HasShadow = true;

            return newShadow;
        }

        /// <summary>
        /// 从权威实体复制状态到影子实体（用于回滚）
        /// 注意：originalId 和 shadowId 相同，但 original 在 MainWorld，shadow 在 ShadowWorld
        /// </summary>
        public void CopyEntityStateToShadow(long originalId, long shadowId)
        {
            if (MainWorld == null || ShadowWorld == null) return;

            var original = MainWorld.GetEntity(originalId);
            var shadow = ShadowWorld.GetEntity(shadowId);

            if (original == null || shadow == null) return;

            // 复制所有组件状态（直接通过反射复制，不使用 MemoryPack）
            foreach (var kv in original.Components)
            {
                var originalComp = kv.Value;
                var shadowComp = shadow.GetComponentById(originalComp.GetComponentTypeId());

                if (shadowComp != null)
                {
                    // 直接通过反射复制组件属性，不使用 MemoryPack
                    CopyComponentState(originalComp, shadowComp);
                }
            }

            // 复制 Capability 状态并注册到 ShadowWorld 的 CapabilitySystem
            if (original.CapabilityStates != null && shadow.CapabilityStates != null)
            {
                shadow.CapabilityStates.Clear();
                foreach (var kv in original.CapabilityStates)
                {
                    shadow.CapabilityStates[kv.Key] = kv.Value;
                    ShadowWorld.CapabilitySystem?.RegisterEntityCapability(shadowId, kv.Key);
                }
            }
        }

        /// <summary>
        /// 复制组件状态（直接通过反射复制属性，不使用 MemoryPack）
        /// </summary>
        private void CopyComponentState(Components.BaseComponent source, Components.BaseComponent target)
        {
            if (source == null || target == null) return;
            
            try
            {
                var sourceType = source.GetType();
                var targetType = target.GetType();
                
                // 确保类型匹配
                if (sourceType != targetType)
                {
                    ASLogger.Instance.Warning($"复制组件状态失败: 类型不匹配，源类型={sourceType.Name}，目标类型={targetType.Name}", "Room.CopyComponentState");
                    return;
                }
                
                // 直接复制所有可读写的属性
                foreach (var prop in sourceType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    if (!prop.CanRead || !prop.CanWrite) continue;
                    
                    // 跳过 MemoryPackIgnore 的属性（这些通常是运行时状态，不需要复制）
                    if (prop.GetCustomAttributes(typeof(MemoryPack.MemoryPackIgnoreAttribute), false).Length > 0) continue;
                    
                    // 跳过 EntityId（影子实体的 EntityId 应该保持自己的值）
                    if (prop.Name == "EntityId") continue;

                    try
                    {
                        var value = prop.GetValue(source);
                        prop.SetValue(target, value);
                    }
                    catch (Exception ex)
                    {
                        // 忽略无法复制的字段（可能是只读或特殊类型）
                        ASLogger.Instance.Debug($"复制组件属性失败: {prop.Name}, 错误: {ex.Message}", "Room.CopyComponentState");
                    }
                }
                
                // 复制所有字段（包括私有字段，用于完整状态复制）
                foreach (var field in sourceType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance))
                {
                    // 跳过 MemoryPackIgnore 的字段
                    if (field.GetCustomAttributes(typeof(MemoryPack.MemoryPackIgnoreAttribute), false).Length > 0) continue;
                    
                    // 跳过 EntityId
                    if (field.Name == "EntityId" || field.Name.Contains("EntityId")) continue;
                    
                    // 跳过只读字段（如 const 或 readonly）
                    if (field.IsInitOnly || field.IsLiteral) continue;

                    try
                    {
                        var value = field.GetValue(source);
                        field.SetValue(target, value);
                    }
                    catch (Exception ex)
                    {
                        // 忽略无法复制的字段
                        ASLogger.Instance.Debug($"复制组件字段失败: {field.Name}, 错误: {ex.Message}", "Room.CopyComponentState");
                    }
                }
            }
            catch (Exception ex)
            {
                ASLogger.Instance.Warning($"复制组件状态失败: {ex.Message}", "Room.CopyComponentState");
            }
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
            // ShadowWorld 的系统在 FrameTickShadow 中单独更新，这里不需要
        }

        /// <summary>
        /// 初始化房间
        /// </summary>
        /// <param name="controllerType">LSController 类型（"client"、"server" 或 "replay"），默认为 "client"</param>
        /// <param name="worldSnapshot">World 快照数据（可选），如果提供则反序列化并替换 MainWorld</param>
        public virtual void Initialize(string controllerType = "client", byte[] worldSnapshot = null)
        {
            // 如果提供了 World 快照，反序列化并替换 MainWorld
            if (worldSnapshot != null && worldSnapshot.Length > 0)
            {
                LoadWorldFromSnapshot(worldSnapshot);
            }
            else
            {
                MainWorld = ObjectPool.Instance.Fetch<World>();
                MainWorld.Reset(); // 重置状态，确保从对象池获取的对象是干净的
                MainWorld.Initialize(0);
                MainWorld.RoomId = RoomId;
            }
            
            if (MainWorld == null)
            {
                ASLogger.Instance.Error($"Room {RoomId} has no MainWorld defined.");
            }
            
            // 如果 LSController 已经设置（比如 ReplayLSController），则不重新创建
            if (LSController == null)
            {
                // 根据类型创建对应的 LSController
                if (controllerType == "server")
                {
                    LSController = new ServerLSController { Room = this };
                }
                else if (controllerType == "replay")
                {
                    LSController = new ReplayLSController { Room = this };
                }
                else // "client" 或其他
                {
                    LSController = new ClientLSController { Room = this };
                }
            }
            else
            {
                // 确保 LSController 的 Room 引用正确
                LSController.Room = this;
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
                
                // 清理 ShadowWorld（重连/重载时重建影子世界）
                if (ShadowWorld != null && ShadowWorld.IsFromPool)
                {
                    ShadowWorld.Recycle();
                }
                ShadowWorld = null;
                
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
            
            // 清理并回收所有世界资源
            foreach (var world in Worlds)
            {
                if (world != null && world.IsFromPool)
                {
                    world.Recycle(); // 调用 Recycle 方法（包含 Cleanup + Reset + 回收）
                }
            }
            
            // 清理 MainWorld
            if (MainWorld != null && MainWorld.IsFromPool)
            {
                MainWorld.Recycle(); // 调用 Recycle 方法（包含 Cleanup + Reset + 回收）
                MainWorld = null;
            }
            
            // 清理 ShadowWorld
            if (ShadowWorld != null && ShadowWorld.IsFromPool)
            {
                ShadowWorld.Recycle(); // 调用 Recycle 方法（包含 Cleanup + Reset + 回收）
                ShadowWorld = null;
            }
            
            // 清理帧同步控制器
            LSController = null;
            
            // 重置状态
            IsActive = false;
            Worlds.Clear();
        }
        
    }
}
