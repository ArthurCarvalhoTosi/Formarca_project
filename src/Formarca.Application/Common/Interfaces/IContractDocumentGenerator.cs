namespace Formarca.Application.Common.Interfaces;

public sealed record ContractDocumentModel(
    string Code,
    string CompanyDocument,
    string RepresentativeDocument,
    string BrandName,
    string Classes,
    decimal Amount,
    DateTime GeneratedAtUtc,
    DateTime FormarcaSignedAt,
    DateTime? ClientSignedAt,
    string Status);

public interface IContractDocumentGenerator
{
    byte[] GeneratePdf(ContractDocumentModel model);

    byte[] GenerateWord(ContractDocumentModel model);
}
