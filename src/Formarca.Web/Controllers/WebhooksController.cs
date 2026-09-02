using Formarca.Application.Contracts.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Formarca.Web.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/webhooks")]
public sealed class WebhooksController(ISender sender) : ControllerBase
{
    [HttpPost("autentique")]
    public async Task<IActionResult> Autentique(CancellationToken cancellationToken)
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);
        Request.Body.Position = 0;

        var signature = Request.Headers["X-Autentique-Signature"].FirstOrDefault();
        await sender.Send(new HandleAutentiqueWebhookCommand(rawBody, signature), cancellationToken);
        return Ok();
    }
}
