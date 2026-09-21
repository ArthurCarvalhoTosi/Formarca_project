using System.Text;

namespace Formarca.Domain.Tenancy;

public static class BrazilianDocuments
{
    public static string DigitsOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (char.IsAsciiDigit(c))
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    public static bool IsValidCpfOrCnpj(string? value)
    {
        var digits = DigitsOnly(value);
        return digits.Length switch
        {
            11 => IsValidCpf(digits),
            14 => IsValidCnpj(digits),
            _ => false
        };
    }

    public static bool IsValidCpf(string? value)
    {
        var digits = DigitsOnly(value);
        if (digits.Length != 11 || AllDigitsEqual(digits))
        {
            return false;
        }

        return CheckDigit(digits, 9, 10) == digits[9] - '0'
            && CheckDigit(digits, 10, 11) == digits[10] - '0';
    }

    public static bool IsValidCnpj(string? value)
    {
        var digits = DigitsOnly(value);
        if (digits.Length != 14 || AllDigitsEqual(digits))
        {
            return false;
        }

        int[] firstWeights = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] secondWeights = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

        return WeightedCheckDigit(digits, firstWeights) == digits[12] - '0'
            && WeightedCheckDigit(digits, secondWeights) == digits[13] - '0';
    }

    private static bool AllDigitsEqual(string digits)
    {
        for (var i = 1; i < digits.Length; i++)
        {
            if (digits[i] != digits[0])
            {
                return false;
            }
        }

        return true;
    }

    private static int CheckDigit(string digits, int length, int startWeight)
    {
        var sum = 0;
        for (var i = 0; i < length; i++)
        {
            sum += (digits[i] - '0') * (startWeight - i);
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    private static int WeightedCheckDigit(string digits, int[] weights)
    {
        var sum = 0;
        for (var i = 0; i < weights.Length; i++)
        {
            sum += (digits[i] - '0') * weights[i];
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
