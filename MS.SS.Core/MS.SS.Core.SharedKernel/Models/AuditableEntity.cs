namespace MS.SS.Core.SharedKernel.Models;

/// <summary>
/// Entity that records when it was created. There is deliberately no update timestamp: nothing
/// reads it, and it would have to be re-stamped whenever a child row changes.
/// </summary>
public abstract class AuditableEntity : Entity
{
    public DateTime CreatedAt { get; private set; }

    protected AuditableEntity() { }

    protected AuditableEntity(Guid id) : base(id) { }

    public void StampCreated(DateTime at) => CreatedAt = at;
}
