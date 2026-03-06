using FluentAssertions;
using MyHomeSolution.Domain.Common;

namespace MyHomeSolution.Domain.Tests.Common;

public sealed class BaseAuditableEntityTests
{
    private sealed class ConcreteAuditableEntity : BaseAuditableEntity;

    [Fact]
    public void DefaultValues_ShouldBeCorrectlyInitialized()
    {
        var entity = new ConcreteAuditableEntity();

        entity.CreatedAt.Should().Be(default);
        entity.CreatedBy.Should().BeNull();
        entity.LastModifiedAt.Should().BeNull();
        entity.LastModifiedBy.Should().BeNull();
        entity.IsDeleted.Should().BeFalse();
        entity.DeletedAt.Should().BeNull();
        entity.DeletedBy.Should().BeNull();
        entity.AuditLogs.Should().BeEmpty();
    }

    [Fact]
    public void Entity_ShouldImplementIAuditableEntity()
    {
        var entity = new ConcreteAuditableEntity();

        entity.Should().BeAssignableTo<IAuditableEntity>();
    }

    [Fact]
    public void Entity_ShouldImplementISoftDeletable()
    {
        var entity = new ConcreteAuditableEntity();

        entity.Should().BeAssignableTo<ISoftDeletable>();
    }

    [Fact]
    public void AuditProperties_ShouldBeSettable()
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new ConcreteAuditableEntity
        {
            CreatedAt = now,
            CreatedBy = "user-1",
            LastModifiedAt = now.AddHours(1),
            LastModifiedBy = "user-2",
            IsDeleted = true,
            DeletedAt = now.AddHours(2),
            DeletedBy = "user-3"
        };

        entity.CreatedAt.Should().Be(now);
        entity.CreatedBy.Should().Be("user-1");
        entity.LastModifiedAt.Should().Be(now.AddHours(1));
        entity.LastModifiedBy.Should().Be("user-2");
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAt.Should().Be(now.AddHours(2));
        entity.DeletedBy.Should().Be("user-3");
    }
}
