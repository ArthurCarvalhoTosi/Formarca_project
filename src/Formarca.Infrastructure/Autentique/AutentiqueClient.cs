using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Formarca.Application.Autentique;
using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace Formarca.Infrastructure.Autentique;

public sealed class AutentiqueClient(HttpClient http, IOptions<AutentiqueOptions> options) : IAutentiqueClient
{
    public const string GraphqlUrl = "https://api.autentique.com.br/v2/graphql";

    private const string CreateDocumentMutation = """
        mutation CreateDocumentMutation($document: DocumentInput!, $signers: [SignerInput!]!, $file: Upload!, $organizationId: Int, $sandbox: Boolean) {
          createDocument(document: $document, signers: $signers, file: $file, organization_id: $organizationId, sandbox: $sandbox) {
            id
            signatures {
              public_id
              name
              email
              link { short_link }
            }
          }
        }
        """;

    private const string SignDocumentMutation = """
        mutation SignDocument($documentId: UUID!, $organizationId: Int) {
          signDocument(id: $documentId, organization_id: $organizationId)
        }
        """;

    private const string CreateLinkMutation = """
        mutation CreateLinkToSignature($publicId: UUID!) {
          createLinkToSignature(public_id: $publicId) { short_link }
        }
        """;

    private const string DocumentQuery = """
        query Document($id: UUID!) {
          document(id: $id) { files { signed } }
        }
        """;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = null
    };

    private readonly AutentiqueOptions _options = options.Value;

    public string FormarcaSignerEmail => _options.FormarcaSignerEmail ?? string.Empty;

    public bool VerifyWebhookSignature(string rawBody, string? signatureHeader) =>
        AutentiqueWebhookHmac.Verify(rawBody, signatureHeader, _options.WebhookSecret);

    public async Task<AutentiqueCreatedDocument> CreateDocumentAsync(
        AutentiqueCreateDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureConfigured();
        if (string.IsNullOrWhiteSpace(FormarcaSignerEmail))
        {
            throw new ConflictException("Autentique FormarcaSignerEmail is not configured.");
        }

        var lastPage = Math.Max(request.LastPageNumber, 1);
        var variables = new Dictionary<string, object?>
        {
            ["document"] = new Dictionary<string, object?>
            {
                ["name"] = request.Name,
                ["sortable"] = true,
                ["refusable"] = true,
                ["new_signature_style"] = true,
                ["locale"] = new Dictionary<string, object?>
                {
                    ["country"] = "BR",
                    ["language"] = "pt-BR",
                    ["timezone"] = "America/Sao_Paulo"
                },
                ["configs"] = new Dictionary<string, object?>
                {
                    ["signature_appearance"] = "DRAW"
                }
            },
            ["signers"] = new object[]
            {
                FormarcaSigner(lastPage),
                ClientSigner(request, lastPage)
            },
            ["file"] = null,
            ["organizationId"] = OrganizationIdOrNull(),
            ["sandbox"] = _options.Sandbox
        };

        var operations = JsonSerializer.Serialize(
            new Dictionary<string, object?>
            {
                ["query"] = CreateDocumentMutation,
                ["variables"] = variables
            },
            JsonOptions);

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(operations, Encoding.UTF8), "operations");
        content.Add(new StringContent("""{"file":["variables.file"]}""", Encoding.UTF8), "map");
        var fileContent = new ByteArrayContent(request.PdfContent);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", string.IsNullOrWhiteSpace(request.FileName) ? "contract.pdf" : request.FileName);

        using var response = await SendAsync(content, cancellationToken);
        using var json = await ReadGraphqlAsync(response, cancellationToken);
        var created = json.RootElement.GetProperty("data").GetProperty("createDocument");
        var documentId = created.GetProperty("id").GetString()
            ?? throw new ConflictException("Autentique did not return a document id.");

        string? clientPublicId = null;
        string? signUrl = null;
        if (created.TryGetProperty("signatures", out var signatures) && signatures.ValueKind == JsonValueKind.Array)
        {
            foreach (var signature in signatures.EnumerateArray())
            {
                var email = ReadString(signature, "email");
                if (string.Equals(email, FormarcaSignerEmail, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                clientPublicId = ReadString(signature, "public_id");
                if (signature.TryGetProperty("link", out var link)
                    && link.ValueKind == JsonValueKind.Object)
                {
                    signUrl = ReadString(link, "short_link");
                }

                if (!string.IsNullOrWhiteSpace(clientPublicId))
                {
                    break;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(clientPublicId))
        {
            throw new ConflictException("Autentique did not return the client signer public id.");
        }

        return new AutentiqueCreatedDocument(documentId, clientPublicId, signUrl);
    }

    public async Task SignDocumentAsync(string documentId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentId);
        EnsureConfigured();
        using var json = await PostGraphqlAsync(
            SignDocumentMutation,
            new Dictionary<string, object?>
            {
                ["documentId"] = documentId,
                ["organizationId"] = OrganizationIdOrNull()
            },
            cancellationToken);

        if (!json.RootElement.TryGetProperty("data", out var data)
            || !data.TryGetProperty("signDocument", out var signed)
            || signed.ValueKind != JsonValueKind.True)
        {
            throw new ConflictException("Autentique refused to sign the document as Formarca.");
        }
    }

    public async Task<string> CreateLinkToSignatureAsync(
        string signerPublicId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signerPublicId);
        EnsureConfigured();
        using var json = await PostGraphqlAsync(
            CreateLinkMutation,
            new Dictionary<string, object?> { ["publicId"] = signerPublicId },
            cancellationToken);

        var url = json.RootElement
            .GetProperty("data")
            .GetProperty("createLinkToSignature")
            .GetProperty("short_link")
            .GetString();
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ConflictException("Autentique did not return a signature link.");
        }

        return url;
    }

    public async Task<byte[]> DownloadSignedPdfAsync(string documentId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentId);
        EnsureConfigured();
        using var json = await PostGraphqlAsync(
            DocumentQuery,
            new Dictionary<string, object?> { ["id"] = documentId },
            cancellationToken);

        var url = json.RootElement
            .GetProperty("data")
            .GetProperty("document")
            .GetProperty("files")
            .GetProperty("signed")
            .GetString();
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ConflictException("Autentique signed PDF is not available yet.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new ConflictException($"Failed to download Autentique signed PDF ({(int)response.StatusCode}).");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (bytes.Length == 0)
        {
            throw new ConflictException("Autentique signed PDF was empty.");
        }

        return bytes;
    }

    private Dictionary<string, object?> FormarcaSigner(int lastPage) =>
        new()
        {
            ["email"] = FormarcaSignerEmail.Trim(),
            ["action"] = "SIGN",
            ["positions"] = Positions(x: "18.0", lastPage)
        };

    private static Dictionary<string, object?> ClientSigner(AutentiqueCreateDocumentRequest request, int lastPage)
    {
        var signer = new Dictionary<string, object?>
        {
            ["name"] = request.ClientName,
            ["action"] = "SIGN",
            ["positions"] = Positions(x: "62.0", lastPage)
        };

        if (!string.IsNullOrWhiteSpace(request.ClientEmail))
        {
            signer["email"] = request.ClientEmail.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.ClientCpf))
        {
            signer["configs"] = new Dictionary<string, object?> { ["cpf"] = request.ClientCpf };
        }

        return signer;
    }

    private static object[] Positions(string x, int lastPage) =>
    [
        new Dictionary<string, object?> { ["x"] = x, ["y"] = "78.0", ["z"] = lastPage, ["element"] = "SIGNATURE" },
        new Dictionary<string, object?> { ["x"] = x, ["y"] = "88.0", ["z"] = lastPage, ["element"] = "DATE" }
    ];

    private int? OrganizationIdOrNull() =>
        _options.OrganizationId is > 0 ? _options.OrganizationId : null;

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new ConflictException("Autentique ApiKey is not configured.");
        }
    }

    private async Task<JsonDocument> PostGraphqlAsync(
        string query,
        Dictionary<string, object?> variables,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(
            new Dictionary<string, object?>
            {
                ["query"] = query,
                ["variables"] = variables
            },
            JsonOptions);
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var response = await SendAsync(content, cancellationToken);
        return await ReadGraphqlAsync(response, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpContent content, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, GraphqlUrl) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        return await http.SendAsync(request, cancellationToken);
    }

    private static async Task<JsonDocument> ReadGraphqlAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new ConflictException($"Autentique request failed ({(int)response.StatusCode}).");
        }

        JsonDocument json;
        try
        {
            json = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
        }
        catch (JsonException)
        {
            throw new ConflictException("Autentique returned an invalid response.");
        }

        if (json.RootElement.TryGetProperty("errors", out var errors)
            && errors.ValueKind == JsonValueKind.Array
            && errors.GetArrayLength() > 0)
        {
            var message = errors[0].TryGetProperty("message", out var messageElement)
                ? messageElement.GetString()
                : null;
            json.Dispose();
            throw new ConflictException(
                string.IsNullOrWhiteSpace(message) ? "Autentique GraphQL error." : message);
        }

        return json;
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
