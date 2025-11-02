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
            
            ASLogger.Instance.Info($"HitSystem synced {entities.Count} entities to physics world");
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
        /// </summary>
        /// <param name="caster">施法者实体</param>
        /// <param name="shape">碰撞形状数据</param>
        /// <param name="filter">碰撞过滤器（可选）</param>
        /// <param name="skillInstanceId">技能实例ID（用于去重，可选）</param>
        /// <returns>命中的实体列表</returns>
        public List<AstrumEntity> QueryHits(AstrumEntity caster, CollisionShape shape, CollisionFilter filter = null, int skillInstanceId = 0)
        {
            if (caster == null)
            {
                ASLogger.Instance.Warning("HitManager: Caster is null");
                return new List<AstrumEntity>();
            }

            // 获取施法者位置和朝向
            var posComp = caster.GetComponent<TransComponent>();
            var casterPos = posComp?.Position ?? TSVector.zero;
            var casterRot = posComp?.Rotation ?? TSQuaternion.identity;

            // 【统一坐标转换】使用 CollisionShape.ToWorldTransform
            var worldTransform = shape.ToWorldTransform(casterPos, casterRot);

            // 根据形状类型进行查询
            List<AstrumEntity> candidates = null;

            switch (shape.ShapeType)
            {
                case HitBoxShape.Box:
                    candidates = _physicsWorld.QueryBoxOverlap(worldTransform.WorldCenter, shape.HalfSize, worldTransform.WorldRotation);
                    break;

                case HitBoxShape.Sphere:
                    candidates = _physicsWorld.QuerySphereOverlap(worldTransform.WorldCenter, shape.Radius);
                    break;

                case HitBoxShape.Capsule:
                    // TODO: 实现 Capsule 查询
                    ASLogger.Instance.Warning("HitManager: Capsule query not implemented yet");
                    candidates = new List<AstrumEntity>();
                    break;

                default:
                    ASLogger.Instance.Warning($"HitManager: Unknown shape type: {shape.ShapeType}");
                    candidates = new List<AstrumEntity>();
                    break;
            }

            // 防御性检查：确保候选列表不为null
            if (candidates == null)
            {
                ASLogger.Instance.Warning($"[HitSystem.QueryHits] Candidates list is null for caster {caster.UniqueId}");
                return new List<AstrumEntity>();
            }

            // 应用过滤
            var filteredHits = ApplyFilter(caster, candidates, filter);

            // 警告：如果所有候选者都被过滤掉
            if (filteredHits.Count == 0 && candidates.Count > 0)
            {
                ASLogger.Instance.Warning($"[HitSystem.QueryHits] All candidates were filtered out! Caster={caster.UniqueId}, ExcludedIds={string.Join(",", filter?.ExcludedEntityIds ?? new HashSet<long>())}");
            }

            // 应用去重
            if (skillInstanceId > 0)
            {
                filteredHits = ApplyDeduplication(skillInstanceId, filteredHits);
            }

            return filteredHits;
        }

        /// <summary>
        /// 应用过滤规则
        /// </summary>
        private List<AstrumEntity> ApplyFilter(AstrumEntity caster, List<AstrumEntity> candidates, CollisionFilter filter)
        {
            if (filter == null)
            {
                // 默认过滤：排除施法者自己
                return candidates.Where(e => e.UniqueId != caster.UniqueId).ToList();
            }

            var results = new List<AstrumEntity>();

            foreach (var candidate in candidates)
            {
                // 排除列表
                if (filter.ExcludedEntityIds.Contains(candidate.UniqueId))
                    continue;

                // 阵营过滤（基于Archetype）
                //if (filter.ExcludeAllies && IsSameTeam(caster, candidate))
                //    continue;
                
                //if (filter.OnlyEnemies && !IsEnemy(caster, candidate))
                //    continue;

                // 自定义过滤
                if (filter.CustomFilter != null && !filter.CustomFilter(candidate))
                    continue;

                results.Add(candidate);
            }

            return results;
        }

        /// <summary>
        /// 应用去重逻辑（同一技能实例不会重复命中同一目标）
        /// </summary>
        private List<AstrumEntity> ApplyDeduplication(int skillInstanceId, List<AstrumEntity> hits)
        {
            if (!HitCache.TryGetValue(skillInstanceId, out var hitTargets))
            {
                hitTargets = new Dictionary<long, int>();
                HitCache[skillInstanceId] = hitTargets;
            }

            var results = new List<AstrumEntity>();
            var currentFrame = 0; // TODO: 从 TimeManager 获取当前帧

            foreach (var hit in hits)
            {
                // 如果这个目标之前没有被命中过，或者已经超过冷却时间
                if (!hitTargets.ContainsKey(hit.UniqueId))
                {
                    hitTargets[hit.UniqueId] = currentFrame;
                    results.Add(hit);
                }
            }

            return results;
        }

        /// <summary>
        /// 判断两个实体是否是同一阵营
        /// </summary>
        private bool IsSameTeam(AstrumEntity entity1, AstrumEntity entity2)
        {
            if (entity1 == null || entity2 == null)
                return false;
            
            
            string archetype1 = entity1.ArchetypeName;
            string archetype2 = entity2.ArchetypeName;
            
            return archetype1 == archetype2;
        }
        
        /// <summary>
        /// 判断target是否是caster的敌人
        /// </summary>
        private bool IsEnemy(AstrumEntity caster, AstrumEntity target)
        {
            if (caster == null || target == null)
                return false;
            
            string casterArchetype = caster.ArchetypeName;
            string targetArchetype = target.ArchetypeName;
            
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

