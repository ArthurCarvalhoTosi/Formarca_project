namespace Formarca.Domain.Invoices;

public static class InvoicePricing
{
    public static decimal CalculateExpectedAmount(
        decimal contractAmount,
        decimal invoiceDiscount,
        decimal generalDiscount,
        decimal pixDiscount,
        PaymentMethod method,
        int installments,
        InterestRates rates)
    {
        ArgumentNullException.ThrowIfNull(rates);

        if (contractAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(contractAmount), "Contract amount cannot be negative.");
        }

        if (installments is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(installments), "Installments must be between 1 and 12.");
        }

        if (method == PaymentMethod.Pix && installments != 1)
        {
            throw new InvalidOperationException("Pix payments must use 1 installment.");
        }

        var discountPercent = invoiceDiscount != 0 ? invoiceDiscount : generalDiscount;
        if (discountPercent is < 0 or > 100)
        {
            throw new InvalidOperationException("Discount must be between 0 and 100.");
        }

        var afterDiscount = Round(contractAmount * (1 - (discountPercent / 100m)));
        if (method == PaymentMethod.Pix)
        {
            return Round(afterDiscount * (1 - (pixDiscount / 100m)));
        }

        var rate = rates.ForCard(installments);
        return Round(afterDiscount * (1 + (rate / 100m)));
    }

    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
