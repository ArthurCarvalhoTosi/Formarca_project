using System.Globalization;
using Formarca.Domain.Contracts;
using Formarca.Domain.Tenancy;

namespace Formarca.Application.Contracts;

internal static class ContractPlaceholders
{
    public const string ContratanteNome = "contratanteNome";
    public const string Cnpj = "cnpj";
    public const string Endereco = "endereco";
    public const string Numero = "numero";
    public const string Cidade = "cidade";
    public const string Uf = "uf";
    public const string Cep = "cep";
    public const string MarcaNome = "marcaNome";
    public const string QuantidadeClasses = "quantidadeClasses";
    public const string Valor = "valor";
    public const string Periodo = "periodo";

    public static string Merge(string template, IReadOnlyDictionary<string, string> values)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(values);

        var result = template;
        foreach (var (key, value) in values)
        {
            result = result.Replace("{{" + key + "}}", value ?? string.Empty, StringComparison.Ordinal);
        }

        return result;
    }

    public static IReadOnlyDictionary<string, string> From(
        string companyName,
        string companyDocument,
        string street,
        string number,
        string city,
        string state,
        string zipCode,
        string brandName,
        string classes,
        decimal amount,
        string minimumTerm) =>
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ContratanteNome] = companyName ?? string.Empty,
            [Cnpj] = FormatDocument(companyDocument),
            [Endereco] = street ?? string.Empty,
            [Numero] = number ?? string.Empty,
            [Cidade] = city ?? string.Empty,
            [Uf] = state ?? string.Empty,
            [Cep] = FormatZipCode(zipCode),
            [MarcaNome] = brandName ?? string.Empty,
            [QuantidadeClasses] = CountClasses(classes).ToString(CultureInfo.InvariantCulture),
            [Valor] = amount.ToString("N2", CultureInfo.GetCultureInfo("pt-BR")),
            [Periodo] = string.IsNullOrWhiteSpace(minimumTerm)
                ? ContractTemplateKeys.DefaultMinimumTerm
                : minimumTerm
        };

    public static string FormatDocument(string? value)
    {
        var digits = BrazilianDocuments.DigitsOnly(value);
        return digits.Length switch
        {
            14 => $"{digits[..2]}.{digits[2..5]}.{digits[5..8]}/{digits[8..12]}-{digits[12..]}",
            11 => $"{digits[..3]}.{digits[3..6]}.{digits[6..9]}-{digits[9..]}",
            _ => digits
        };
    }

    public static string FormatZipCode(string? value)
    {
        var digits = BrazilianDocuments.DigitsOnly(value);
        return digits.Length == 8 ? $"{digits[..5]}-{digits[5..]}" : digits;
    }

    public static int CountClasses(string? classes)
    {
        if (string.IsNullOrWhiteSpace(classes))
        {
            return 0;
        }

        return classes
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Length;
    }
}
