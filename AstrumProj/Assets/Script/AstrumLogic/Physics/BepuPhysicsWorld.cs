using System;
using System.Collections.Generic;
using BEPUphysics;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.Entities.Prefabs;
using BEPUutilities;
using TrueSync;
using Astrum.LogicCore.Components;
using Astrum.CommonBase;
using FixMath.NET;
// 使用别名避免命名冲突：BEPU 也有 Entity 类
using AstrumEntity = Astrum.LogicCore.Core.Entity;
using BepuEntity = BEPUphysics.Entities.Entity;

namespace Astrum.LogicCore.Physics
{
    /// <summary>
    /// BEPU 物理世界封装
    /// 用于技能碰撞检测的查询型物理世界
    /// </summary>
    public class BepuPhysicsWorld
    {
        private Space _space;
        // 修复：存储 BEPU Entity 而不是 BroadPhaseEntry
        private readonly Dictionary<long, BepuEntity> _entityBodies = new Dictionary<long, BepuEntity>();

        /// <summary>
        /// 获取 BEPU Space 实例
        /// </summary>
        public Space Space => _space;

        /// <summary>
        /// 初始化物理世界
        /// </summary>
        public void Initialize()
        {
            _space = new Space();
            
            // 配置为查询型世界（不进行动力学模拟）
            _space.ForceUpdater.Gravity = Vector3.Zero; // 无重力
            _space.TimeStepSettings.TimeStepDuration = Fix64.Zero; // 不进行时间步进
            
            // 禁用不需要的模拟阶段（性能优化）
            _space.DeactivationManager.Enabled = false; // 禁用休眠管理
            _space.Solver.Enabled = false; // 禁用求解器（只做查询）
        }

        /// <summary>
        /// 注册实体到物理世界（从 CollisionComponent 获取碰撞盒）
        /// </summary>
        public void RegisterEntity(AstrumEntity entity)
        {
            if (entity == null || _entityBodies.ContainsKey(entity.UniqueId))
                return;

            // 从 CollisionComponent 获取碰撞盒
            var collisionComponent = entity.GetComponent<CollisionComponent>();
            if (collisionComponent == null || collisionComponent.Shapes == null || collisionComponent.Shapes.Count == 0)
            {
                ASLogger.Instance.Warning($"[BepuPhysicsWorld] Entity {entity.UniqueId} has no collision shapes");
                return;
            }

            var position = entity.GetComponent<PositionComponent>()?.Position ?? TSVector.zero;
            var bepuPos = position.ToBepuVector();

            // TODO: 目前只注册第一个碰撞盒，后续可支持多个
            var shape = collisionComponent.Shapes[0];
            BepuEntity bepuEntity = null;

            switch (shape.ShapeType)
            {
                case HitBoxShape.Box:
                    var halfSize = shape.HalfSize.ToBepuVector();
                    var box = new Box(bepuPos, halfSize.X * (Fix64)2, halfSize.Y * (Fix64)2, halfSize.Z * (Fix64)2);
                    box.Tag = entity; // 存储我们的实体引用
                    bepuEntity = box;
                    _space.Add(box);
                    break;

                case HitBoxShape.Sphere:
                    var sphere = new Sphere(bepuPos, shape.Radius.ToFix64());
                    sphere.Tag = entity;
                    bepuEntity = sphere;
                    _space.Add(sphere);
                    break;

                case HitBoxShape.Capsule:
                    var capsule = new Capsule(bepuPos, shape.Height.ToFix64(), shape.Radius.ToFix64());
                    capsule.Tag = entity;
                    bepuEntity = capsule;
                    _space.Add(capsule);
                    break;
            }

            if (bepuEntity != null)
            {
                _entityBodies[entity.UniqueId] = bepuEntity;
            }
        }

        /// <summary>
        /// 从物理世界移除实体
        /// </summary>
        public void UnregisterEntity(AstrumEntity entity)
        {
            if (entity == null || !_entityBodies.TryGetValue(entity.UniqueId, out var bepuEntity))
                return;

            _space.Remove(bepuEntity);
            _entityBodies.Remove(entity.UniqueId);
        }

        /// <summary>
        /// 更新实体位置（如果实体移动了）
        /// </summary>
        public void UpdateEntityPosition(AstrumEntity entity)
        {
            if (entity == null || !_entityBodies.TryGetValue(entity.UniqueId, out var bepuEntity))
                return;

            var position = entity.GetComponent<PositionComponent>()?.Position ?? TSVector.zero;
            var bepuPos = position.ToBepuVector();
            
            bepuEntity.Position = bepuPos;
        }

        /// <summary>
        /// 清空物理世界
        /// </summary>
        public void Clear()
        {
            foreach (var bepuEntity in _entityBodies.Values)
            {
                _space.Remove(bepuEntity);
            }
            _entityBodies.Clear();
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Clear();
            _space = null;
        }

        /// <summary>
        /// Box 重叠查询
        /// </summary>
        public List<AstrumEntity> QueryBoxOverlap(TSVector center, TSVector halfSize, TSQuaternion rotation)
        {
            var results = new List<AstrumEntity>();
            var bepuCenter = center.ToBepuVector();
            var bepuHalfSize = halfSize.ToBepuVector();
            var bepuRotation = rotation.ToBepuQuaternion();

            // 创建临时 Box 用于查询
            var queryBox = new Box(bepuCenter, bepuHalfSize.X * (Fix64)2, bepuHalfSize.Y * (Fix64)2, bepuHalfSize.Z * (Fix64)2);
            queryBox.Orientation = bepuRotation;

            // 计算 AABB 包围盒（Box 的 AABB）
            var boundingBox = new BEPUutilities.BoundingBox();
            queryBox.CollisionInformation.UpdateBoundingBox();
            boundingBox = queryBox.CollisionInformation.BoundingBox;

            // 使用 BroadPhase 进行 AABB 查询
            var candidates = new BEPUutilities.DataStructures.RawList<BroadPhaseEntry>();
            _space.BroadPhase.QueryAccelerator.GetEntries(boundingBox, candidates);

            // 遍历候选者，提取实体
            foreach (var candidate in candidates)
            {
                // 从 EntityCollidable 中获取 Entity
                if (candidate is EntityCollidable collidable && collidable.Entity != null)
                {
                    var bepuEntity = collidable.Entity;
                    if (bepuEntity.Tag is AstrumEntity entity)
                    {
                        results.Add(entity);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Sphere 重叠查询
        /// </summary>
        public List<AstrumEntity> QuerySphereOverlap(TSVector center, FP radius)
        {
            var results = new List<AstrumEntity>();
            var bepuCenter = center.ToBepuVector();
            var bepuRadius = radius.ToFix64();

            // 创建临时 Sphere 用于查询
            var querySphere = new Sphere(bepuCenter, bepuRadius);

            // 计算 AABB 包围盒（Sphere 的 AABB）
            var boundingBox = new BEPUutilities.BoundingBox();
            querySphere.CollisionInformation.UpdateBoundingBox();
            boundingBox = querySphere.CollisionInformation.BoundingBox;

            // 使用 BroadPhase 进行 AABB 查询
            var candidates = new BEPUutilities.DataStructures.RawList<BroadPhaseEntry>();
            _space.BroadPhase.QueryAccelerator.GetEntries(boundingBox, candidates);

            // 遍历候选者，提取实体
            foreach (var candidate in candidates)
            {
                // candidate 是 BroadPhaseEntry (CollisionInformation)
                // 需要获取所属的 BEPU Entity，然后从其 Tag 获取我们的 Entity
                if (candidate is EntityCollidable collidable)
                {
                    var bepuEntity = collidable.Entity;
                    if (bepuEntity != null && bepuEntity.Tag is AstrumEntity entity)
                    {
                        results.Add(entity);
                    }
                }
            }

            return results;
        }
    }
}

