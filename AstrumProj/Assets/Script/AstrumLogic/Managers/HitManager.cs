using System;
using System.Collections.Generic;
using System.Linq;
using TrueSync;
using Astrum.LogicCore.Components;
using Astrum.CommonBase;
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
    /// 命中管理器
    /// 负责技能碰撞检测的即时查询
    /// </summary>
    public class HitManager :Singleton<HitManager>
    {
        private readonly BepuPhysicsWorld _physicsWorld;
        
        // 命中缓存：技能实例ID -> (目标实体ID -> 上次命中时间)
        private readonly Dictionary<int, Dictionary<long, int>> _hitCache = new Dictionary<int, Dictionary<long, int>>();

        /// <summary>
        /// 获取物理世界实例
        /// </summary>
        public BepuPhysicsWorld PhysicsWorld => _physicsWorld;

        public HitManager()
        {
            _physicsWorld = new BepuPhysicsWorld();
            _physicsWorld.Initialize();
        }

        /// <summary>
        /// 构造函数（接收外部物理世界实例）
        /// </summary>
        public HitManager(BepuPhysicsWorld physicsWorld)
        {
            _physicsWorld = physicsWorld ?? throw new ArgumentNullException(nameof(physicsWorld));
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
            
            // 调试日志
            ASLogger.Instance.Info($"[HitManager.QueryHits] Caster={caster.UniqueId} " +
                $"Pos=({casterPos.x:F2},{casterPos.y:F2},{casterPos.z:F2}) " +
                $"Rot=({casterRot.x:F2},{casterRot.y:F2},{casterRot.z:F2},{casterRot.w:F2}) " +
                $"LocalOffset=({shape.LocalOffset.x:F2},{shape.LocalOffset.y:F2},{shape.LocalOffset.z:F2}) " +
                $"→ WorldCenter=({worldTransform.WorldCenter.x:F2},{worldTransform.WorldCenter.y:F2},{worldTransform.WorldCenter.z:F2})");

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

            // 应用过滤
            var filteredHits = ApplyFilter(caster, candidates, filter);

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

                // TODO: 阵营过滤（需要实现 Team/Faction 系统）
                // if (filter.ExcludeAllies && IsSameTeam(caster, candidate))
                //     continue;
                
                // if (filter.OnlyEnemies && !IsEnemy(caster, candidate))
                //     continue;

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
            if (!_hitCache.TryGetValue(skillInstanceId, out var hitTargets))
            {
                hitTargets = new Dictionary<long, int>();
                _hitCache[skillInstanceId] = hitTargets;
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
        /// 清除技能实例的命中缓存
        /// </summary>
        public void ClearHitCache(int skillInstanceId)
        {
            _hitCache.Remove(skillInstanceId);
        }

        /// <summary>
        /// 清除所有命中缓存
        /// </summary>
        public void ClearAllHitCache()
        {
            _hitCache.Clear();
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
            _hitCache.Clear();
            _physicsWorld?.Dispose();
        }
    }
}

