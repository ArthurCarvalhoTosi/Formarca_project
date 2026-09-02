namespace Formarca.Application.Common.Interfaces;

public sealed record AutentiqueCreateDocumentRequest(
    string Name,
    byte[] PdfContent,
    string FileName,
    string ClientName,
    string? ClientEmail,
    string? ClientCpf,
    int LastPageNumber);

public sealed record AutentiqueCreatedDocument(
    string DocumentId,
    string ClientSignerPublicId,
    string? SignUrl);

public interface IAutentiqueClient
{
    string FormarcaSignerEmail { get; }

    bool VerifyWebhookSignature(string rawBody, string? signatureHeader);

    Task<AutentiqueCreatedDocument> CreateDocumentAsync(
        AutentiqueCreateDocumentRequest request,
        CancellationToken cancellationToken = default);

    Task SignDocumentAsync(string documentId, CancellationToken cancellationToken = default);

    Task<string> CreateLinkToSignatureAsync(string signerPublicId, CancellationToken cancellationToken = default);

    Task<byte[]> DownloadSignedPdfAsync(string documentId, CancellationToken cancellationToken = default);
}
