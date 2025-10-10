using System;
using System.Collections.Generic;
using System.Linq;
using TrueSync;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Components;
using Astrum.CommonBase;

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
        public Func<Entity, bool> CustomFilter { get; set; }
    }

    /// <summary>
    /// 命中管理器
    /// 负责技能碰撞检测的即时查询
    /// </summary>
    public class HitManager
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
        /// 即时命中查询（主入口）
        /// </summary>
        /// <param name="caster">施法者实体</param>
        /// <param name="hitBoxData">命中盒数据</param>
        /// <param name="filter">碰撞过滤器（可选）</param>
        /// <param name="skillInstanceId">技能实例ID（用于去重，可选）</param>
        /// <returns>命中的实体列表</returns>
        public List<Entity> QueryHits(Entity caster, HitBoxData hitBoxData, CollisionFilter filter = null, int skillInstanceId = 0)
        {
            if (caster == null)
            {
                ASLogger.Instance.Warning("HitManager: Caster is null");
                return new List<Entity>();
            }

            // 获取施法者位置和朝向
            var casterPos = caster.GetComponent<PositionComponent>()?.Position ?? TSVector.zero;
            var casterRot = TSQuaternion.identity; // TODO: 从 RotationComponent 获取

            // 计算世界空间的命中盒姿态（使用四元数变换向量）
            var worldCenter = casterPos + casterRot * hitBoxData.LocalOffset;
            var worldRotation = casterRot * hitBoxData.LocalRotation;

            // 根据形状类型进行查询
            List<Entity> candidates = null;

            switch (hitBoxData.ShapeType)
            {
                case HitBoxShape.Box:
                    candidates = _physicsWorld.QueryBoxOverlap(worldCenter, hitBoxData.HalfSize, worldRotation);
                    break;

                case HitBoxShape.Sphere:
                    candidates = _physicsWorld.QuerySphereOverlap(worldCenter, hitBoxData.Radius);
                    break;

                case HitBoxShape.Capsule:
                    // TODO: 实现 Capsule 查询
                    ASLogger.Instance.Warning("HitManager: Capsule query not implemented yet");
                    candidates = new List<Entity>();
                    break;

                default:
                    ASLogger.Instance.Warning($"HitManager: Unknown shape type: {hitBoxData.ShapeType}");
                    candidates = new List<Entity>();
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
        private List<Entity> ApplyFilter(Entity caster, List<Entity> candidates, CollisionFilter filter)
        {
            if (filter == null)
            {
                // 默认过滤：排除施法者自己
                return candidates.Where(e => e.UniqueId != caster.UniqueId).ToList();
            }

            var results = new List<Entity>();

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
        private List<Entity> ApplyDeduplication(int skillInstanceId, List<Entity> hits)
        {
            if (!_hitCache.TryGetValue(skillInstanceId, out var hitTargets))
            {
                hitTargets = new Dictionary<long, int>();
                _hitCache[skillInstanceId] = hitTargets;
            }

            var results = new List<Entity>();
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

        /// <summary>
        /// 注册可命中实体
        /// </summary>
        public void RegisterEntity(Entity entity, CollisionShape shape)
        {
            _physicsWorld.RegisterEntity(entity, shape);
        }

        /// <summary>
        /// 移除实体
        /// </summary>
        public void UnregisterEntity(Entity entity)
        {
            _physicsWorld.UnregisterEntity(entity);
        }

        /// <summary>
        /// 更新实体位置
        /// </summary>
        public void UpdateEntityPosition(Entity entity)
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

