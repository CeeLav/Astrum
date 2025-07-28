using Astrum.CommonBase;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Factories;
using Xunit;

namespace Astrum.LogicCore.Tests
{
    public class CommonBaseIntegrationTests
    {
        [Fact]
        public void LogicCore_ShouldUseCommonBase_EventSystem()
        {
            // Arrange
            var eventSystem = EventSystem.Instance;
            var world = new World();
            var factory = EntityFactory.Instance;
            factory.Initialize(world);
            var unit = factory.CreateEntity<Unit>("TestUnit");
            bool eventReceived = false;

            // Act
            eventSystem.Subscribe<GameEventData>(data =>
            {
                eventReceived = true;
            });

            var gameEvent = new GameEventData
            {
                EventType = "UnitCreated",
                Data = unit
            };

            eventSystem.Publish(gameEvent);

            // Assert
            Assert.True(eventReceived);
        }

        [Fact]
        public void LogicCore_ShouldUseCommonBase_Logger()
        {
            // Arrange
            var logger = ASLogger.Instance;
            var world = new World();
            var factory = EntityFactory.Instance;
            factory.Initialize(world);
            var unit = factory.CreateEntity<Unit>("TestUnit");

            // Act & Assert - 确保不会抛出异常
            Assert.NotNull(logger);
            logger.Info("Unit created: {0}", unit.Name);
            logger.Debug("Unit position: {0}", unit.GetComponent<Astrum.LogicCore.Components.PositionComponent>());
        }

        [Fact]
        public void LogicCore_ShouldUseCommonBase_ObjectPool()
        {
            // Arrange
            var poolManager = ObjectPoolManager.Instance;
            var world = new World();
            var factory = EntityFactory.Instance;
            factory.Initialize(world);
            var unit = factory.CreateEntity<Unit>("TestUnit");

            // Act
            var pool = poolManager.GetSimplePool<Unit>(initialSize: 5, maxSize: 100);

            // Assert
            Assert.NotNull(pool);
            Assert.Equal(5, pool.Count);
            Assert.Equal(100, pool.MaxSize);
        }

        [Fact]
        public void LogicCore_ShouldUseCommonBase_Singleton()
        {
            // Arrange & Act
            var eventSystem1 = EventSystem.Instance;
            var eventSystem2 = EventSystem.Instance;

            // Assert
            Assert.NotNull(eventSystem1);
            Assert.NotNull(eventSystem2);
            Assert.Same(eventSystem1, eventSystem2);
        }
    }
} 