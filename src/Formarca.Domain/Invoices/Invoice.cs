using Formarca.Domain.Common;
using Formarca.Domain.Tenancy;

namespace Formarca.Domain.Invoices;

public class Invoice : Entity, ITenantScoped
{
    private Invoice()
    {
    }

    public Guid TenantId { get; private set; }

    public Guid ContractId { get; private set; }

    public PaymentMethod? PaymentMethod { get; private set; }

    public int? Installments { get; private set; }

    public decimal ContractAmount { get; private set; }

    public decimal Discount { get; private set; }

    public decimal ExpectedAmount { get; private set; }

    public decimal ReceivedAmount { get; private set; }

    public InvoiceStatus Status { get; private set; } = InvoiceStatus.PendingPayment;

    public static Invoice CreatePending(Guid tenantId, Guid contractId, decimal contractAmount, decimal discount = 0)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant is required.", nameof(tenantId));
        }

        if (contractId == Guid.Empty)
        {
            throw new ArgumentException("Contract is required.", nameof(contractId));
        }

        if (contractAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(contractAmount), "Contract amount cannot be negative.");
        }

        if (discount is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(discount), "Discount must be between 0 and 100.");
        }

        return new Invoice
        {
            TenantId = tenantId,
            ContractId = contractId,
            ContractAmount = contractAmount,
            Discount = discount,
            ExpectedAmount = contractAmount,
            Status = InvoiceStatus.PendingPayment
        };
    }

    public void SubmitPayment(PaymentMethod method, int installments, decimal expectedAmount)
    {
        if (Status is not InvoiceStatus.PendingPayment and not InvoiceStatus.Rejected)
        {
            throw new InvalidOperationException("Only pending or rejected invoices can receive a payment submission.");
        }

        if (installments is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(installments), "Installments must be between 1 and 12.");
        }

        if (method == Formarca.Domain.Invoices.PaymentMethod.Pix && installments != 1)
        {
            throw new InvalidOperationException("Pix payments must use 1 installment.");
        }

        if (expectedAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedAmount), "Expected amount cannot be negative.");
        }

        PaymentMethod = method;
        Installments = installments;
        ExpectedAmount = expectedAmount;
        Status = InvoiceStatus.AwaitingApproval;
        MarkUpdated();
    }

    public void Approve(decimal receivedAmount)
    {
        if (Status is not InvoiceStatus.PendingPayment and not InvoiceStatus.AwaitingApproval)
        {
            throw new InvalidOperationException("Only pending or awaiting-approval invoices can be approved.");
        }

        if (receivedAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(receivedAmount), "Received amount cannot be negative.");
        }

        ReceivedAmount = receivedAmount;
        Status = InvoiceStatus.Paid;
        MarkUpdated();
    }

    public void Reject()
    {
        if (Status is not InvoiceStatus.PendingPayment and not InvoiceStatus.AwaitingApproval)
        {
            throw new InvalidOperationException("Only pending or awaiting-approval invoices can be rejected.");
        }

        Status = InvoiceStatus.Rejected;
        MarkUpdated();
    }
}
