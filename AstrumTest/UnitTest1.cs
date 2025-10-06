using NUnit.Framework;
using Astrum.LogicCore.Core;
using Astrum.LogicCore.Factories;
using Astrum.LogicCore.Archetypes;

namespace AstrumTest;

public class Tests
{
    [SetUp]
    public void Setup()
    {
        ArchetypeManager.Instance.Initialize();
    }

    [Test]
    public void CreateByArchetype_ShouldCreateEntity()
    {
        var world = new World { WorldId = 1, Name = "TestWorld" };
        world.Initialize(0);
        EntityFactory.Instance.Initialize(world);

        var entity = EntityFactory.Instance.CreateByArchetype("BaseUnit", 0);
        Assert.That(entity, Is.Not.Null);
        Assert.That(world.Entities.ContainsKey(entity.UniqueId), Is.True);
    }
}
