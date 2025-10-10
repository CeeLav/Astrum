using System;
using Xunit;
using Xunit.Abstractions;
using TrueSync;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Physics;

namespace AstrumTest.PhysicsTests
{
    public class DebugPhysicsTest : IDisposable
    {
        private readonly BepuPhysicsWorld _physicsWorld;
        private readonly ITestOutputHelper _output;

        public DebugPhysicsTest(ITestOutputHelper output)
        {
            _output = output;
            _physicsWorld = new BepuPhysicsWorld();
            _physicsWorld.Initialize();
        }

        public void Dispose()
        {
            _physicsWorld?.Dispose();
        }

        [Fact]
        public void Test_DirectPhysicsWorldQuery()
        {
            // 创建一个实体
            var entity = new Entity();
            var posComp = new PositionComponent { Position = new TSVector(FP.Zero, FP.Zero, FP.Zero) };
            entity.AddComponent(posComp);

            // 添加碰撞组件
            var shape = new CollisionShape
            {
                ShapeType = HitBoxShape.Box,
                LocalOffset = TSVector.zero,
                LocalRotation = TSQuaternion.identity,
                HalfSize = TSVector.one,
                Radius = FP.Zero,
                Height = FP.Zero
            };
            var collisionComp = new CollisionComponent();
            collisionComp.Shapes = new System.Collections.Generic.List<CollisionShape> { shape };
            entity.AddComponent(collisionComp);

            // 注册到物理世界（自动从 CollisionComponent 获取）
            _physicsWorld.RegisterEntity(entity);

            // 检查 Space 中是否有实体
            var spaceEntitiesCount = _physicsWorld.Space.Entities.Count;
            Assert.Equal(1, spaceEntitiesCount);  // 应该有1个实体

            // 关键修复：调用 Space.Update() 来更新 BroadPhase 索引
            _physicsWorld.Space.Update();

            // 手动调试 BroadPhase 查询
            var queryCenter = new BEPUutilities.Vector3(0, 0, 0);
            var queryHalfSize = new BEPUutilities.Vector3(5, 5, 5);
            var queryBox = new BEPUphysics.Entities.Prefabs.Box(queryCenter, 10, 10, 10);
            queryBox.CollisionInformation.UpdateBoundingBox();
            var boundingBox = queryBox.CollisionInformation.BoundingBox;

            var candidates = new BEPUutilities.DataStructures.RawList<BEPUphysics.BroadPhaseEntries.BroadPhaseEntry>();
            _physicsWorld.Space.BroadPhase.QueryAccelerator.GetEntries(boundingBox, candidates);

            _output.WriteLine($"Space Entities Count: {spaceEntitiesCount}");
            _output.WriteLine($"BoundingBox: {boundingBox.Min} to {boundingBox.Max}");
            _output.WriteLine($"Candidates Count: {candidates.Count}");
            
            // 遍历候选者
            int validEntities = 0;
            foreach (var candidate in candidates)
            {
                _output.WriteLine($"Candidate Type: {candidate.GetType().Name}");
                if (candidate is BEPUphysics.BroadPhaseEntries.MobileCollidables.EntityCollidable collidable)
                {
                    _output.WriteLine($"  -> EntityCollidable.Entity: {collidable.Entity?.GetType().Name}");
                    if (collidable.Entity != null && collidable.Entity.Tag is Entity)
                    {
                        validEntities++;
                    }
                }
            }
            
            _output.WriteLine($"Valid Entities: {validEntities}");
            
            // 使用原始方法查询
            var results = _physicsWorld.QueryBoxOverlap(
                TSVector.zero,
                TSVector.one * (FP)5,
                TSQuaternion.identity
            );
            _output.WriteLine($"Query Results Count: {results.Count}");
            
            // 断言应该找到1个实体
            Assert.NotEmpty(results);
        }
    }
}

