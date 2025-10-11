using System;
using System.Collections.Generic;
using Xunit;
using TrueSync;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Physics;

namespace AstrumTest.PhysicsTests
{
    /// <summary>
    /// HitManager 测试
    /// 验证碰撞检测功能的正确性
    /// </summary>
    [Trait("TestLevel", "Component")]   // 组件测试：单模块 + 物理引擎依赖
    [Trait("Category", "Unit")]
    [Trait("Module", "Physics")]
    [Trait("Priority", "High")]
    public class HitManagerTests : IDisposable
    {
        private readonly HitManager _hitManager;

        public HitManagerTests()
        {
            _hitManager = new HitManager();
        }

        public void Dispose()
        {
            _hitManager?.Dispose();
        }

        #region 辅助方法

        /// <summary>
        /// 创建测试用实体（带 CollisionComponent）
        /// </summary>
        private Entity CreateTestEntity(long uniqueId, TSVector position, CollisionShape shape)
        {
            // 创建实体
            var entity = new Entity();
            
            // 添加位置组件
            var posComp = new TransComponent { Position = position };
            entity.AddComponent(posComp);

            // 添加碰撞组件
            var collisionComp = new CollisionComponent();
            collisionComp.Shapes = new List<CollisionShape> { shape };
            entity.AddComponent(collisionComp);

            // 注册到物理世界（现在会自动从 CollisionComponent 获取）
            _hitManager.RegisterEntity(entity);

            return entity;
        }

        /// <summary>
        /// 创建 Box 实体
        /// </summary>
        private Entity CreateBoxEntity(long id, TSVector position, TSVector halfSize)
        {
            var shape = new CollisionShape
            {
                ShapeType = HitBoxShape.Box,
                LocalOffset = TSVector.zero,
                LocalRotation = TSQuaternion.identity,
                HalfSize = halfSize,
                Radius = FP.Zero,
                Height = FP.Zero
            };

            return CreateTestEntity(id, position, shape);
        }

        /// <summary>
        /// 创建 Sphere 实体
        /// </summary>
        private Entity CreateSphereEntity(long id, TSVector position, FP radius)
        {
            var shape = new CollisionShape
            {
                ShapeType = HitBoxShape.Sphere,
                LocalOffset = TSVector.zero,
                LocalRotation = TSQuaternion.identity,
                HalfSize = TSVector.zero,
                Radius = radius,
                Height = FP.Zero
            };

            return CreateTestEntity(id, position, shape);
        }

        #endregion

        #region Box Overlap 测试

        [Fact]
        public void Test_BoxOverlap_Basic_Hit()
        {
            // Arrange - 创建目标实体
            var target = CreateBoxEntity(1, new TSVector(FP.Zero, FP.Zero, FP.Zero), TSVector.one);

            // Arrange - 创建施法者和命中盒
            var caster = CreateBoxEntity(2, new TSVector((FP)2, FP.Zero, FP.Zero), TSVector.one);
            var hitBox = CollisionShape.CreateBox(TSVector.one * (FP)2); // 大命中盒，应该能命中目标

            // Act
            var hits = _hitManager.QueryHits(caster, hitBox);

            // Assert
            Assert.NotEmpty(hits);
            Assert.Contains(hits, e => e.UniqueId == target.UniqueId);
        }

        [Fact]
        public void Test_BoxOverlap_No_Hit()
        {
            // Arrange - 创建目标实体（距离很远）
            var target = CreateBoxEntity(1, new TSVector((FP)100, FP.Zero, FP.Zero), TSVector.one);

            // Arrange - 创建施法者和命中盒
            var caster = CreateBoxEntity(2, TSVector.zero, TSVector.one);
            var hitBox = CollisionShape.CreateBox(TSVector.one); // 小命中盒，不应该命中远处目标

            // Act
            var hits = _hitManager.QueryHits(caster, hitBox);

            // Assert
            Assert.DoesNotContain(hits, e => e.UniqueId == target.UniqueId);
        }

        [Fact]
        public void Test_BoxOverlap_Multiple_Targets()
        {
            // Arrange - 创建多个目标实体
            var target1 = CreateBoxEntity(1, new TSVector(FP.One, FP.Zero, FP.Zero), TSVector.one);
            var target2 = CreateBoxEntity(2, new TSVector((FP)(-1), FP.Zero, FP.Zero), TSVector.one);
            var target3 = CreateBoxEntity(3, new TSVector(FP.Zero, FP.One, FP.Zero), TSVector.one);

            // Arrange - 创建施法者和大范围命中盒
            var caster = CreateBoxEntity(4, TSVector.zero, TSVector.one);
            var hitBox = CollisionShape.CreateBox(TSVector.one * (FP)3); // 大命中盒

            // Act
            var hits = _hitManager.QueryHits(caster, hitBox);

            // Assert
            Assert.True(hits.Count >= 3);
            Assert.Contains(hits, e => e.UniqueId == target1.UniqueId);
            Assert.Contains(hits, e => e.UniqueId == target2.UniqueId);
            Assert.Contains(hits, e => e.UniqueId == target3.UniqueId);
        }

        [Fact]
        public void Test_BoxOverlap_Exclude_Self()
        {
            // Arrange - 创建施法者
            var caster = CreateBoxEntity(1, TSVector.zero, TSVector.one);
            var hitBox = CollisionShape.CreateBox(TSVector.one * (FP)2);

            // Act
            var hits = _hitManager.QueryHits(caster, hitBox);

            // Assert - 施法者不应该命中自己
            Assert.DoesNotContain(hits, e => e.UniqueId == caster.UniqueId);
        }

        #endregion

        #region Sphere Overlap 测试

        [Fact]
        public void Test_SphereOverlap_Basic_Hit()
        {
            // Arrange - 创建目标实体
            var target = CreateSphereEntity(1, new TSVector(FP.One, FP.Zero, FP.Zero), FP.One);

            // Arrange - 创建施法者和命中盒
            var caster = CreateSphereEntity(2, TSVector.zero, FP.One);
            var hitBox = CollisionShape.CreateSphere((FP)2); // 大半径，应该能命中

            // Act
            var hits = _hitManager.QueryHits(caster, hitBox);

            // Assert
            Assert.NotEmpty(hits);
            Assert.Contains(hits, e => e.UniqueId == target.UniqueId);
        }

        [Fact]
        public void Test_SphereOverlap_No_Hit()
        {
            // Arrange - 创建目标实体（距离很远）
            var target = CreateSphereEntity(1, new TSVector((FP)10, FP.Zero, FP.Zero), FP.One);

            // Arrange - 创建施法者和命中盒
            var caster = CreateSphereEntity(2, TSVector.zero, FP.One);
            var hitBox = CollisionShape.CreateSphere(FP.One); // 小半径，不应该命中

            // Act
            var hits = _hitManager.QueryHits(caster, hitBox);

            // Assert
            Assert.DoesNotContain(hits, e => e.UniqueId == target.UniqueId);
        }

        #endregion

        #region 过滤器测试

        [Fact]
        public void Test_CollisionFilter_ExcludeEntityIds()
        {
            // Arrange - 创建多个目标
            var target1 = CreateBoxEntity(1, new TSVector(FP.One, FP.Zero, FP.Zero), TSVector.one);
            var target2 = CreateBoxEntity(2, new TSVector((FP)(-1), FP.Zero, FP.Zero), TSVector.one);

            // Arrange - 创建施法者和过滤器
            var caster = CreateBoxEntity(3, TSVector.zero, TSVector.one);
            var hitBox = CollisionShape.CreateBox(TSVector.one * (FP)3);
            var filter = new CollisionFilter
            {
                ExcludedEntityIds = new HashSet<long> { target1.UniqueId } // 排除 target1
            };

            // Act
            var hits = _hitManager.QueryHits(caster, hitBox, filter);

            // Assert
            Assert.DoesNotContain(hits, e => e.UniqueId == target1.UniqueId);
            Assert.Contains(hits, e => e.UniqueId == target2.UniqueId);
        }

        [Fact]
        public void Test_CollisionFilter_CustomFilter()
        {
            // Arrange - 创建多个目标
            var target1 = CreateBoxEntity(1, new TSVector(FP.One, FP.Zero, FP.Zero), TSVector.one);
            var target2 = CreateBoxEntity(2, new TSVector((FP)(-1), FP.Zero, FP.Zero), TSVector.one);

            // Arrange - 创建施法者和自定义过滤器（只允许 target2，排除 target1）
            var caster = CreateBoxEntity(3, TSVector.zero, TSVector.one);
            var hitBox = CollisionShape.CreateBox(TSVector.one * (FP)3);
            var filter = new CollisionFilter
            {
                CustomFilter = (entity) => entity.UniqueId == target2.UniqueId // 只允许 target2
            };

            // Act
            var hits = _hitManager.QueryHits(caster, hitBox, filter);

            // Assert
            Assert.DoesNotContain(hits, e => e.UniqueId == target1.UniqueId); // target1 被自定义过滤器排除
            Assert.Contains(hits, e => e.UniqueId == target2.UniqueId); // target2 通过过滤器
        }

        #endregion

        #region 去重测试

        [Fact]
        public void Test_Deduplication_Same_SkillInstance()
        {
            // Arrange - 创建目标
            var target = CreateBoxEntity(1, TSVector.zero, TSVector.one);

            // Arrange - 创建施法者
            var caster = CreateBoxEntity(2, new TSVector(FP.One, FP.Zero, FP.Zero), TSVector.one);
            var hitBox = CollisionShape.CreateBox(TSVector.one * (FP)3);
            var skillInstanceId = 1001;

            // Act - 第一次查询
            var hits1 = _hitManager.QueryHits(caster, hitBox, null, skillInstanceId);

            // Act - 第二次查询（同一技能实例）
            var hits2 = _hitManager.QueryHits(caster, hitBox, null, skillInstanceId);

            // Assert - 第一次应该命中，第二次因为去重不应该命中
            Assert.Contains(hits1, e => e.UniqueId == target.UniqueId);
            Assert.DoesNotContain(hits2, e => e.UniqueId == target.UniqueId);
        }

        [Fact]
        public void Test_ClearHitCache()
        {
            // Arrange - 创建目标
            var target = CreateBoxEntity(1, TSVector.zero, TSVector.one);

            // Arrange - 创建施法者
            var caster = CreateBoxEntity(2, new TSVector(FP.One, FP.Zero, FP.Zero), TSVector.one);
            var hitBox = CollisionShape.CreateBox(TSVector.one * (FP)3);
            var skillInstanceId = 1001;

            // Act - 第一次查询
            var hits1 = _hitManager.QueryHits(caster, hitBox, null, skillInstanceId);

            // Act - 清除缓存
            _hitManager.ClearHitCache(skillInstanceId);

            // Act - 清除后再次查询
            var hits2 = _hitManager.QueryHits(caster, hitBox, null, skillInstanceId);

            // Assert - 清除缓存后应该能再次命中
            Assert.Contains(hits1, e => e.UniqueId == target.UniqueId);
            Assert.Contains(hits2, e => e.UniqueId == target.UniqueId);
        }

        #endregion

        #region 边界测试

        [Fact]
        public void Test_Null_Caster()
        {
            // Arrange
            var hitBox = CollisionShape.CreateBox(TSVector.one);

            // Act
            var hits = _hitManager.QueryHits(null, hitBox);

            // Assert - 应该返回空列表而不是抛出异常
            Assert.NotNull(hits);
            Assert.Empty(hits);
        }

        [Fact]
        public void Test_Empty_World()
        {
            // Arrange - 不创建任何实体，只创建施法者
            var caster = new Entity();
            var posComp = new TransComponent { Position = TSVector.zero };
            caster.AddComponent(posComp);

            var hitBox = CollisionShape.CreateBox(TSVector.one * (FP)100);

            // Act
            var hits = _hitManager.QueryHits(caster, hitBox);

            // Assert - 应该返回空列表（物理世界是空的）
            Assert.NotNull(hits);
            Assert.Empty(hits);
        }

        #endregion

        #region 确定性测试

        [Fact]
        public void Test_Determinism_Same_Query_Same_Result()
        {
            // Arrange - 创建固定场景
            var target1 = CreateBoxEntity(1, new TSVector(FP.One, FP.Zero, FP.Zero), TSVector.one);
            var target2 = CreateBoxEntity(2, new TSVector((FP)(-1), FP.Zero, FP.Zero), TSVector.one);
            var caster = CreateBoxEntity(3, TSVector.zero, TSVector.one);
            var hitBox = CollisionShape.CreateBox(TSVector.one * (FP)3);

            // Act - 多次执行相同查询
            var hits1 = _hitManager.QueryHits(caster, hitBox);
            var hits2 = _hitManager.QueryHits(caster, hitBox);
            var hits3 = _hitManager.QueryHits(caster, hitBox);

            // Assert - 所有结果应该相同
            Assert.Equal(hits1.Count, hits2.Count);
            Assert.Equal(hits1.Count, hits3.Count);

            foreach (var hit in hits1)
            {
                Assert.Contains(hits2, e => e.UniqueId == hit.UniqueId);
                Assert.Contains(hits3, e => e.UniqueId == hit.UniqueId);
            }
        }

        #endregion
    }
}

