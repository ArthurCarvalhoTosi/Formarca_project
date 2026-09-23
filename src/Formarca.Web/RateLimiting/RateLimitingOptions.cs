namespace Formarca.Web.RateLimiting;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public RateLimitWindowOptions Login { get; set; } = new() { PermitLimit = 10, WindowSeconds = 60 };

    public RateLimitWindowOptions Refresh { get; set; } = new() { PermitLimit = 30, WindowSeconds = 60 };

    public RateLimitWindowOptions Registration { get; set; } = new() { PermitLimit = 5, WindowSeconds = 3600 };

    public RateLimitWindowOptions Webhook { get; set; } = new() { PermitLimit = 60, WindowSeconds = 60 };

    public RateLimitWindowOptions Health { get; set; } = new() { PermitLimit = 60, WindowSeconds = 60 };

    public RateLimitWindowOptions Global { get; set; } = new() { PermitLimit = 120, WindowSeconds = 60 };
}

public sealed class RateLimitWindowOptions
{
    public int PermitLimit { get; set; }

    public int WindowSeconds { get; set; } = 60;
}

public static class RateLimitingPolicies
{
    public const string Login = "login";
    public const string Refresh = "refresh";
    public const string Registration = "registration";
    public const string Webhook = "webhook";
    public const string Health = "health";
}
