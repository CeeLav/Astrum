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
using Ray = BEPUutilities.Ray;
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

            var position = entity.GetComponent<TransComponent>()?.Position ?? TSVector.zero;
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

            var transComp = entity.GetComponent<TransComponent>();
            if (transComp == null)
                return;

            var position = transComp.Position;
            var rotation = transComp.Rotation;
            var bepuPos = position.ToBepuVector();
            var bepuRot = rotation.ToBepuQuaternion();
            
            // 【关键修复】手动更新位置时，必须同时更新碰撞信息的世界变换
            // 否则AABB和查询会使用旧的位置，导致在原点附近才能命中
            bepuEntity.Position = bepuPos;
            bepuEntity.Orientation = bepuRot;
            
            // 更新碰撞信息的世界变换（这对于BroadPhase查询至关重要）
            bepuEntity.CollisionInformation.UpdateWorldTransform(ref bepuPos, ref bepuRot);
            
            // 更新AABB包围盒（BroadPhase使用AABB进行快速筛选）
            bepuEntity.CollisionInformation.UpdateBoundingBox(0); // dt=0，因为我们不关心速度扩展
            
            // 注意：IsActive是只读属性，不能手动设置
            // BroadPhase会在查询时自动使用更新后的AABB（通过UpdateBoundingBox）
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
        /// 获取实体在物理世界中的实际位置（用于调试）
        /// </summary>
        public TSVector? GetPhysicsPosition(AstrumEntity entity)
        {
            if (entity == null || !_entityBodies.TryGetValue(entity.UniqueId, out var bepuEntity))
                return null;
            
            return bepuEntity.Position.ToTSVector();
        }

        /// <summary>
        /// 获取实体在物理世界中的实际旋转（用于调试）
        /// </summary>
        public TSQuaternion? GetPhysicsRotation(AstrumEntity entity)
        {
            if (entity == null || !_entityBodies.TryGetValue(entity.UniqueId, out var bepuEntity))
                return null;
            
            return bepuEntity.Orientation.ToTSQuaternion();
        }

        /// <summary>
        /// 射线检测（返回沿射线方向命中的所有实体）
        /// </summary>
        /// <param name="origin">射线起点</param>
        /// <param name="direction">射线方向（无需归一化）</param>
        /// <param name="maxDistance">最大检测距离（<=0 表示使用 direction 的长度）</param>
        /// <param name="entryFilter">可选过滤器，返回 true 表示保留该实体</param>
        public List<RaycastHitInfo> Raycast(TSVector origin, TSVector direction, FP maxDistance, Func<AstrumEntity, bool> entryFilter = null)
        {
            var hits = new List<RaycastHitInfo>();

            if (_space == null)
            {
                Initialize();
            }

            var dirLength = direction.magnitude;
            if (dirLength <= FP.Epsilon)
            {
                return hits;
            }

            var normalizedDir = direction / dirLength;
            var length = maxDistance > FP.Zero ? maxDistance : dirLength;
            if (length <= FP.Epsilon)
            {
                return hits;
            }

            var ray = new Ray(origin.ToBepuVector(), normalizedDir.ToBepuVector());
            var maxLenFix = length.ToFix64();

            var results = PhysicsResources.GetRayCastResultList();

            bool didHit = _space.RayCast(
                ray,
                maxLenFix,
                entry =>
                {
                    if (entry is EntityCollidable collidable && collidable.Entity?.Tag is AstrumEntity entity)
                    {
                        return entryFilter?.Invoke(entity) ?? true;
                    }
                    return false;
                },
                results);

            if (didHit)
            {
                foreach (var result in results)
                {
                    if (result.HitObject is EntityCollidable collidable && collidable.Entity?.Tag is AstrumEntity entity)
                    {
                        hits.Add(new RaycastHitInfo
                        {
                            Entity = entity,
                            Distance = result.HitData.T.ToFP(),
                            Point = result.HitData.Location.ToTSVector(),
                            Normal = result.HitData.Normal.ToTSVector()
                        });
                    }
                }

                hits.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            }

            PhysicsResources.GiveBack(results);
            return hits;
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

            // 【关键修复】在查询前更新BroadPhase，确保使用最新的AABB
            // 因为我们的物理世界可能不会自动调用Space.Update()，所以需要手动刷新
            _space.BroadPhase.Update();

            // 使用 BroadPhase 进行 AABB 查询
            var candidates = new BEPUutilities.DataStructures.RawList<BroadPhaseEntry>();
            _space.BroadPhase.QueryAccelerator.GetEntries(boundingBox, candidates);

            // 遍历候选者，进行窄相检测
            foreach (var candidate in candidates)
            {
                // 从 EntityCollidable 中获取 Entity
                if (candidate is EntityCollidable collidable && collidable.Entity != null)
                {
                    var bepuEntity = collidable.Entity;
                    
                    // 检查 Tag 是否正确设置
                    if (!(bepuEntity.Tag is AstrumEntity entity))
                    {
                        ASLogger.Instance.Warning($"[QueryBoxOverlap] BEPU Entity Tag is not AstrumEntity (Tag type: {bepuEntity.Tag?.GetType().Name ?? "null"}), skipping");
                        continue;
                    }
                    
                    var candidatePos = bepuEntity.Position.ToTSVector();
                    
                    // 【关键修复】进行窄相精确检测（而不是仅仅依赖AABB重叠）
                    // 支持Box和Capsule形状的精确检测
                    var queryTransform = new BEPUutilities.RigidTransform(bepuCenter, bepuRotation);
                    var targetTransform = new BEPUutilities.RigidTransform(bepuEntity.Position, bepuEntity.Orientation);
                    bool isColliding = false;
                    
                    if (bepuEntity is Box targetBox)
                    {
                        // Box vs Box：使用专用的BoxBoxCollider（更快）
                        isColliding = BEPUphysics.CollisionTests.CollisionAlgorithms.BoxBoxCollider.AreBoxesColliding(
                            queryBox.CollisionInformation.Shape, targetBox.CollisionInformation.Shape, 
                            ref queryTransform, ref targetTransform);
                    }
                    else if (bepuEntity is BEPUphysics.Entities.Prefabs.Capsule targetCapsule)
                    {
                        // Box vs Capsule：使用通用的GJK算法（因为两者都是凸体）
                        var queryBoxShape = queryBox.CollisionInformation.Shape;
                        var targetCapsuleShape = targetCapsule.CollisionInformation.Shape;
                        isColliding = BEPUphysics.CollisionTests.CollisionAlgorithms.GJK.GJKToolbox.AreShapesIntersecting(
                            queryBoxShape, targetCapsuleShape, 
                            ref queryTransform, ref targetTransform);
                    }
                    else
                    {
                        // 其他凸体形状：尝试使用通用GJK算法
                        if (bepuEntity.CollisionInformation?.Shape is BEPUphysics.CollisionShapes.ConvexShapes.ConvexShape targetConvexShape)
                        {
                            var queryBoxShape = queryBox.CollisionInformation.Shape;
                            isColliding = BEPUphysics.CollisionTests.CollisionAlgorithms.GJK.GJKToolbox.AreShapesIntersecting(
                                queryBoxShape, targetConvexShape, 
                                ref queryTransform, ref targetTransform);
                        }
                        else
                        {
                            ASLogger.Instance.Warning($"[QueryBoxOverlap] Candidate Entity={entity.UniqueId} is not a supported convex shape (type: {bepuEntity.GetType().Name}), skipping narrow phase");
                        }
                    }
                    
                    if (isColliding)
                    {
                        // 精确检测通过，添加到结果
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

        /// <summary>
        /// 调试：获取当前注册到物理世界的实体及其位置（TSVector）
        /// </summary>
        public System.Collections.Generic.List<System.ValueTuple<long, TSVector>> GetEntityPositions()
        {
            var list = new System.Collections.Generic.List<System.ValueTuple<long, TSVector>>();
            foreach (var kv in _entityBodies)
            {
                var id = kv.Key;
                var bepuEntity = kv.Value;
                var tsPos = bepuEntity.Position.ToTSVector();
                list.Add(new System.ValueTuple<long, TSVector>(id, tsPos));
            }
            return list;
        }
    }
}

