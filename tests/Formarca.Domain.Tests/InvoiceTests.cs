using Formarca.Domain.Invoices;

namespace Formarca.Domain.Tests;

public class InvoiceTests
{
    [Fact]
    public void Submit_Then_Approve_Pays_Invoice()
    {
        var invoice = Invoice.CreatePending(Guid.NewGuid(), Guid.NewGuid(), 1000m);
        invoice.SubmitPayment(PaymentMethod.Pix, 1, 950m);
        Assert.Equal(InvoiceStatus.AwaitingApproval, invoice.Status);

        invoice.Approve(950m);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.Equal(950m, invoice.ReceivedAmount);
    }

    [Fact]
    public void Approve_Allowed_From_PendingPayment()
    {
        var invoice = Invoice.CreatePending(Guid.NewGuid(), Guid.NewGuid(), 1000m);
        invoice.Approve(1000m);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
    }

    [Fact]
    public void Approve_Throws_When_Rejected()
    {
        var invoice = Invoice.CreatePending(Guid.NewGuid(), Guid.NewGuid(), 1000m);
        invoice.Reject();
        Assert.Throws<InvalidOperationException>(() => invoice.Approve(1000m));
    }

    [Fact]
    public void Pricing_Applies_Pix_Discount()
    {
        var expected = InvoicePricing.CalculateExpectedAmount(
            1000m,
            invoiceDiscount: 0,
            generalDiscount: 10,
            pixDiscount: 5,
            PaymentMethod.Pix,
            1,
            InterestRates.Zero);

        Assert.Equal(855m, expected);
    }

    [Fact]
    public void Pricing_Applies_Card_Installment_Interest()
    {
        var rates = InterestRates.Create(
            Enumerable.Range(1, 12).ToDictionary(i => i, i => i == 3 ? 4.99m : 0m),
            cardCash: 2m);

        var expected = InvoicePricing.CalculateExpectedAmount(
            1000m,
            invoiceDiscount: 0,
            generalDiscount: 0,
            pixDiscount: 0,
            PaymentMethod.Card,
            3,
            rates);

        Assert.Equal(1049.90m, expected);
    }

    [Fact]
    public void Pricing_Uses_CardCash_For_One_Installment()
    {
        var rates = InterestRates.Create(
            Enumerable.Range(1, 12).ToDictionary(i => i, _ => 10m),
            cardCash: 2m);

        var expected = InvoicePricing.CalculateExpectedAmount(
            1000m,
            0,
            0,
            0,
            PaymentMethod.Card,
            1,
            rates);

        Assert.Equal(1020m, expected);
    }
}

public class InterestRatesTests
{
    [Fact]
    public void Parse_Requires_Keys_1_To_12_And_CardCash()
    {
        var json = InterestRates.Zero.ToJson();
        var rates = InterestRates.Parse(json);
        Assert.Equal(0m, rates.CardCash);
        Assert.Equal(0m, rates.GetInstallmentRate(12));
    }

    [Fact]
    public void Parse_Throws_When_Key_Missing()
    {
        var json = """{"1":0,"2":0,"3":0,"4":0,"5":0,"6":0,"7":0,"8":0,"9":0,"10":0,"11":0,"cardCash":0}""";
        Assert.Throws<ArgumentException>(() => InterestRates.Parse(json));
    }

    [Fact]
    public void Settings_Reject_Invalid_Json()
    {
        var settings = InvoiceSettings.CreateDefault();
        Assert.Throws<ArgumentException>(() => settings.Update("{bad", 0, 0));
    }
}
