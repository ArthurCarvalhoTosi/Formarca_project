namespace Formarca.Domain.Tenancy;

public static class BrazilianGeographic
{
    public static readonly IReadOnlySet<string> States = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG",
        "PA", "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO"
    };

    public static bool IsValidState(string? value) =>
        !string.IsNullOrWhiteSpace(value) && States.Contains(value.Trim());

    public static bool IsValidZipCode(string? value)
    {
        var digits = BrazilianDocuments.DigitsOnly(value);
        return digits.Length == 8;
    }

    public static bool IsValidPhone(string? value)
    {
        var digits = NormalizePhone(value);
        return digits.Length is 10 or 11;
    }

    public static string NormalizePhone(string? value)
    {
        var digits = BrazilianDocuments.DigitsOnly(value);
        if (digits.StartsWith("55", StringComparison.Ordinal) && digits.Length is 12 or 13)
        {
            return digits[2..];
        }

        return digits;
    }
}
