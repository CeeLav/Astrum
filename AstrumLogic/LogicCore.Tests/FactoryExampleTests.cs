using Astrum.LogicCore.Core;
using Astrum.LogicCore.Factories;
using Astrum.LogicCore.Components;
using Xunit;

namespace Astrum.LogicCore.Tests
{
    public class FactoryExampleTests
    {
        [Fact]
        public void EntityFactory_ShouldAutomaticallyBuildComponents_ForUnit()
        {
            // Arrange
            var world = new World();
            var factory = EntityFactory.Instance;
            factory.Initialize(world);

            // Act
            var unit = factory.CreateEntity<Unit>("TestUnit");

            // Assert - Unit 应该自动包含所需的组件
            Assert.True(unit.HasComponent<PositionComponent>());
            Assert.True(unit.HasComponent<MovementComponent>());
            Assert.True(unit.HasComponent<HealthComponent>());

            // 验证组件已正确挂载到实体
            var positionComponent = unit.GetComponent<PositionComponent>();
            var movementComponent = unit.GetComponent<MovementComponent>();
            var healthComponent = unit.GetComponent<HealthComponent>();

            Assert.NotNull(positionComponent);
            Assert.NotNull(movementComponent);
            Assert.NotNull(healthComponent);

            // 验证组件已正确关联到实体
            Assert.Equal(unit.UniqueId, positionComponent.EntityId);
            Assert.Equal(unit.UniqueId, movementComponent.EntityId);
            Assert.Equal(unit.UniqueId, healthComponent.EntityId);
        }

        [Fact]
        public void EntityFactory_ShouldCreateBasicEntity_WithoutComponents()
        {
            // Arrange
            var world = new World();
            var factory = EntityFactory.Instance;
            factory.Initialize(world);

            // Act
            var entity = factory.CreateEntity("BasicEntity");

            // Assert - 基础 Entity 不应该有组件
            Assert.False(entity.HasComponent<PositionComponent>());
            Assert.False(entity.HasComponent<MovementComponent>());
            Assert.False(entity.HasComponent<HealthComponent>());
        }

        [Fact]
        public void Unit_ShouldDefineRequiredComponents()
        {
            // Arrange
            var unit = new Unit();

            // Act
            var requiredComponents = unit.GetRequiredComponentTypes();

            // Assert
            Assert.Contains(typeof(PositionComponent), requiredComponents);
            Assert.Contains(typeof(MovementComponent), requiredComponents);
            Assert.Contains(typeof(HealthComponent), requiredComponents);
            Assert.Equal(3, requiredComponents.Length);
        }

        [Fact]
        public void Entity_ShouldHaveEmptyComponentList_ByDefault()
        {
            // Arrange
            var entity = new Entity();

            // Act
            var requiredComponents = entity.GetRequiredComponentTypes();

            // Assert
            Assert.Empty(requiredComponents);
        }
    }
} 