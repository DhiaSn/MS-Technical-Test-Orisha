namespace MS.SS.Core.SharedKernel.Models;

/// <summary>
/// Identity-by-Id base for every persisted type. Equality is Id-based so two instances loaded
/// through different DbContexts still compare equal.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected set; }

    /// <summary>Materialisation constructor for EF Core and for derived factories.</summary>
    protected Entity() { }

    protected Entity(Guid id) => Id = id;

    public override bool Equals(object? obj)
    {
        if (obj is not Entity other || GetType() != other.GetType()) return false;
        return ReferenceEquals(this, other) || (Id != Guid.Empty && Id == other.Id);
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity? a, Entity? b) => a is null ? b is null : a.Equals(b);

    public static bool operator !=(Entity? a, Entity? b) => !(a == b);
}
