using System.Globalization;
using System.Text;

namespace Formarca.Domain.Tenancy;

public static class TenantSlug
{
    public static string FromName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var normalized = name.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsAsciiLetterOrDigit(c))
            {
                builder.Append(char.ToLowerInvariant(c));
            }
            else if (char.IsWhiteSpace(c) || c is '-' or '_')
            {
                builder.Append('-');
            }
        }

        var slug = CollapseHyphens(builder.ToString()).Trim('-');
        if (string.IsNullOrEmpty(slug))
        {
            slug = "tenant";
        }

        return slug.Length <= 100 ? slug : slug[..100].TrimEnd('-');
    }

    private static string CollapseHyphens(string value)
    {
        var builder = new StringBuilder(value.Length);
        var previousHyphen = false;
        foreach (var c in value)
        {
            if (c == '-')
            {
                if (previousHyphen)
                {
                    continue;
                }

                previousHyphen = true;
                builder.Append(c);
            }
            else
            {
                previousHyphen = false;
                builder.Append(c);
            }
        }

        return builder.ToString();
    }
}
