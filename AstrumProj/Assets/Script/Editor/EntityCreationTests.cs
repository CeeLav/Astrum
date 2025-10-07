using System;
using NUnit.Framework;
using UnityEngine;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Factories;
using Astrum.LogicCore.Archetypes;

namespace Astrum.Tests.Editor
{
    /// <summary>
    /// 简单的编辑器下用例：验证通过 Archetype 和工厂的实体创建/销毁流程。
    /// 说明：不依赖配置表，仅使用 Archetype 名称，避免路径初始化依赖。
    /// </summary>
    public class EntityCreationTests
    {
        private World _world;

        [SetUp]
        public void SetUp()
        {
            // 初始化逻辑侧原型管理器（应在 GameApplication 中初始化；此处测试独立初始化）
            ArchetypeManager.Instance.Initialize();

            // 创建一个最小 World 并注入给工厂
            _world = new World { WorldId = 1, Name = "TestWorld", RoomId = 1001 };
            _world.Initialize(frame: 0);
            EntityFactory.Instance.Initialize(_world);
        }

        [TearDown]
        public void TearDown()
        {
            _world?.Cleanup();
        }

        [Test]
        public void CreateByArchetype_ShouldCreateEntity_AndRegisterInWorld()
        {
            // Arrange
            const string archetypeName = "BaseUnit"; // 依赖内置示例原型
            const int entityConfigId = 0; // 不使用配置表

            // Act
            var entity = EntityFactory.Instance.CreateByArchetype(archetypeName, entityConfigId);

            // Assert
            Assert.NotNull(entity, "实体应成功创建");
            Assert.IsTrue(_world.Entities.ContainsKey(entity.UniqueId), "实体应被加入 World.Entities");
            Assert.AreEqual(entityConfigId, entity.EntityConfigId, "应保存传入的 ConfigId（可为0）");
            Assert.IsTrue(entity.IsActive, "新建实体应为激活状态");
            Assert.IsFalse(entity.IsDestroyed, "新建实体不应为销毁状态");
        }

        [Test]
        public void DestroyEntity_ShouldRemove_FromWorld()
        {
            // Arrange
            var entity = EntityFactory.Instance.CreateByArchetype("BaseUnit", 0);
            var id = entity.UniqueId;
            Assert.IsTrue(_world.Entities.ContainsKey(id), "前置：实体已加入世界");

            // Act
            EntityFactory.Instance.DestroyEntity(entity);

            // Assert
            Assert.IsFalse(_world.Entities.ContainsKey(id), "销毁后实体应从世界移除");
        }
    }
}



