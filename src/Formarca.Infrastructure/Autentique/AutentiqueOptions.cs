namespace Formarca.Infrastructure.Autentique;

public sealed class AutentiqueOptions
{
    public const string SectionName = "Autentique";

    public string ApiKey { get; set; } = string.Empty;

    public int? OrganizationId { get; set; }

    public bool Sandbox { get; set; }

    public string WebhookSecret { get; set; } = string.Empty;

    public string FormarcaSignerEmail { get; set; } = string.Empty;
}
