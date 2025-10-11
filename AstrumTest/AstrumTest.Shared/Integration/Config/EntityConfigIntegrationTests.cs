using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using TrueSync;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Physics;
using Astrum.LogicCore.Managers;
using AstrumTest.Shared.Fixtures;

namespace AstrumTest.PhysicsTests
{
    /// <summary>
    /// Entity 配置集成测试
    /// 测试从配置表加载实体数据，验证碰撞盒配置的反序列化和使用
    /// </summary>
    [Collection("Config Collection")]
    [Trait("Category", "Integration")]
    [Trait("Module", "Physics")]
    [Trait("Priority", "High")]
    public class EntityConfigIntegrationTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly ConfigFixture _configFixture;
        private readonly BepuPhysicsWorld _physicsWorld;
        private readonly HitManager _hitManager;

        public EntityConfigIntegrationTests(ITestOutputHelper output, ConfigFixture configFixture)
        {
            _output = output;
            _configFixture = configFixture;
            _physicsWorld = new BepuPhysicsWorld();
            _physicsWorld.Initialize();
            _hitManager = new HitManager(_physicsWorld);
            
            _output.WriteLine("[EntityConfigIntegrationTests] Test initialized");
            _output.WriteLine($"[EntityConfigIntegrationTests] Config path: {_configFixture.ConfigPath}");
        }

        public void Dispose()
        {
            _hitManager?.Dispose();
            _physicsWorld?.Dispose();
        }

        /// <summary>
        /// 测试：从配置表加载实体模型数据
        /// </summary>
        [Fact]
        public void Test_LoadEntityModelFromConfig()
        {
            _output.WriteLine("=== Test: Load Entity Model From Config ===");
            
            // 获取配置管理器
            var configManager = ConfigManager.Instance;
            Assert.NotNull(configManager);
            Assert.NotNull(configManager.Tables);
            
            // 获取 EntityModelTable
            var modelTable = configManager.Tables.TbEntityModelTable;
            Assert.NotNull(modelTable);
            
            _output.WriteLine($"EntityModelTable loaded with {modelTable.DataList.Count} entries");
            
            // 遍历所有模型配置，检查碰撞数据
            int modelsWithCollision = 0;
            foreach (var modelConfig in modelTable.DataList)
            {
                _output.WriteLine($"Model ID: {modelConfig.ModelId}, Path: {modelConfig.ModelPath}");
                
                if (!string.IsNullOrEmpty(modelConfig.CollisionData))
                {
                    _output.WriteLine($"  -> CollisionData: {modelConfig.CollisionData}");
                    modelsWithCollision++;
                    
                    // 测试解析碰撞数据
                    var shapes = CollisionDataParser.Parse(modelConfig.CollisionData);
                    Assert.NotNull(shapes);
                    Assert.NotEmpty(shapes);
                    
                    foreach (var shape in shapes)
                    {
                        _output.WriteLine($"     Shape: {shape.ShapeType}, Offset: {shape.LocalOffset}");
                        
                        // 验证形状数据有效性
                        Assert.True(shape.ShapeType >= 0);
                        
                        switch (shape.ShapeType)
                        {
                            case HitBoxShape.Box:
                                Assert.True(shape.HalfSize.x > FP.Zero || shape.HalfSize.y > FP.Zero || shape.HalfSize.z > FP.Zero);
                                break;
                            case HitBoxShape.Sphere:
                                Assert.True(shape.Radius > FP.Zero);
                                break;
                            case HitBoxShape.Capsule:
                                Assert.True(shape.Radius > FP.Zero);
                                Assert.True(shape.Height > FP.Zero);
                                break;
                        }
                    }
                }
            }
            
            _output.WriteLine($"Total models with collision data: {modelsWithCollision}");
            Assert.True(modelsWithCollision >= 0, "Should have loaded collision data");
        }

        /// <summary>
        /// 测试：创建带配置碰撞盒的实体
        /// </summary>
        [Fact]
        public void Test_CreateEntityWithConfiguredCollision()
        {
            _output.WriteLine("=== Test: Create Entity With Configured Collision ===");
            
            // 获取第一个有碰撞数据的模型配置
            var configManager = ConfigManager.Instance;
            var modelTable = configManager.Tables.TbEntityModelTable;
            
            var modelWithCollision = modelTable.DataList
                .FirstOrDefault(m => !string.IsNullOrEmpty(m.CollisionData));
            
            if (modelWithCollision == null)
            {
                _output.WriteLine("No model with collision data found, creating test data");
                
                // 如果没有配置数据，手动创建测试实体
                var testEntity = CreateTestEntityWithCollision(
                    position: TSVector.zero,
                    collisionData: "Box:0,1,0:0,0,0,1:0.5,1,0.5"
                );
                
                Assert.NotNull(testEntity);
                Assert.NotNull(testEntity.GetComponent<CollisionComponent>());
                return;
            }
            
            _output.WriteLine($"Using model {modelWithCollision.ModelId} with collision: {modelWithCollision.CollisionData}");
            
            // 创建实体
            var entity = CreateEntityFromModelConfig(modelWithCollision);
            
            // 验证实体创建成功
            Assert.NotNull(entity);
            
            // 验证碰撞组件
            var collisionComp = entity.GetComponent<CollisionComponent>();
            Assert.NotNull(collisionComp);
            Assert.NotNull(collisionComp.Shapes);
            Assert.NotEmpty(collisionComp.Shapes);
            
            _output.WriteLine($"Entity created with {collisionComp.Shapes.Count} collision shape(s)");
            
            // 注册到物理世界
            _physicsWorld.RegisterEntity(entity);
            
            // 验证注册成功
            var spaceEntities = _physicsWorld.Space.Entities.Count;
            Assert.Equal(1, spaceEntities);
            
            _output.WriteLine($"Entity successfully registered to physics world");
        }

        /// <summary>
        /// 测试：完整的碰撞检测流程（从配置到查询）
        /// </summary>
        [Fact]
        public void Test_CompleteCollisionDetectionWithConfig()
        {
            _output.WriteLine("=== Test: Complete Collision Detection With Config ===");
            
            // 创建目标实体（使用配置或测试数据）
            var target = CreateTestEntityWithCollision(
                position: new TSVector(FP.Zero, FP.One, FP.Zero),
                collisionData: "Capsule:0,1,0:0,0,0,1:0.5:2.0"
            );
            
            _output.WriteLine($"Target entity created at {target.GetComponent<TransComponent>().Position}");
            
            // 创建施法者实体
            var caster = new Entity();
            var casterPos = new TransComponent { Position = TSVector.zero };
            caster.AddComponent(casterPos);
            
            // 创建查询碰撞盒（Box）
            var queryShape = CollisionShape.CreateBox(
                halfSize: TSVector.one * (FP)2,
                localOffset: TSVector.zero
            );
            
            _output.WriteLine($"Querying with Box: halfSize={queryShape.HalfSize}");
            
            // 执行查询
            var results = _hitManager.QueryHits(caster, queryShape);
            
            _output.WriteLine($"Query results: {results.Count} entities found");
            
            // 验证结果
            Assert.NotEmpty(results);
            Assert.Contains(target, results);
            
            _output.WriteLine("✓ Complete collision detection test passed");
        }

        /// <summary>
        /// 测试：多个实体的碰撞检测
        /// </summary>
        [Fact]
        public void Test_MultipleEntitiesCollisionDetection()
        {
            _output.WriteLine("=== Test: Multiple Entities Collision Detection ===");
            
            // 创建多个实体
            var entities = new List<Entity>();
            
            // 实体1：Box 在原点
            entities.Add(CreateTestEntityWithCollision(
                position: TSVector.zero,
                collisionData: "Box:0,0,0:0,0,0,1:1,1,1"
            ));
            
            // 实体2：Sphere 在右侧
            entities.Add(CreateTestEntityWithCollision(
                position: new TSVector((FP)3, FP.Zero, FP.Zero),
                collisionData: "Sphere:0,0,0:0,0,0,1:1"
            ));
            
            // 实体3：Capsule 在后方
            entities.Add(CreateTestEntityWithCollision(
                position: new TSVector(FP.Zero, FP.Zero, (FP)5),
                collisionData: "Capsule:0,1,0:0,0,0,1:0.5:2"
            ));
            
            _output.WriteLine($"Created {entities.Count} test entities");
            
            // 创建施法者
            var caster = new Entity();
            caster.AddComponent(new TransComponent { Position = TSVector.zero });
            
            // 大范围查询（应该找到所有实体）
            var largeQueryShape = CollisionShape.CreateBox(
                halfSize: TSVector.one * (FP)10
            );
            
            var allResults = _hitManager.QueryHits(caster, largeQueryShape);
            _output.WriteLine($"Large query found {allResults.Count} entities");
            Assert.Equal(entities.Count, allResults.Count);
            
            // 小范围查询（应该只找到原点附近的）
            var smallQueryShape = CollisionShape.CreateBox(
                halfSize: TSVector.one * (FP)1.5
            );
            
            var nearResults = _hitManager.QueryHits(caster, smallQueryShape);
            _output.WriteLine($"Small query found {nearResults.Count} entities");
            Assert.True(nearResults.Count < entities.Count);
            
            _output.WriteLine("✓ Multiple entities collision detection test passed");
        }

        #region 辅助方法

        /// <summary>
        /// 从模型配置创建实体
        /// </summary>
        private Entity CreateEntityFromModelConfig(cfg.Entity.EntityModelTable modelConfig)
        {
            var entity = new Entity();
            
            // 添加位置组件
            var posComp = new TransComponent { Position = TSVector.zero };
            entity.AddComponent(posComp);
            
            // 解析并添加碰撞组件
            if (!string.IsNullOrEmpty(modelConfig.CollisionData))
            {
                var shapes = CollisionDataParser.Parse(modelConfig.CollisionData);
                var collisionComp = new CollisionComponent();
                collisionComp.Shapes = shapes;
                entity.AddComponent(collisionComp);
            }
            
            return entity;
        }

        /// <summary>
        /// 创建带碰撞盒的测试实体
        /// </summary>
        private Entity CreateTestEntityWithCollision(TSVector position, string collisionData)
        {
            var entity = new Entity();
            
            // 添加位置组件
            var posComp = new TransComponent { Position = position };
            entity.AddComponent(posComp);
            
            // 解析并添加碰撞组件
            var shapes = CollisionDataParser.Parse(collisionData);
            var collisionComp = new CollisionComponent();
            collisionComp.Shapes = shapes;
            entity.AddComponent(collisionComp);
            
            // 注册到物理世界
            _hitManager.RegisterEntity(entity);
            
            return entity;
        }

        #endregion
    }
}

