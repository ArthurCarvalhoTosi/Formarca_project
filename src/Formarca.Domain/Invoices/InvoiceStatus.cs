namespace Formarca.Domain.Invoices;

public enum InvoiceStatus
{
    PendingPayment = 0,
    AwaitingApproval = 1,
    Paid = 2,
    Rejected = 3
}
