using Formarca.Domain.Common;
using Formarca.Domain.Tenancy;

namespace Formarca.Domain.Invoices;

public class InvoiceSettings : Entity, IPlatformEntity
{
    private InvoiceSettings()
    {
    }

    public string InterestRatesJson { get; private set; } = InterestRates.Zero.ToJson();

    public decimal PixDiscount { get; private set; }

    public decimal GeneralDiscount { get; private set; }

    public static InvoiceSettings CreateDefault() =>
        new()
        {
            InterestRatesJson = InterestRates.Zero.ToJson(),
            PixDiscount = 0,
            GeneralDiscount = 0
        };

    public void Update(string interestRatesJson, decimal pixDiscount, decimal generalDiscount)
    {
        _ = InterestRates.Parse(interestRatesJson);
        EnsureDiscount(pixDiscount, nameof(pixDiscount));
        EnsureDiscount(generalDiscount, nameof(generalDiscount));

        InterestRatesJson = interestRatesJson.Trim();
        PixDiscount = pixDiscount;
        GeneralDiscount = generalDiscount;
        MarkUpdated();
    }

    public void Update(InterestRates rates, decimal pixDiscount, decimal generalDiscount)
    {
        ArgumentNullException.ThrowIfNull(rates);
        Update(rates.ToJson(), pixDiscount, generalDiscount);
    }

    public InterestRates GetInterestRates() => InterestRates.Parse(InterestRatesJson);

    private static void EnsureDiscount(decimal value, string name)
    {
        if (value is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(name, "Discount must be between 0 and 100.");
        }
    }
}
