using Formarca.Domain.Common;
using Formarca.Domain.Tenancy;

namespace Formarca.Domain.PlatformContent;

/// <summary>
/// Stub for future Formarca platform notices / anti-fraud blog content.
/// </summary>
public class PlatformNotice : Entity, IPlatformEntity
{
    private PlatformNotice()
    {
    }

    public string Title { get; private set; } = string.Empty;

    public string Body { get; private set; } = string.Empty;

    public bool IsPublished { get; private set; }

    public static PlatformNotice Create(string title, string body, bool isPublished = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);

        return new PlatformNotice
        {
            Title = title.Trim(),
            Body = body.Trim(),
            IsPublished = isPublished
        };
    }
}
