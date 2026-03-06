using FluentAssertions;
using MyHomeSolution.Domain.Common;

namespace MyHomeSolution.Domain.Tests.Common;

public sealed class BaseEntityTests
{
    private sealed class ConcreteEntity : BaseEntity;

    [Fact]
    public void Id_ShouldBeGenerated_WhenEntityIsCreated()
    {
        var entity = new ConcreteEntity();

        entity.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Id_ShouldBeUnique_ForDifferentInstances()
    {
        var entity1 = new ConcreteEntity();
        var entity2 = new ConcreteEntity();

        entity1.Id.Should().NotBe(entity2.Id);
    }

    [Fact]
    public void Entity_ShouldImplementIEntity()
    {
        var entity = new ConcreteEntity();

        entity.Should().BeAssignableTo<IEntity>();
    }
}
