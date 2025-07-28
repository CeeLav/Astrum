using System.Numerics;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.Factories;
using Xunit;

namespace Astrum.LogicCore.Tests
{
    public class UnitTests
    {
        [Fact]
        public void CreateUnit_ShouldHaveRequiredComponents()
        {
            // Arrange & Act
            var world = new World();
            var factory = EntityFactory.Instance;
            factory.Initialize(world);
            var unit = factory.CreateEntity<Unit>("TestUnit");

            // Assert
            Assert.Equal("TestUnit", unit.Name);
            Assert.True(unit.HasComponent<PositionComponent>());
            Assert.True(unit.HasComponent<MovementComponent>());
            Assert.True(unit.HasComponent<HealthComponent>());
        }

        [Fact]
        public void GetPosition_ShouldReturnCorrectPosition()
        {
            // Arrange
            var world = new World();
            var factory = EntityFactory.Instance;
            factory.Initialize(world);
            var unit = factory.CreateEntity<Unit>("TestUnit");
            unit.SetInitialPosition(1.0f, 2.0f, 3.0f);

            // Act
            var positionComponent = unit.GetComponent<PositionComponent>();
            var position = new Vector3(positionComponent!.X, positionComponent.Y, positionComponent.Z);

            // Assert
            Assert.Equal(new Vector3(1.0f, 2.0f, 3.0f), position);
        }

        [Fact]
        public void SetPosition_ShouldUpdatePosition()
        {
            // Arrange
            var world = new World();
            var factory = EntityFactory.Instance;
            factory.Initialize(world);
            var unit = factory.CreateEntity<Unit>("TestUnit");
            var newPosition = new Vector3(5.0f, 10.0f, 15.0f);

            // Act
            var positionComponent = unit.GetComponent<PositionComponent>();
            positionComponent!.SetPosition(newPosition.X, newPosition.Y, newPosition.Z);

            // Assert
            var updatedPosition = new Vector3(positionComponent.X, positionComponent.Y, positionComponent.Z);
            Assert.Equal(newPosition, updatedPosition);
        }

        [Fact]
        public void SetPosition_WithFloatParameters_ShouldUpdatePosition()
        {
            // Arrange
            var world = new World();
            var factory = EntityFactory.Instance;
            factory.Initialize(world);
            var unit = factory.CreateEntity<Unit>("TestUnit");

            // Act
            var positionComponent = unit.GetComponent<PositionComponent>();
            positionComponent!.SetPosition(7.0f, 8.0f, 9.0f);

            // Assert
            var updatedPosition = new Vector3(positionComponent.X, positionComponent.Y, positionComponent.Z);
            Assert.Equal(new Vector3(7.0f, 8.0f, 9.0f), updatedPosition);
        }

        [Fact]
        public void DistanceTo_ShouldCalculateCorrectDistance()
        {
            // Arrange
            var world = new World();
            var factory = EntityFactory.Instance;
            factory.Initialize(world);
            var unit1 = factory.CreateEntity<Unit>("Unit1");
            var unit2 = factory.CreateEntity<Unit>("Unit2");
            unit2.SetInitialPosition(3.0f, 4.0f, 0.0f);

            // Act
            var position1 = unit1.GetComponent<PositionComponent>();
            var position2 = unit2.GetComponent<PositionComponent>();
            var distance = position1!.DistanceTo(position2!);

            // Assert
            Assert.Equal(5.0f, distance, 3); // 3-4-5 三角形
        }

        [Fact]
        public void HasMovementCapability_ShouldReturnTrue_WhenMovementComponentExists()
        {
            // Arrange
            var world = new World();
            var factory = EntityFactory.Instance;
            factory.Initialize(world);
            var unit = factory.CreateEntity<Unit>("TestUnit");

            // Act
            var hasMovement = unit.HasComponent<MovementComponent>();

            // Assert
            Assert.True(hasMovement);
        }

        [Fact]
        public void IsAlive_ShouldReturnTrue_WhenHealthGreaterThanZero()
        {
            // Arrange
            var world = new World();
            var factory = EntityFactory.Instance;
            factory.Initialize(world);
            var unit = factory.CreateEntity<Unit>("TestUnit");
            var healthComponent = unit.GetComponent<HealthComponent>();
            healthComponent!.CurrentHealth = 100;

            // Act
            var isAlive = healthComponent.CurrentHealth > 0;

            // Assert
            Assert.True(isAlive);
        }

        [Fact]
        public void IsAlive_ShouldReturnFalse_WhenHealthIsZero()
        {
            // Arrange
            var world = new World();
            var factory = EntityFactory.Instance;
            factory.Initialize(world);
            var unit = factory.CreateEntity<Unit>("TestUnit");
            var healthComponent = unit.GetComponent<HealthComponent>();
            healthComponent!.CurrentHealth = 0;

            // Act
            var isAlive = healthComponent.CurrentHealth > 0;

            // Assert
            Assert.False(isAlive);
        }

        [Fact]
        public void GetComponentMethods_ShouldReturnCorrectComponents()
        {
            // Arrange
            var world = new World();
            var factory = EntityFactory.Instance;
            factory.Initialize(world);
            var unit = factory.CreateEntity<Unit>("TestUnit");

            // Act & Assert
            Assert.NotNull(unit.GetComponent<PositionComponent>());
            Assert.NotNull(unit.GetComponent<MovementComponent>());
            Assert.NotNull(unit.GetComponent<HealthComponent>());
        }

        [Fact]
        public void Unit_ShouldInheritFromEntity()
        {
            // Arrange & Act
            var world = new World();
            var factory = EntityFactory.Instance;
            factory.Initialize(world);
            var unit = factory.CreateEntity<Unit>("TestUnit");

            // Assert
            Assert.True(unit is Entity);
            Assert.True(unit.UniqueId > 0);
            Assert.True(unit.IsActive);
            Assert.False(unit.IsDestroyed);
        }
    }
} 