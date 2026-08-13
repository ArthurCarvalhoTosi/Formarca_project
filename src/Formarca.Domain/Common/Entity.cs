namespace Formarca.Domain.Common;

public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; protected set; } = Guid.NewGuid();

    public DateTime CreatedAtUtc { get; protected set; } = Clock.Current.UtcNow;

    public DateTime? UpdatedAtUtc { get; protected set; }

    public bool IsDeleted { get; protected set; }

    public DateTime? DeletedAtUtc { get; protected set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();

    public void MarkUpdated() => UpdatedAtUtc = Clock.Current.UtcNow;

    public void SoftDelete()
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedAtUtc = Clock.Current.UtcNow;
        MarkUpdated();
    }
}
