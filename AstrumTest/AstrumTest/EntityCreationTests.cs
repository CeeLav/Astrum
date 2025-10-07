using System;
using Xunit;
using Astrum.LogicCore.Archetypes;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Factories;
using Astrum.LogicCore.Managers;
using System.IO;

namespace AstrumTest
{
    public class EntityCreationTests
    {
        [Fact]
        public void CreateByArchetype_BaseUnit_ShouldCreateAndRegister()
        {
            // Arrange
            ArchetypeManager.Instance.Initialize();
            var world = new World { WorldId = 1, Name = "TestWorld", RoomId = 1001 };
            world.Initialize(0);
            EntityFactory.Instance.Initialize(world);

            // Act
            var entity = EntityFactory.Instance.CreateByArchetype("BaseUnit", 0);

            // Assert
            Assert.NotNull(entity);
            Assert.True(world.Entities.ContainsKey(entity.UniqueId));
            Assert.True(entity.IsActive);
            Assert.False(entity.IsDestroyed);
        }

        [Fact]
        public void CreateFromConfig_1001_ShouldCreateAndRegister()
        {
            // Arrange: 初始化配置管理器，指向导出表路径
            var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
            var configPath = Path.Combine(root, "AstrumConfig", "Tables", "output", "Client");
            ConfigManager.Instance.Initialize(configPath);

            ArchetypeManager.Instance.Initialize();
            var world = new World { WorldId = 2, Name = "CfgWorld", RoomId = 2001 };
            world.Initialize(0);
            EntityFactory.Instance.Initialize(world);

            // Act: 从表格数据 1001 创建
            var entity = world.CreateEntityByConfig(1001);

            // Assert
            Assert.NotNull(entity);
            Assert.True(world.Entities.ContainsKey(entity.UniqueId));
            Assert.Equal(1001, entity.EntityConfigId);
        }
    }
}


