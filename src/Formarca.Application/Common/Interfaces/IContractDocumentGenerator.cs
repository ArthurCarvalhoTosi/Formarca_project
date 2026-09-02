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
    string Status,
    string CompanyName,
    string Street,
    string Number,
    string City,
    string State,
    string ZipCode,
    string MinimumTerm,
    string BodyMarkdown,
    byte[]? ClientSignaturePng,
    bool StampFormarcaSignature);

public interface IContractDocumentGenerator
{
    byte[] GeneratePdf(ContractDocumentModel model);

    byte[] GenerateWord(ContractDocumentModel model);
}

public interface IContractTemplateProvider
{
    string GetTemplate(string templateKey);
}
