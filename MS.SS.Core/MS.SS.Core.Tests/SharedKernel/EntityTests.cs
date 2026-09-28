using MS.SS.Core.SharedKernel.Models;

namespace MS.SS.Core.Tests.SharedKernel;

public sealed class EntityTests
{
    private sealed class TestEntity : Entity
    {
        public TestEntity() { }
        public TestEntity(Guid id) : base(id) { }
    }

    private sealed class OtherEntity : Entity
    {
        public OtherEntity(Guid id) : base(id) { }
    }

    private sealed class TestAuditableEntity : AuditableEntity
    {
        public TestAuditableEntity() { }
        public TestAuditableEntity(Guid id) : base(id) { }
    }

    [Fact]
    public void Equals_SameId_SameType_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        var a = new TestEntity(id);
        var b = new TestEntity(id);

        Assert.True(a.Equals(b));
        Assert.True(a == b);
    }

    [Fact]
    public void Equals_SameId_DifferentType_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var a = new TestEntity(id);
        var b = new OtherEntity(id);

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_DifferentId_ReturnsFalse()
    {
        var a = new TestEntity(Guid.NewGuid());
        var b = new TestEntity(Guid.NewGuid());

        Assert.False(a.Equals(b));
        Assert.True(a != b);
    }

    [Fact]
    public void Equals_BothEmptyId_ReturnsFalse()
    {
        var a = new TestEntity();
        var b = new TestEntity();

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        var a = new TestEntity(Guid.NewGuid());

        Assert.False(a.Equals(null));
        Assert.False(a == null);
        Assert.True(a != null);
    }

    [Fact]
    public void Equals_ReferenceEquals_ReturnsTrue()
    {
        var a = new TestEntity(Guid.NewGuid());

        Assert.True(a.Equals(a));
    }

    [Fact]
    public void Equals_SameReferenceWithEmptyId_ReturnsTrue()
    {
        var a = new TestEntity();

        Assert.True(a.Equals(a));
    }

    [Fact]
    public void OperatorEquals_BothNull_ReturnsTrue()
    {
        TestEntity? a = null;
        TestEntity? b = null;

        Assert.True(a == b);
    }

    [Fact]
    public void GetHashCode_SameId_SameType_AreEqual()
    {
        var id = Guid.NewGuid();
        var a = new TestEntity(id);
        var b = new TestEntity(id);

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void StampCreated_SetsCreatedAt()
    {
        var at = new DateTime(2026, 1, 15, 8, 30, 0, DateTimeKind.Utc);
        var entity = new TestAuditableEntity(Guid.NewGuid());

        entity.StampCreated(at);

        Assert.Equal(at, entity.CreatedAt);
    }

    [Fact]
    public void AuditableEntity_KeepsIdBasedEquality()
    {
        var id = Guid.NewGuid();

        Assert.Equal(new TestAuditableEntity(id), new TestAuditableEntity(id));
    }
}
