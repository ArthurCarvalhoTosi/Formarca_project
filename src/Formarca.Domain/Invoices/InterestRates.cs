using System.Globalization;
using System.Text.Json;

namespace Formarca.Domain.Invoices;

public sealed class InterestRates
{
    public const string CardCashKey = "cardCash";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly decimal[] _installments;

    private InterestRates(decimal[] installments, decimal cardCash)
    {
        _installments = installments;
        CardCash = cardCash;
    }

    public decimal CardCash { get; }

    public decimal GetInstallmentRate(int installments)
    {
        if (installments is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(installments), "Installments must be between 1 and 12.");
        }

        return _installments[installments - 1];
    }

    public decimal ForCard(int installments) =>
        installments == 1 ? CardCash : GetInstallmentRate(installments);

    public static InterestRates Zero { get; } = Create(
        Enumerable.Range(1, 12).ToDictionary(i => i, _ => 0m),
        cardCash: 0m);

    public static InterestRates Create(IReadOnlyDictionary<int, decimal> installments, decimal cardCash)
    {
        ArgumentNullException.ThrowIfNull(installments);
        EnsureNonNegative(cardCash, CardCashKey);

        var values = new decimal[12];
        for (var i = 1; i <= 12; i++)
        {
            if (!installments.TryGetValue(i, out var rate))
            {
                throw new ArgumentException($"Interest rate for installment {i} is required.", nameof(installments));
            }

            EnsureNonNegative(rate, i.ToString(CultureInfo.InvariantCulture));
            values[i - 1] = rate;
        }

        return new InterestRates(values, cardCash);
    }

    public static InterestRates Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("Interest rates JSON is required.", nameof(json));
        }

        Dictionary<string, JsonElement> document;
        try
        {
            document = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, JsonOptions)
                ?? throw new ArgumentException("Interest rates JSON is invalid.", nameof(json));
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Interest rates JSON is invalid.", nameof(json), ex);
        }

        var installments = new Dictionary<int, decimal>();
        for (var i = 1; i <= 12; i++)
        {
            var key = i.ToString(CultureInfo.InvariantCulture);
            if (!document.TryGetValue(key, out var element))
            {
                throw new ArgumentException($"Interest rates JSON must include key \"{key}\".", nameof(json));
            }

            installments[i] = ReadDecimal(element, key);
        }

        if (!document.TryGetValue(CardCashKey, out var cardCashElement))
        {
            throw new ArgumentException($"Interest rates JSON must include key \"{CardCashKey}\".", nameof(json));
        }

        return Create(installments, ReadDecimal(cardCashElement, CardCashKey));
    }

    public string ToJson()
    {
        var payload = new Dictionary<string, decimal>(13);
        for (var i = 1; i <= 12; i++)
        {
            payload[i.ToString(CultureInfo.InvariantCulture)] = _installments[i - 1];
        }

        payload[CardCashKey] = CardCash;
        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static decimal ReadDecimal(JsonElement element, string key)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out var value))
        {
            return value;
        }

        if (element.ValueKind == JsonValueKind.String
            && decimal.TryParse(element.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out value))
        {
            return value;
        }

        throw new ArgumentException($"Interest rate \"{key}\" must be a number.");
    }

    private static void EnsureNonNegative(decimal value, string key)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), $"Interest rate \"{key}\" cannot be negative.");
        }
    }
}
