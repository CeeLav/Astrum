using System;
using System.Collections.Generic;
using Astrum.LogicCore.Capabilities;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.FrameSync;
using Astrum.LogicCore.Systems;
using Xunit;

namespace AstrumTest.Shared.Unit.ECS
{
    /// <summary>
    /// CapabilitySystem 基础功能测试
    /// </summary>
    [Trait("TestLevel", "Component")]
    [Trait("Category", "Unit")]
    [Trait("Module", "ECS")]
    public class CapabilitySystemTests
    {
        private World CreateTestWorld()
        {
            var world = new World();
            world.Initialize(0);
            return world;
        }

        private Entity CreateTestEntity(World world)
        {
            var entity = new Entity
            {
                UniqueId = 1,
                Name = "TestEntity",
                ArchetypeName = "Test",
                EntityConfigId = 1,
                IsActive = true,
                IsDestroyed = false,
                CreationTime = DateTime.Now,
                World = world
            };
            world.Entities[entity.UniqueId] = entity;
            return entity;
        }

        [Fact]
        public void Test_CapabilitySystem_Initialization()
        {
            // Arrange & Act
            var world = CreateTestWorld();

            // Assert
            Assert.NotNull(world.CapabilitySystem);
            Assert.Equal(world, world.CapabilitySystem.World);
        }

        [Fact]
        public void Test_CapabilityState_Enable()
        {
            // Arrange
            var world = CreateTestWorld();
            var entity = CreateTestEntity(world);
            var typeId = MovementCapabilityV2.TypeId;

            // Act - 启用 Capability
            entity.CapabilityStates[typeId] = new CapabilityState
            {
                IsActive = false,
                ActiveDuration = 0,
                DeactiveDuration = 0,
                CustomData = new Dictionary<string, object>()
            };
            world.CapabilitySystem.RegisterEntityCapability(entity.UniqueId, typeId);

            // Assert
            Assert.True(entity.CapabilityStates.ContainsKey(typeId));
            Assert.True(world.CapabilitySystem.TypeIdToEntityIds.ContainsKey(typeId));
            Assert.Contains(entity.UniqueId, world.CapabilitySystem.TypeIdToEntityIds[typeId]);
        }

        [Fact]
        public void Test_CapabilityState_Disable()
        {
            // Arrange
            var world = CreateTestWorld();
            var entity = CreateTestEntity(world);
            var typeId = MovementCapabilityV2.TypeId;

            entity.CapabilityStates[typeId] = new CapabilityState
            {
                IsActive = false,
                ActiveDuration = 0,
                DeactiveDuration = 0,
                CustomData = new Dictionary<string, object>()
            };
            world.CapabilitySystem.RegisterEntityCapability(entity.UniqueId, typeId);

            // Act - 禁用 Capability（移除状态）
            entity.CapabilityStates.Remove(typeId);
            world.CapabilitySystem.UnregisterEntityCapability(entity.UniqueId, typeId);

            // Assert
            Assert.False(entity.CapabilityStates.ContainsKey(typeId));
            Assert.False(world.CapabilitySystem.TypeIdToEntityIds.ContainsKey(typeId) &&
                         world.CapabilitySystem.TypeIdToEntityIds[typeId].Contains(entity.UniqueId));
        }

        [Fact]
        public void Test_Tag_Disable()
        {
            // Arrange
            var world = CreateTestWorld();
            var entity = CreateTestEntity(world);
            var instigatorId = 999L;
            var movementCapability = CapabilitySystem.GetCapability(typeof(MovementCapabilityV2));
            Assert.NotNull(movementCapability);

            var typeId = movementCapability.TypeId;
            entity.CapabilityStates[typeId] = new CapabilityState
            {
                IsActive = true,
                ActiveDuration = 0,
                DeactiveDuration = 0,
                CustomData = new Dictionary<string, object>()
            };
            world.CapabilitySystem.RegisterEntityCapability(entity.UniqueId, typeId);

            // Act - 禁用 Movement Tag
            world.CapabilitySystem.DisableCapabilitiesByTag(entity, CapabilityTag.Movement, instigatorId);

            // Assert
            Assert.True(world.CapabilitySystem.IsTagDisabled(entity, CapabilityTag.Movement, instigatorId));
            Assert.Contains(instigatorId, entity.DisabledTags[CapabilityTag.Movement]);
        }

        [Fact]
        public void Test_Tag_Enable()
        {
            // Arrange
            var world = CreateTestWorld();
            var entity = CreateTestEntity(world);
            var instigatorId = 999L;
            var movementCapability = CapabilitySystem.GetCapability(typeof(MovementCapabilityV2));
            Assert.NotNull(movementCapability);

            var typeId = movementCapability.TypeId;
            entity.CapabilityStates[typeId] = new CapabilityState
            {
                IsActive = true,
                ActiveDuration = 0,
                DeactiveDuration = 0,
                CustomData = new Dictionary<string, object>()
            };
            world.CapabilitySystem.RegisterEntityCapability(entity.UniqueId, typeId);

            // 先禁用
            world.CapabilitySystem.DisableCapabilitiesByTag(entity, CapabilityTag.Movement, instigatorId);
            Assert.True(world.CapabilitySystem.IsTagDisabled(entity, CapabilityTag.Movement, instigatorId));

            // Act - 启用 Tag
            world.CapabilitySystem.EnableCapabilitiesByTag(entity, CapabilityTag.Movement, instigatorId);

            // Assert
            Assert.False(world.CapabilitySystem.IsTagDisabled(entity, CapabilityTag.Movement, instigatorId));
            Assert.False(entity.DisabledTags.ContainsKey(CapabilityTag.Movement) &&
                         entity.DisabledTags[CapabilityTag.Movement].Contains(instigatorId));
        }

        [Fact]
        public void Test_ShouldActivate_WithRequiredComponents()
        {
            // Arrange
            var world = CreateTestWorld();
            var entity = CreateTestEntity(world);
            var movementCapability = CapabilitySystem.GetCapability(typeof(MovementCapabilityV2)) as MovementCapabilityV2;
            Assert.NotNull(movementCapability);

            var typeId = movementCapability.TypeId;
            entity.CapabilityStates[typeId] = new CapabilityState
            {
                IsActive = false,
                ActiveDuration = 0,
                DeactiveDuration = 0,
                CustomData = new Dictionary<string, object>()
            };
            world.CapabilitySystem.RegisterEntityCapability(entity.UniqueId, typeId);

            // Act & Assert - 没有组件时应该返回 false
            Assert.False(movementCapability.ShouldActivate(entity));

            // 添加必需组件
            var inputComponent = new LSInputComponent();
            inputComponent.EntityId = entity.UniqueId;
            entity.Components.Add(inputComponent);
            
            var movementComponent = new MovementComponent();
            movementComponent.EntityId = entity.UniqueId;
            entity.Components.Add(movementComponent);
            
            var transComponent = new TransComponent();
            transComponent.EntityId = entity.UniqueId;
            entity.Components.Add(transComponent);

            // 现在应该可以激活
            Assert.True(movementCapability.ShouldActivate(entity));
        }

        [Fact]
        public void Test_ShouldDeactivate_WhenComponentRemoved()
        {
            // Arrange
            var world = CreateTestWorld();
            var entity = CreateTestEntity(world);
            var movementCapability = CapabilitySystem.GetCapability(typeof(MovementCapabilityV2)) as MovementCapabilityV2;
            Assert.NotNull(movementCapability);

            var typeId = movementCapability.TypeId;
            entity.CapabilityStates[typeId] = new CapabilityState
            {
                IsActive = true,
                ActiveDuration = 10,
                DeactiveDuration = 0,
                CustomData = new Dictionary<string, object>()
            };
            world.CapabilitySystem.RegisterEntityCapability(entity.UniqueId, typeId);

            // 添加必需组件
            var inputComponent = new LSInputComponent();
            inputComponent.EntityId = entity.UniqueId;
            entity.Components.Add(inputComponent);
            
            var movementComponent = new MovementComponent();
            movementComponent.EntityId = entity.UniqueId;
            entity.Components.Add(movementComponent);
            
            var transComponent = new TransComponent();
            transComponent.EntityId = entity.UniqueId;
            entity.Components.Add(transComponent);

            // 初始状态应该不要求停用
            Assert.False(movementCapability.ShouldDeactivate(entity));

            // Act - 移除必需组件
            entity.Components.Remove(inputComponent);

            // Assert - 现在应该要求停用
            Assert.True(movementCapability.ShouldDeactivate(entity));
        }

        [Fact]
        public void Test_OnAttached_InitializesCustomData()
        {
            // Arrange
            var world = CreateTestWorld();
            var entity = CreateTestEntity(world);
            var movementCapability = CapabilitySystem.GetCapability(typeof(MovementCapabilityV2)) as MovementCapabilityV2;
            Assert.NotNull(movementCapability);

            var typeId = movementCapability.TypeId;

            // Act
            movementCapability.OnAttached(entity);

            // Assert
            Assert.True(entity.CapabilityStates.ContainsKey(typeId));
            var state = entity.CapabilityStates[typeId];
            Assert.NotNull(state.CustomData);
            Assert.True(state.CustomData.ContainsKey("MovementThreshold"));
        }

        [Fact]
        public void Test_UnregisterEntity_CleansUpAllCapabilities()
        {
            // Arrange
            var world = CreateTestWorld();
            var entity = CreateTestEntity(world);
            var movementCapability = CapabilitySystem.GetCapability(typeof(MovementCapabilityV2));
            Assert.NotNull(movementCapability);

            var typeId = movementCapability.TypeId;
            entity.CapabilityStates[typeId] = new CapabilityState
            {
                IsActive = false,
                ActiveDuration = 0,
                DeactiveDuration = 0,
                CustomData = new Dictionary<string, object>()
            };
            world.CapabilitySystem.RegisterEntityCapability(entity.UniqueId, typeId);

            // Act
            world.CapabilitySystem.UnregisterEntity(entity.UniqueId);

            // Assert
            Assert.False(world.CapabilitySystem.TypeIdToEntityIds.ContainsKey(typeId) &&
                         world.CapabilitySystem.TypeIdToEntityIds[typeId].Contains(entity.UniqueId));
        }

        [Fact]
        public void Test_Update_ProcessesOnlyEntitiesWithCapability()
        {
            // Arrange
            var world = CreateTestWorld();
            var entity1 = CreateTestEntity(world);
            entity1.UniqueId = 1;
            var entity2 = new Entity
            {
                UniqueId = 2,
                Name = "TestEntity2",
                ArchetypeName = "Test",
                EntityConfigId = 2,
                IsActive = true,
                IsDestroyed = false,
                CreationTime = DateTime.Now,
                World = world
            };
            world.Entities[entity2.UniqueId] = entity2;

            var movementCapability = CapabilitySystem.GetCapability(typeof(MovementCapabilityV2));
            Assert.NotNull(movementCapability);

            var typeId = movementCapability.TypeId;
            
            // 只给 entity1 注册 Capability
            entity1.CapabilityStates[typeId] = new CapabilityState
            {
                IsActive = false,
                ActiveDuration = 0,
                DeactiveDuration = 0,
                CustomData = new Dictionary<string, object>()
            };
            world.CapabilitySystem.RegisterEntityCapability(entity1.UniqueId, typeId);

            // Act
            world.CapabilitySystem.Update(world);

            // Assert - 系统应该只处理拥有该 Capability 的实体
            // （这里主要是验证不会因为 entity2 没有 Capability 而报错）
            Assert.True(world.CapabilitySystem.TypeIdToEntityIds.ContainsKey(typeId));
            Assert.Contains(entity1.UniqueId, world.CapabilitySystem.TypeIdToEntityIds[typeId]);
            Assert.DoesNotContain(entity2.UniqueId, 
                world.CapabilitySystem.TypeIdToEntityIds.ContainsKey(typeId) 
                    ? world.CapabilitySystem.TypeIdToEntityIds[typeId] 
                    : new HashSet<long>());
        }
    }
}

