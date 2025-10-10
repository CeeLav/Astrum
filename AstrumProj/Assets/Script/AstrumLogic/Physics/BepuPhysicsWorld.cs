using System;
using System.Collections.Generic;
using BEPUphysics;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.CollisionShapes.ConvexShapes;
using BEPUphysics.Entities.Prefabs;
using BEPUutilities;
using TrueSync;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Components;
using FixMath.NET;

namespace Astrum.LogicCore.Physics
{
    /// <summary>
    /// BEPU 物理世界封装
    /// 用于技能碰撞检测的查询型物理世界
    /// </summary>
    public class BepuPhysicsWorld
    {
        private Space _space;
        private readonly Dictionary<long, BroadPhaseEntry> _entityBodies = new Dictionary<long, BroadPhaseEntry>();

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
        /// 注册实体到物理世界
        /// </summary>
        public void RegisterEntity(Entity entity, CollisionShape shape)
        {
            if (entity == null || _entityBodies.ContainsKey(entity.UniqueId))
                return;

            var position = entity.GetComponent<PositionComponent>()?.Position ?? TSVector.zero;
            var bepuPos = position.ToBepuVector();

            BroadPhaseEntry body = null;

            switch (shape.ShapeType)
            {
                case HitBoxShape.Box:
                    var halfSize = shape.HalfSize.ToBepuVector();
                    var box = new Box(bepuPos, halfSize.X * (Fix64)2, halfSize.Y * (Fix64)2, halfSize.Z * (Fix64)2);
                    box.Tag = entity; // 存储实体引用
                    body = box.CollisionInformation;
                    _space.Add(box);
                    break;

                case HitBoxShape.Sphere:
                    var sphere = new Sphere(bepuPos, shape.Radius.ToFix64());
                    sphere.Tag = entity;
                    body = sphere.CollisionInformation;
                    _space.Add(sphere);
                    break;

                case HitBoxShape.Capsule:
                    var capsule = new Capsule(bepuPos, shape.Height.ToFix64(), shape.Radius.ToFix64());
                    capsule.Tag = entity;
                    body = capsule.CollisionInformation;
                    _space.Add(capsule);
                    break;
            }

            if (body != null)
            {
                _entityBodies[entity.UniqueId] = body;
            }
        }

        /// <summary>
        /// 从物理世界移除实体
        /// </summary>
        public void UnregisterEntity(Entity entity)
        {
            if (entity == null || !_entityBodies.TryGetValue(entity.UniqueId, out var body))
                return;

            // 从 Tag 中获取对应的 BEPU Entity
            var bepuEntity = body.Tag as BEPUphysics.Entities.Entity;
            if (bepuEntity != null)
            {
                _space.Remove(bepuEntity);
            }

            _entityBodies.Remove(entity.UniqueId);
        }

        /// <summary>
        /// 更新实体位置（如果实体移动了）
        /// </summary>
        public void UpdateEntityPosition(Entity entity)
        {
            if (entity == null || !_entityBodies.TryGetValue(entity.UniqueId, out var body))
                return;

            var position = entity.GetComponent<PositionComponent>()?.Position ?? TSVector.zero;
            var bepuPos = position.ToBepuVector();

            // 从 Tag 中获取对应的 BEPU Entity
            var bepuEntity = body.Tag as BEPUphysics.Entities.Entity;
            if (bepuEntity != null)
            {
                bepuEntity.Position = bepuPos;
            }
        }

        /// <summary>
        /// 清空物理世界
        /// </summary>
        public void Clear()
        {
            foreach (var body in _entityBodies.Values)
            {
                // 从 Tag 中获取对应的 BEPU Entity
                var bepuEntity = body.Tag as BEPUphysics.Entities.Entity;
                if (bepuEntity != null)
                {
                    _space.Remove(bepuEntity);
                }
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
        public List<Entity> QueryBoxOverlap(TSVector center, TSVector halfSize, TSQuaternion rotation)
        {
            var results = new List<Entity>();
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
                // 从 BroadPhaseEntry 的 Tag 获取实体
                var bepuEntity = candidate.Tag as BEPUphysics.Entities.Entity;
                if (bepuEntity != null && bepuEntity.Tag is Entity entity)
                {
                    results.Add(entity);
                }
            }

            return results;
        }

        /// <summary>
        /// Sphere 重叠查询
        /// </summary>
        public List<Entity> QuerySphereOverlap(TSVector center, FP radius)
        {
            var results = new List<Entity>();
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
                // 从 BroadPhaseEntry 的 Tag 获取实体
                var bepuEntity = candidate.Tag as BEPUphysics.Entities.Entity;
                if (bepuEntity != null && bepuEntity.Tag is Entity entity)
                {
                    results.Add(entity);
                }
            }

            return results;
        }
    }
}

