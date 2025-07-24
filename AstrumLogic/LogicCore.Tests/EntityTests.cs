using Xunit;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Components;
using Astrum.LogicCore.FrameSync;

namespace Astrum.LogicCore.Tests
{
    /// <summary>
    /// 实体相关的单元测试
    /// </summary>
    public class EntityTests
    {
        [Fact]
        public void CreateEntity_ShouldHaveUniqueId()
        {
            // Arrange & Act
            var entity1 = new Entity();
            var entity2 = new Entity();

            // Assert
            Assert.NotEqual(entity1.UniqueId, entity2.UniqueId);
            Assert.True(entity1.UniqueId > 0);
            Assert.True(entity2.UniqueId > 0);
        }

        [Fact]
        public void AddComponent_ShouldAddComponentSuccessfully()
        {
            // Arrange
            var entity = new Entity();
            var positionComponent = new PositionComponent(1, 2, 3);

            // Act
            entity.AddComponent(positionComponent);

            // Assert
            Assert.True(entity.HasComponent<PositionComponent>());
            var retrievedComponent = entity.GetComponent<PositionComponent>();
            Assert.NotNull(retrievedComponent);
            Assert.Equal(1, retrievedComponent.X);
            Assert.Equal(2, retrievedComponent.Y);
            Assert.Equal(3, retrievedComponent.Z);
        }

        [Fact]
        public void RemoveComponent_ShouldRemoveComponentSuccessfully()
        {
            // Arrange
            var entity = new Entity();
            var positionComponent = new PositionComponent(1, 2, 3);
            entity.AddComponent(positionComponent);

            // Act
            entity.RemoveComponent<PositionComponent>();

            // Assert
            Assert.False(entity.HasComponent<PositionComponent>());
            Assert.Null(entity.GetComponent<PositionComponent>());
        }

        [Fact]
        public void SetParent_ShouldSetParentChildRelationship()
        {
            // Arrange
            var parent = new Entity { Name = "Parent" };
            var child = new Entity { Name = "Child" };

            // Act
            child.SetParent(parent.UniqueId);
            parent.AddChild(child.UniqueId);

            // Assert
            Assert.Equal(parent.UniqueId, child.ParentId);
            Assert.Contains(child.UniqueId, parent.ChildrenIds);
        }

        [Fact]
        public void Destroy_ShouldMarkEntityAsDestroyed()
        {
            // Arrange
            var entity = new Entity();
            entity.AddComponent(new PositionComponent());

            // Act
            entity.Destroy();

            // Assert
            Assert.True(entity.IsDestroyed);
            Assert.False(entity.IsActive);
            Assert.Empty(entity.Components);
        }

        [Fact]
        public void ApplyInput_ShouldSetInputOnInputComponent()
        {
            // Arrange
            var entity = new Entity();
            var inputComponent = new LSInputComponent(1);
            entity.AddComponent(inputComponent);

            var input = new LSInput
            {
                PlayerId = 1,
                Frame = 10,
                MoveX = 0.5f,
                MoveY = -0.3f,
                Attack = true
            };

            // Act
            entity.ApplyInput(input);

            // Assert
            var component = entity.GetComponent<LSInputComponent>();
            Assert.NotNull(component);
            Assert.NotNull(component.CurrentInput);
            Assert.Equal(0.5f, component.CurrentInput.MoveX);
            Assert.Equal(-0.3f, component.CurrentInput.MoveY);
            Assert.True(component.CurrentInput.Attack);
        }
    }
}
