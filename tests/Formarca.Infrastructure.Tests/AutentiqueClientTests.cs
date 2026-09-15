using System.Net;
using System.Text;
using Formarca.Application.Autentique;
using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Infrastructure.Autentique;
using Microsoft.Extensions.Options;

namespace Formarca.Infrastructure.Tests;

public class AutentiqueClientTests
{
    [Fact]
    public void VerifyWebhookSignature_Uses_Configured_Secret()
    {
        var client = new AutentiqueClient(
            new HttpClient(),
            Options.Create(new AutentiqueOptions { WebhookSecret = "secret" }));
        const string body = """{"ok":true}""";
        var hex = AutentiqueWebhookHmac.ComputeHex(body, "secret");

        Assert.True(client.VerifyWebhookSignature(body, hex));
        Assert.False(client.VerifyWebhookSignature(body, "nope"));
    }

    [Fact]
    public async Task CreateDocument_Requires_ApiKey()
    {
        var client = new AutentiqueClient(
            new HttpClient(),
            Options.Create(new AutentiqueOptions { FormarcaSignerEmail = "a@b.com" }));

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            client.CreateDocumentAsync(
                new AutentiqueCreateDocumentRequest("FM-1", "%PDF"u8.ToArray(), "a.pdf", "Acme", null, null, 1),
                CancellationToken.None));

        Assert.Contains("ApiKey", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateDocument_Omits_OrganizationId_When_Not_Configured()
    {
        var handler = new CaptureHandler
        {
            ResponseBody = """
                {"data":{"createDocument":{"id":"doc-1","signatures":[
                  {"public_id":"s-formarca","email":"formarca@example.com","link":null},
                  {"public_id":"s-client","email":null,"link":{"short_link":"https://assina.ae/abc"}}
                ]}}}
                """
        };
        var client = new AutentiqueClient(
            new HttpClient(handler),
            Options.Create(new AutentiqueOptions
            {
                ApiKey = "key",
                FormarcaSignerEmail = "formarca@example.com"
            }));

        var created = await client.CreateDocumentAsync(
            new AutentiqueCreateDocumentRequest("FM-1", "%PDF"u8.ToArray(), "a.pdf", "Acme", null, null, 1),
            CancellationToken.None);

        Assert.Equal("doc-1", created.DocumentId);
        Assert.Equal("s-client", created.ClientSignerPublicId);
        Assert.Contains("sandbox: $sandbox", handler.Operations, StringComparison.Ordinal);
        Assert.Contains("\"new_signature_style\":true", handler.Operations, StringComparison.Ordinal);
        Assert.Contains("\"show_audit_page\":false", handler.Operations, StringComparison.Ordinal);
        Assert.Contains("\"x\":\"12.0\"", handler.Operations, StringComparison.Ordinal);
        Assert.Contains("\"x\":\"58.0\"", handler.Operations, StringComparison.Ordinal);
        Assert.Contains("\"y\":\"12.0\"", handler.Operations, StringComparison.Ordinal);
        Assert.Contains("\"y\":\"21.0\"", handler.Operations, StringComparison.Ordinal);
        Assert.Contains("\"z\":1", handler.Operations, StringComparison.Ordinal);
        Assert.Contains("\"element\":\"SIGNATURE\"", handler.Operations, StringComparison.Ordinal);
        Assert.Contains("\"element\":\"DATE\"", handler.Operations, StringComparison.Ordinal);
        Assert.Contains("DELIVERY_METHOD_LINK", handler.Operations, StringComparison.Ordinal);
        Assert.DoesNotContain("organization_id", handler.Operations, StringComparison.Ordinal);
        Assert.DoesNotContain("organizationId", handler.Operations, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateDocument_Binds_Client_Email_To_Link_Delivery()
    {
        var handler = new CaptureHandler
        {
            ResponseBody = """
                {"data":{"createDocument":{"id":"doc-1","signatures":[
                  {"public_id":"s-formarca","email":"formarca@example.com","link":null},
                  {"public_id":"s-client","email":"client@acme.com","link":{"short_link":"https://assina.ae/abc"}}
                ]}}}
                """
        };
        var client = new AutentiqueClient(
            new HttpClient(handler),
            Options.Create(new AutentiqueOptions
            {
                ApiKey = "key",
                FormarcaSignerEmail = "formarca@example.com"
            }));

        await client.CreateDocumentAsync(
            new AutentiqueCreateDocumentRequest(
                "FM-1",
                "%PDF"u8.ToArray(),
                "a.pdf",
                "Acme",
                "client@acme.com",
                "39053344705",
                1),
            CancellationToken.None);

        Assert.Contains("client@acme.com", handler.Operations, StringComparison.Ordinal);
        Assert.Contains("DELIVERY_METHOD_LINK", handler.Operations, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateDocument_Surfaces_Graphql_Validation_Without_Disposing_Json()
    {
        var handler = new CaptureHandler
        {
            ResponseBody = """
                {"errors":[{"message":"validation","locations":[{"line":2,"column":3}],"path":["createDocument"],"extensions":{"validation":{"organization_id":["must_be_an_integer"]},"category":"validation"}}],"data":{"createDocument":null}}
                """
        };
        var client = new AutentiqueClient(
            new HttpClient(handler),
            Options.Create(new AutentiqueOptions
            {
                ApiKey = "key",
                FormarcaSignerEmail = "formarca@example.com"
            }));

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            client.CreateDocumentAsync(
                new AutentiqueCreateDocumentRequest("FM-1", "%PDF"u8.ToArray(), "a.pdf", "Acme", null, null, 1),
                CancellationToken.None));

        Assert.Contains("organization_id", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("must_be_an_integer", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("disposed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public string ResponseBody { get; set; } = "{}";

        public string Operations { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Content is MultipartFormDataContent multipart)
            {
                foreach (var part in multipart)
                {
                    var name = part.Headers.ContentDisposition?.Name?.Trim('"');
                    if (name == "operations")
                    {
                        Operations = await part.ReadAsStringAsync(cancellationToken);
                    }
                }
            }
            else if (request.Content is not null)
            {
                Operations = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ResponseBody, Encoding.UTF8, "application/json")
            };
        }
    }
}
