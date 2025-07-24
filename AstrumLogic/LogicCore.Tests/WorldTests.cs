using Xunit;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.FrameSync;

namespace Astrum.LogicCore.Tests
{
    /// <summary>
    /// 世界相关的单元测试
    /// </summary>
    public class WorldTests
    {
        [Fact]
        public void CreateEntity_ShouldAddEntityToWorld()
        {
            // Arrange
            var world = new World { WorldId = 1, Name = "TestWorld" };

            // Act
            var entity = world.CreateEntity("TestEntity");

            // Assert
            Assert.NotNull(entity);
            Assert.Equal("TestEntity", entity.Name);
            Assert.True(world.Entities.ContainsKey(entity.UniqueId));
            Assert.True(entity.IsActive);
            Assert.False(entity.IsDestroyed);
        }

        [Fact]
        public void DestroyEntity_ShouldRemoveEntityFromWorld()
        {
            // Arrange
            var world = new World();
            var entity = world.CreateEntity("TestEntity");
            long entityId = entity.UniqueId;

            // Act
            world.DestroyEntity(entityId);

            // Assert
            Assert.False(world.Entities.ContainsKey(entityId));
            Assert.True(entity.IsDestroyed);
        }

        [Fact]
        public void GetEntity_ShouldReturnCorrectEntity()
        {
            // Arrange
            var world = new World();
            var entity = world.CreateEntity("TestEntity");

            // Act
            var retrievedEntity = world.GetEntity(entity.UniqueId);

            // Assert
            Assert.NotNull(retrievedEntity);
            Assert.Equal(entity.UniqueId, retrievedEntity.UniqueId);
            Assert.Equal("TestEntity", retrievedEntity.Name);
        }

        [Fact]
        public void GetEntity_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var world = new World();

            // Act
            var entity = world.GetEntity(99999);

            // Assert
            Assert.Null(entity);
        }

        [Fact]
        public void Initialize_ShouldSetCreationTime()
        {
            // Arrange
            var world = new World();
            var beforeInit = DateTime.Now;

            // Act
            world.Initialize();

            // Assert
            var afterInit = DateTime.Now;
            Assert.True(world.CreationTime >= beforeInit);
            Assert.True(world.CreationTime <= afterInit);
            Assert.Equal(0f, world.TotalTime);
        }

        [Fact]
        public void Update_ShouldUpdateTotalTime()
        {
            // Arrange
            var world = new World();
            float deltaTime = 0.016f; // ~60 FPS

            // Act
            world.Update(deltaTime);

            // Assert
            Assert.Equal(deltaTime, world.DeltaTime);
            Assert.Equal(deltaTime, world.TotalTime);
        }

        [Fact]
        public void Cleanup_ShouldDestroyAllEntities()
        {
            // Arrange
            var world = new World();
            var entity1 = world.CreateEntity("Entity1");
            var entity2 = world.CreateEntity("Entity2");

            // Act
            world.Cleanup();

            // Assert
            Assert.Empty(world.Entities);
            Assert.True(entity1.IsDestroyed);
            Assert.True(entity2.IsDestroyed);
        }

        [Fact]
        public void ApplyInputsToEntities_ShouldApplyInputsToEntitiesWithInputComponent()
        {
            // Arrange
            var world = new World();
            var entity = world.CreateEntity("PlayerEntity");
            entity.AddComponent(new LogicCore.FrameSync.LSInputComponent(1));

            var frameInputs = new OneFrameInputs(10);
            var input = new LSInput
            {
                PlayerId = 1,
                Frame = 10,
                MoveX = 0.7f,
                MoveY = 0.3f
            };
            frameInputs.AddInput(1, input);

            // Act
            world.ApplyInputsToEntities(frameInputs);

            // Assert
            var inputComponent = entity.GetComponent<LSInputComponent>();
            Assert.NotNull(inputComponent);
            Assert.NotNull(inputComponent.CurrentInput);
            Assert.Equal(0.7f, inputComponent.CurrentInput.MoveX);
            Assert.Equal(0.3f, inputComponent.CurrentInput.MoveY);
        }
    }
}
