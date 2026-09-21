using Formarca.Domain.Common;
using Formarca.Domain.Tenancy;

namespace Formarca.Domain.Processes;

public class ProcessStatusChange : Entity, ITenantScoped
{
    public const int NoteMaxLength = 1000;

    private ProcessStatusChange()
    {
    }

    public Guid TenantId { get; private set; }

    public Guid ProcessId { get; private set; }

    public Guid? FromStatusId { get; private set; }

    public Guid ToStatusId { get; private set; }

    public Guid? ChangedByUserId { get; private set; }

    public string? Note { get; private set; }

    public static ProcessStatusChange Record(
        Guid tenantId,
        Guid processId,
        Guid? fromStatusId,
        Guid toStatusId,
        Guid? changedByUserId,
        string? note)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant is required.", nameof(tenantId));
        }

        if (processId == Guid.Empty)
        {
            throw new ArgumentException("Process is required.", nameof(processId));
        }

        if (toStatusId == Guid.Empty)
        {
            throw new ArgumentException("Status is required.", nameof(toStatusId));
        }

        var trimmedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (trimmedNote is { Length: > NoteMaxLength })
        {
            throw new ArgumentException("Status note is too long.", nameof(note));
        }

        return new ProcessStatusChange
        {
            TenantId = tenantId,
            ProcessId = processId,
            FromStatusId = fromStatusId,
            ToStatusId = toStatusId,
            ChangedByUserId = changedByUserId,
            Note = trimmedNote
        };
    }
}
