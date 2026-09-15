using System.Text.Json;

namespace Formarca.Domain.IdentityAccess;

public static class TableViewCatalog
{
    public const string Customers = "customers";
    public const string Brands = "brands";
    public const string Contracts = "contracts";
    public const string Invoices = "invoices";
    public const string Processes = "processes";
    public const string AdminTenants = "admin.tenants";
    public const string AdminUsers = "admin.users";
    public const string AdminGroups = "admin.groups";

    public static IReadOnlyList<string> TableKeys { get; } =
    [
        Customers,
        Brands,
        Contracts,
        Invoices,
        Processes,
        AdminTenants,
        AdminUsers,
        AdminGroups
    ];

    private static readonly Dictionary<string, HashSet<string>> ColumnKeysByTable = new(StringComparer.Ordinal)
    {
        [Customers] = ["name", "document", "email", "status"],
        [Brands] = ["name", "tenantName", "inpiClasses", "servicesDescription"],
        [Contracts] =
        [
            "code", "tenantName", "companyDocument", "representativeDocument", "brandName", "status",
            "clientSignedAt", "formarcaSignedAt", "generatedAtUtc", "signedAtUtc", "amount", "paidAmount",
            "classes", "paymentMethod"
        ],
        [Invoices] =
        [
            "contractCode", "tenantName", "paymentMethod", "installments", "contractAmount", "expectedAmount",
            "receivedAmount", "status"
        ],
        [Processes] = ["brandName", "contractCode", "processNumber", "status"],
        [AdminTenants] = ["name", "slug", "document", "status"],
        [AdminUsers] = ["email", "displayName", "platformRole", "isActive"],
        [AdminGroups] = ["name", "tenantId", "description"]
    };

    public static bool IsKnownTableKey(string? tableKey) =>
        !string.IsNullOrWhiteSpace(tableKey) && ColumnKeysByTable.ContainsKey(tableKey);

    public static IReadOnlyList<string> FilterColumnKeys(string tableKey, IEnumerable<string?>? keys)
    {
        EnsureKnownTableKey(tableKey);

        var known = ColumnKeysByTable[tableKey];
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        if (keys is null)
        {
            return result;
        }

        foreach (var raw in keys)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var key = raw.Trim();
            if (!known.Contains(key) || !seen.Add(key))
            {
                continue;
            }

            result.Add(key);
        }

        return result;
    }

    public static IReadOnlyList<string> NormalizeColumnKeys(string tableKey, IEnumerable<string?>? keys)
    {
        var filtered = FilterColumnKeys(tableKey, keys);
        if (filtered.Count == 0)
        {
            throw new ArgumentException("At least one known column key is required.", nameof(keys));
        }

        return filtered;
    }

    public static IReadOnlyList<string> ParseColumnKeysJson(string json, string tableKey)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("Column keys JSON is required.", nameof(json));
        }

        string?[]? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<string?[]>(json);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Column keys JSON is invalid.", nameof(json), ex);
        }

        if (parsed is null)
        {
            throw new ArgumentException("Column keys JSON is invalid.", nameof(json));
        }

        return NormalizeColumnKeys(tableKey, parsed);
    }

    public static string ToJson(IReadOnlyList<string> columnKeys)
    {
        ArgumentNullException.ThrowIfNull(columnKeys);
        return JsonSerializer.Serialize(columnKeys);
    }

    public static void EnsureKnownTableKey(string tableKey)
    {
        if (!IsKnownTableKey(tableKey))
        {
            throw new ArgumentException("Unknown table key.", nameof(tableKey));
        }
    }
}
