using System.Text.Json;
using Formarca.Domain.Common;
using Formarca.Domain.Tenancy;

namespace Formarca.Domain.IdentityAccess;

public class UserTableView : Entity, IPlatformEntity
{
    public const int NameMaxLength = 100;

    private UserTableView()
    {
    }

    public Guid UserId { get; private set; }

    public string TableKey { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string ColumnKeysJson { get; private set; } = "[]";

    public bool IsActive { get; private set; }

    public static UserTableView Create(
        Guid userId,
        string tableKey,
        string name,
        IReadOnlyList<string> columnKeys,
        bool activate,
        IEnumerable<UserTableView>? existingForTable = null)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        TableViewCatalog.EnsureKnownTableKey(tableKey);
        var normalizedKeys = TableViewCatalog.NormalizeColumnKeys(tableKey, columnKeys);

        var view = new UserTableView
        {
            UserId = userId,
            TableKey = tableKey,
            Name = NormalizeName(name),
            ColumnKeysJson = TableViewCatalog.ToJson(normalizedKeys),
            IsActive = false
        };

        if (activate)
        {
            view.Activate(existingForTable ?? []);
        }

        return view;
    }

    public IReadOnlyList<string> GetColumnKeys()
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<string?[]>(ColumnKeysJson) ?? [];
            return TableViewCatalog.FilterColumnKeys(TableKey, parsed);
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public void Rename(string name)
    {
        Name = NormalizeName(name);
        MarkUpdated();
    }

    public void SetColumnKeys(IReadOnlyList<string> columnKeys)
    {
        var normalized = TableViewCatalog.NormalizeColumnKeys(TableKey, columnKeys);
        ColumnKeysJson = TableViewCatalog.ToJson(normalized);
        MarkUpdated();
    }

    public void Activate(IEnumerable<UserTableView> siblingsForTable)
    {
        ArgumentNullException.ThrowIfNull(siblingsForTable);

        foreach (var sibling in siblingsForTable)
        {
            if (sibling.Id == Id || sibling.IsDeleted)
            {
                continue;
            }

            sibling.Deactivate();
        }

        if (!IsActive)
        {
            IsActive = true;
            MarkUpdated();
        }
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        MarkUpdated();
    }

    public void Delete(IEnumerable<UserTableView> siblingsForTable)
    {
        ArgumentNullException.ThrowIfNull(siblingsForTable);

        var wasActive = IsActive;
        if (IsActive)
        {
            IsActive = false;
        }

        SoftDelete();

        if (!wasActive)
        {
            return;
        }

        var successor = siblingsForTable
            .Where(v => v.Id != Id && !v.IsDeleted)
            .OrderByDescending(v => v.CreatedAtUtc)
            .ThenBy(v => v.Id)
            .FirstOrDefault();

        successor?.Activate([]);
    }

    private static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var trimmed = name.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            throw new ArgumentException($"Name cannot exceed {NameMaxLength} characters.", nameof(name));
        }

        return trimmed;
    }
}
