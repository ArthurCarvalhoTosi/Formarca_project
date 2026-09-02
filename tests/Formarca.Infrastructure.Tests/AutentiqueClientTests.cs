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
}
