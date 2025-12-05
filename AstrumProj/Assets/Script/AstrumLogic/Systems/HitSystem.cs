using System;
using System.Collections.Generic;
using System.Linq;
using TrueSync;
using Astrum.LogicCore.Components;
using Astrum.CommonBase;
using MemoryPack;
// 使用别名避免与 BEPU 的 Entity 类冲突
using AstrumEntity = Astrum.LogicCore.Core.Entity;

namespace Astrum.LogicCore.Physics
{
    /// <summary>
    /// 碰撞过滤器
    /// </summary>
    public class CollisionFilter
    {
        /// <summary>排除的实体（通常是施法者自己）</summary>
        public HashSet<long> ExcludedEntityIds { get; set; } = new HashSet<long>();

        /// <summary>是否排除友军（默认：是）</summary>
        public bool ExcludeAllies { get; set; } = true;

        /// <summary>是否只命中敌人（默认：是）</summary>
        public bool OnlyEnemies { get; set; } = true;

        /// <summary>自定义过滤函数</summary>
        public Func<AstrumEntity, bool> CustomFilter { get; set; }
    }

    /// <summary>
    /// 命中系统
    /// 负责技能碰撞检测的即时查询，隶属于 World
    /// </summary>
    [MemoryPackable]
    public partial class HitSystem
    {
        /// <summary>
        /// 物理世界实例（不序列化，每次初始化时重新创建）
        /// </summary>
        [MemoryPackIgnore]
        private BepuPhysicsWorld _physicsWorld;
        
        /// <summary>
        /// 命中缓存：技能实例ID -> (目标实体ID -> 上次命中时间)
        /// </summary>
        public Dictionary<int, Dictionary<long, int>> HitCache { get; private set; }

        /// <summary>
        /// 获取物理世界实例
        /// </summary>
        [MemoryPackIgnore]
        public BepuPhysicsWorld PhysicsWorld => _physicsWorld;

        /// <summary>
        /// 默认构造函数（用于序列化）
        /// </summary>
        public HitSystem()
        {
            HitCache = new Dictionary<int, Dictionary<long, int>>();
        }

        /// <summary>
        /// MemoryPack 构造函数
        /// </summary>
        [MemoryPackConstructor]
        public HitSystem(Dictionary<int, Dictionary<long, int>> hitCache)
        {
            HitCache = hitCache ?? new Dictionary<int, Dictionary<long, int>>();
        }

        /// <summary>
        /// 初始化物理世界（每次启动时调用）
        /// </summary>
        public void Initialize()
        {
            if (_physicsWorld == null)
            {
                _physicsWorld = new BepuPhysicsWorld();
                _physicsWorld.Initialize();
            }
        }

        /// <summary>
        /// 从 World 的实体同步物理数据（反序列化后调用）
        /// </summary>
        /// <param name="entities">World 中的所有实体</param>
        public void SyncFromEntities(Dictionary<long, AstrumEntity> entities)
        {
            // 先初始化物理世界
            Initialize();
            
            // 清空现有物理对象
            _physicsWorld.Clear();
            
            // 从 World 的实体重新注册到物理世界
            foreach (var entity in entities.Values)
            {
                // 只注册有碰撞组件的实体
                var collisionComponent = entity.GetComponent<CollisionComponent>();
                if (collisionComponent != null && collisionComponent.Shapes != null && collisionComponent.Shapes.Count > 0)
                {
                    RegisterEntity(entity);
                }
            }
            
            ASLogger.Instance.Debug($"HitSystem synced {entities.Count} entities to physics world");
        }

        /// <summary>
        /// 注册实体到物理世界（代理方法）
        /// </summary>
        public void RegisterEntity(AstrumEntity entity)
        {
            _physicsWorld.RegisterEntity(entity);
        }

        /// <summary>
        /// 即时命中查询（主入口）
        /// 优化版本：使用输出参数避免创建新 List
        /// </summary>
        /// <param name="caster">施法者实体</param>
        /// <param name="shape">碰撞形状数据</param>
        /// <param name="filter">碰撞过滤器（可选）</param>
        /// <param name="outResults">输出结果列表（会被清空后填充）</param>
        /// <param name="skillInstanceId">技能实例ID（用于去重，可选）</param>
        public void QueryHits(AstrumEntity caster, CollisionShape shape, CollisionFilter filter, List<AstrumEntity> outResults, int skillInstanceId = 0)
        {
            outResults.Clear();
            
            if (caster == null)
            {
                ASLogger.Instance.Warning("HitManager: Caster is null");
                return;
            }

            // 获取施法者位置和朝向
            var posComp = caster.GetComponent<TransComponent>();
            var casterPos = posComp?.Position ?? TSVector.zero;
            var casterRot = posComp?.Rotation ?? TSQuaternion.identity;

            // 【统一坐标转换】使用 CollisionShape.ToWorldTransform
            var worldTransform = shape.ToWorldTransform(casterPos, casterRot);

            // 根据形状类型进行查询（直接填充到 outResults，避免创建临时 List）
            switch (shape.ShapeType)
            {
                case HitBoxShape.Box:
                    _physicsWorld.QueryBoxOverlap(worldTransform.WorldCenter, shape.HalfSize, worldTransform.WorldRotation, outResults);
                    break;

                case HitBoxShape.Sphere:
                    _physicsWorld.QuerySphereOverlap(worldTransform.WorldCenter, shape.Radius, outResults);
                    break;

                case HitBoxShape.Capsule:
                    // TODO: 实现 Capsule 查询
                    ASLogger.Instance.Warning("HitManager: Capsule query not implemented yet");
                    return;

                case HitBoxShape.Cylinder:
                    ASLogger.Instance.Warning("HitManager: Cylinder query not implemented yet");
                    return;

                default:
                    ASLogger.Instance.Warning($"HitManager: Unknown shape type: {shape.ShapeType}");
                    return;
            }

            // 如果没有候选者，直接返回
            if (outResults.Count == 0)
            {
                return;
            }

            // 应用过滤（就地修改 outResults）
            using (new ProfileScope("HitSys.ApplyFilter"))
            {
                ApplyFilterInPlace(caster, filter, outResults);
            }

            // 应用去重（就地修改 outResults）
            if (skillInstanceId > 0)
            {
                using (new ProfileScope("HitSys.ApplyDedup"))
                {
                    ApplyDeduplication(skillInstanceId, outResults);
                }
            }
        }

        /// <summary>
        /// 射线命中查询（用于抛射物）
        /// </summary>
        public List<RaycastHitInfo> QueryRaycast(AstrumEntity caster, TSVector origin, TSVector direction, FP maxDistance, CollisionFilter filter = null)
        {
            Initialize();

            Func<AstrumEntity, bool> entryFilter = entity =>
            {
                if (entity == null)
                    return false;

                if (caster != null && entity.UniqueId == caster.UniqueId)
                    return false;

                if (filter != null)
                {
                    if (filter.ExcludedEntityIds.Contains(entity.UniqueId))
                        return false;

                    if (caster != null)
                    {
                        if (filter.ExcludeAllies && IsSameTeam(caster, entity))
                            return false;

                        if (filter.OnlyEnemies && !IsEnemy(caster, entity))
                            return false;
                    }

                    if (filter.CustomFilter != null && !filter.CustomFilter(entity))
                        return false;
                }

                return true;
            };

            return _physicsWorld.Raycast(origin, direction, maxDistance, entryFilter);
        }

        /// <summary>
        /// 应用过滤规则（优化版本：就地修改 List，避免创建新 List）
        /// </summary>
        private void ApplyFilterInPlace(AstrumEntity caster, CollisionFilter filter, List<AstrumEntity> inOutResults)
        {
            if (filter == null)
            {
                using (new ProfileScope("Filter.DefaultFilter"))
                {
                    // 默认过滤：排除施法者自己（反向遍历，就地移除）
                    for (int i = inOutResults.Count - 1; i >= 0; i--)
                    {
                        if (inOutResults[i].UniqueId == caster.UniqueId)
                        {
                            inOutResults.RemoveAt(i);
                        }
                    }
                }
                return;
            }

            using (new ProfileScope("Filter.CustomFilter"))
            {
                // 反向遍历，就地移除不符合条件的实体（避免索引问题）
                for (int i = inOutResults.Count - 1; i >= 0; i--)
                {
                    var candidate = inOutResults[i];
                    bool shouldRemove = false;
                    
                    // 排除列表
                    if (filter.ExcludedEntityIds.Contains(candidate.UniqueId))
                    {
                        shouldRemove = true;
                    }
                    // 阵营过滤（基于Archetype）
                    //else if (filter.ExcludeAllies && IsSameTeam(caster, candidate))
                    //{
                    //    shouldRemove = true;
                    //}
                    //else if (filter.OnlyEnemies && !IsEnemy(caster, candidate))
                    //{
                    //    shouldRemove = true;
                    //}
                    // 自定义过滤
                    else if (filter.CustomFilter != null && !filter.CustomFilter(candidate))
                    {
                        shouldRemove = true;
                    }

                    if (shouldRemove)
                    {
                        inOutResults.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>
        /// 应用去重逻辑（同一技能实例不会重复命中同一目标）
        /// 优化版本：就地修改 List，避免创建新 List
        /// </summary>
        private void ApplyDeduplication(int skillInstanceId, List<AstrumEntity> inOutHits)
        {
            Dictionary<long, int> hitTargets;
            
            using (new ProfileScope("Dedup.GetCache"))
            {
                if (!HitCache.TryGetValue(skillInstanceId, out hitTargets))
                {
                    hitTargets = new Dictionary<long, int>();
                    HitCache[skillInstanceId] = hitTargets;
                }
            }

            var currentFrame = 0; // TODO: 从 TimeManager 获取当前帧

            using (new ProfileScope("Dedup.RemoveDuplicates"))
            {
                // 反向遍历，移除已命中过的目标（避免索引问题）
                for (int i = inOutHits.Count - 1; i >= 0; i--)
                {
                    var hit = inOutHits[i];
                    
                    // 如果这个目标之前已经被命中过
                    if (hitTargets.ContainsKey(hit.UniqueId))
                    {
                        inOutHits.RemoveAt(i);  // 就地移除
                    }
                    else
                    {
                        hitTargets[hit.UniqueId] = currentFrame;  // 记录新命中
                    }
                }
            }
        }

        /// <summary>
        /// 判断两个实体是否是同一阵营
        /// </summary>
        private bool IsSameTeam(AstrumEntity entity1, AstrumEntity entity2)
        {
            if (entity1 == null || entity2 == null)
                return false;
            
            
            var archetype1 = entity1.Archetype;
            var archetype2 = entity2.Archetype;
            
            return archetype1 == archetype2;
        }
        
        /// <summary>
        /// 判断target是否是caster的敌人
        /// </summary>
        private bool IsEnemy(AstrumEntity caster, AstrumEntity target)
        {
            if (caster == null || target == null)
                return false;
            
            var casterArchetype = caster.Archetype;
            var targetArchetype = target.Archetype;
            
            return casterArchetype != targetArchetype;
        }

        /// <summary>
        /// 清除技能实例的命中缓存
        /// </summary>
        public void ClearHitCache(int skillInstanceId)
        {
            HitCache.Remove(skillInstanceId);
        }

        /// <summary>
        /// 清除所有命中缓存
        /// </summary>
        public void ClearAllHitCache()
        {
            HitCache.Clear();
        }

        /// <summary>
        /// 更新物理世界（刷新 BroadPhase 数据）
        /// </summary>
        /// <param name="deltaTime">时间步长（目前仅用于兼容接口）</param>
        public void StepPhysics(float deltaTime)
        {
            Initialize();
            _physicsWorld.Step(deltaTime);
        }

        // RegisterEntity 方法已移到前面统一定义

        /// <summary>
        /// 移除实体
        /// </summary>
        public void UnregisterEntity(AstrumEntity entity)
        {
            _physicsWorld.UnregisterEntity(entity);
        }

        /// <summary>
        /// 更新实体位置
        /// </summary>
        public void UpdateEntityPosition(AstrumEntity entity)
        {
            _physicsWorld.UpdateEntityPosition(entity);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            HitCache.Clear();
            _physicsWorld?.Dispose();
        }
    }
}

