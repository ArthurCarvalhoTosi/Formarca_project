using Formarca.Application.Common.Interfaces;
using Formarca.Domain.Contracts;

namespace Formarca.Application.Contracts;

internal static class ContractDocuments
{
    public static ContractDocumentModel ToDocumentModel(this Contract contract, bool stampFormarcaSignature) =>
        new(
            contract.Code,
            contract.CompanyDocument,
            contract.RepresentativeDocument,
            contract.BrandName,
            contract.Classes,
            contract.Amount,
            contract.GeneratedAtUtc,
            contract.FormarcaSignedAt,
            contract.ClientSignedAt,
            contract.Status.ToString(),
            contract.CompanyName,
            contract.Street,
            contract.Number,
            contract.City,
            contract.State,
            contract.ZipCode,
            contract.MinimumTerm,
            contract.BodyMarkdown,
            contract.ClientSignaturePng,
            stampFormarcaSignature);

    public static async Task UpsertPdfAsync(
        IGenericRepository repository,
        Contract contract,
        ContractFileKind kind,
        byte[] content,
        CancellationToken cancellationToken)
    {
        var existing = await repository.FirstOrDefaultAsync<ContractFile>(
            f => f.ContractId == contract.Id && f.Kind == kind && !f.IsDeleted,
            cancellationToken);
        if (existing is null)
        {
            repository.Add(ContractFile.CreatePdf(
                contract.TenantId,
                contract.Id,
                kind,
                $"{contract.Code}.pdf",
                content));
            return;
        }

        existing.ReplaceContent(content);
    }
}
