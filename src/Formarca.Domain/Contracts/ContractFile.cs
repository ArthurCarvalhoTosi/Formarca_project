using Formarca.Domain.Common;
using Formarca.Domain.Tenancy;

namespace Formarca.Domain.Contracts;

public class ContractFile : Entity, ITenantScoped
{
    private ContractFile()
    {
    }

    public Guid TenantId { get; private set; }

    public Guid ContractId { get; private set; }

    public ContractFileKind Kind { get; private set; }

    public string FileName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public byte[] Content { get; private set; } = [];

    public static ContractFile CreatePdf(
        Guid tenantId,
        Guid contractId,
        ContractFileKind kind,
        string fileName,
        byte[] content)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("Tenant is required.", nameof(tenantId));
        }

        if (contractId == Guid.Empty)
        {
            throw new ArgumentException("Contract is required.", nameof(contractId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(content);
        if (content.Length == 0)
        {
            throw new ArgumentException("PDF content is required.", nameof(content));
        }

        return new ContractFile
        {
            TenantId = tenantId,
            ContractId = contractId,
            Kind = kind,
            FileName = fileName.Trim(),
            ContentType = "application/pdf",
            Content = content
        };
    }

    public void ReplaceContent(byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (content.Length == 0)
        {
            throw new ArgumentException("PDF content is required.", nameof(content));
        }

        Content = content;
        MarkUpdated();
    }
}
