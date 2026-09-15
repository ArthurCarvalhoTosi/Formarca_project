namespace Formarca.Application.Common;

public static class QueryFilterValues
{
    public static IReadOnlyList<TEnum> ParseEnums<TEnum>(IEnumerable<string>? values)
        where TEnum : struct, Enum
    {
        if (values is null)
        {
            return [];
        }

        var result = new List<TEnum>();
        foreach (var part in Split(values))
        {
            if (Enum.TryParse<TEnum>(part, ignoreCase: true, out var parsed) && !result.Contains(parsed))
            {
                result.Add(parsed);
            }
        }

        return result;
    }

    public static IReadOnlyList<Guid> ParseGuids(IEnumerable<string>? values)
    {
        if (values is null)
        {
            return [];
        }

        var result = new List<Guid>();
        foreach (var part in Split(values))
        {
            if (Guid.TryParse(part, out var parsed) && parsed != Guid.Empty && !result.Contains(parsed))
            {
                result.Add(parsed);
            }
        }

        return result;
    }

    public static IReadOnlyList<int> ParseInts(IEnumerable<string>? values)
    {
        if (values is null)
        {
            return [];
        }

        var result = new List<int>();
        foreach (var part in Split(values))
        {
            if (int.TryParse(part, out var parsed) && !result.Contains(parsed))
            {
                result.Add(parsed);
            }
        }

        return result;
    }

    public static IReadOnlyList<T>? NullIfEmpty<T>(IReadOnlyList<T> values) =>
        values.Count == 0 ? null : values;

    public static IReadOnlyList<Guid>? MergeGuids(Guid? single, IEnumerable<string>? values)
    {
        var ids = new List<Guid>();
        if (single is Guid id && id != Guid.Empty)
        {
            ids.Add(id);
        }

        foreach (var parsed in ParseGuids(values))
        {
            if (!ids.Contains(parsed))
            {
                ids.Add(parsed);
            }
        }

        return NullIfEmpty(ids);
    }

    private static IEnumerable<string> Split(IEnumerable<string> values)
    {
        foreach (var raw in values)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                yield return part;
            }
        }
    }
}
