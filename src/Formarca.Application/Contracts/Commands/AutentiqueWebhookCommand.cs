using System.Text.Json;
using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Domain.Contracts;
using MediatR;

namespace Formarca.Application.Contracts.Commands;

public sealed record HandleAutentiqueWebhookCommand(string RawBody, string? SignatureHeader) : IRequest;

public sealed class HandleAutentiqueWebhookCommandHandler(
    IAutentiqueClient autentique,
    IGenericRepository repository,
    ITenantContext tenantContext) : IRequestHandler<HandleAutentiqueWebhookCommand>
{
    public async Task Handle(HandleAutentiqueWebhookCommand request, CancellationToken cancellationToken)
    {
        if (!autentique.VerifyWebhookSignature(request.RawBody ?? string.Empty, request.SignatureHeader))
        {
            throw new UnauthorizedAppException("Invalid Autentique webhook signature.");
        }

        if (!TryReadEvent(request.RawBody, out var eventType, out var documentId))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(documentId))
        {
            return;
        }

        tenantContext.SetIgnoreTenantFilters(true);
        var contract = await repository.FirstOrDefaultAsync<Contract>(
            c => c.AutentiqueDocumentId == documentId && !c.IsDeleted,
            cancellationToken);
        if (contract is null)
        {
            return;
        }

        if (string.Equals(eventType, "document.rejected", StringComparison.OrdinalIgnoreCase))
        {
            if (contract.Status != ContractStatus.Signed)
            {
                contract.MarkAutentiqueRejected();
                await repository.CommitAsync(cancellationToken);
            }

            return;
        }

        if (!string.Equals(eventType, "document.finished", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (contract.Status == ContractStatus.Signed)
        {
            return;
        }

        if (contract.Status != ContractStatus.Issued)
        {
            return;
        }

        var signedPdf = await autentique.DownloadSignedPdfAsync(documentId, cancellationToken);
        try
        {
            contract.SignByClient();
        }
        catch (InvalidOperationException ex)
        {
            throw new ConflictException(ex.Message);
        }

        await ContractDocuments.UpsertPdfAsync(repository, contract, ContractFileKind.Signed, signedPdf, cancellationToken);
        await ContractDocuments.EnsurePendingInvoiceAsync(repository, contract, cancellationToken);
        await repository.CommitAsync(cancellationToken);
    }

    private static bool TryReadEvent(string? rawBody, out string? eventType, out string? documentId)
    {
        eventType = null;
        documentId = null;
        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(rawBody);
            var root = document.RootElement;
            if (!root.TryGetProperty("event", out var eventElement))
            {
                return false;
            }

            eventType = ReadString(eventElement, "type");
            if (eventElement.TryGetProperty("data", out var data)
                && data.TryGetProperty("object", out var payload))
            {
                documentId = ReadString(payload, "id");
            }

            return !string.IsNullOrWhiteSpace(eventType);
        }
        catch (JsonException)
        {
            throw new ConflictException("Invalid Autentique webhook payload.");
        }
    }

    private static string? ReadString(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var text = value.GetString();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }
}
