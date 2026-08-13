using Formarca.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace Formarca.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string? DisplayName { get; set; }

    public bool IsPlatformAdmin { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = Clock.Current.UtcNow;
}

public class ApplicationRole : IdentityRole<Guid>
{
}
