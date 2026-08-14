namespace Formarca.Domain.IdentityAccess;

public static class PermissionCodes
{
    public const string TenantsRead = "tenants.read";
    public const string TenantsManage = "tenants.manage";
    public const string UsersRead = "users.read";
    public const string UsersManage = "users.manage";
    public const string GroupsRead = "groups.read";
    public const string GroupsManage = "groups.manage";
    public const string ProcessesRead = "processes.read";
    public const string RegistrationsRead = "registrations.read";
    public const string RegistrationsManage = "registrations.manage";

    public const string ClaimType = "permission";

    public static IReadOnlyList<(string Code, string Name)> Catalog { get; } =
    [
        (TenantsRead, "Read tenants"),
        (TenantsManage, "Manage tenants"),
        (UsersRead, "Read users"),
        (UsersManage, "Manage users"),
        (GroupsRead, "Read groups"),
        (GroupsManage, "Manage groups"),
        (ProcessesRead, "Read processes"),
        (RegistrationsRead, "Read registration"),
        (RegistrationsManage, "Manage registration")
    ];
}
