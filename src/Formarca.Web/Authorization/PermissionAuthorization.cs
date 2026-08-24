using Formarca.Domain.IdentityAccess;
using Microsoft.AspNetCore.Authorization;

namespace Formarca.Web.Authorization;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var isPlatformAdmin = context.User.HasClaim(PlatformRoles.ClaimType, PlatformRoles.PlatformAdmin);
        if (isPlatformAdmin)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (context.User.HasClaim(PermissionCodes.ClaimType, requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

public static class AuthorizationExtensions
{
    public const string PlatformAdminPolicy = "PlatformAdmin";

    public static IServiceCollection AddFormarcaAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddAuthorizationBuilder()
            .AddPolicy(PlatformAdminPolicy, policy =>
                policy.RequireClaim(PlatformRoles.ClaimType, PlatformRoles.PlatformAdmin))
            .AddPolicy(PermissionCodes.TenantsRead, p => p.AddRequirements(new PermissionRequirement(PermissionCodes.TenantsRead)))
            .AddPolicy(PermissionCodes.TenantsManage, p => p.AddRequirements(new PermissionRequirement(PermissionCodes.TenantsManage)))
            .AddPolicy(PermissionCodes.UsersRead, p => p.AddRequirements(new PermissionRequirement(PermissionCodes.UsersRead)))
            .AddPolicy(PermissionCodes.UsersManage, p => p.AddRequirements(new PermissionRequirement(PermissionCodes.UsersManage)))
            .AddPolicy(PermissionCodes.GroupsRead, p => p.AddRequirements(new PermissionRequirement(PermissionCodes.GroupsRead)))
            .AddPolicy(PermissionCodes.GroupsManage, p => p.AddRequirements(new PermissionRequirement(PermissionCodes.GroupsManage)))
            .AddPolicy(PermissionCodes.RegistrationsRead, p => p.AddRequirements(new PermissionRequirement(PermissionCodes.RegistrationsRead)))
            .AddPolicy(PermissionCodes.RegistrationsManage, p => p.AddRequirements(new PermissionRequirement(PermissionCodes.RegistrationsManage)))
            .AddPolicy(PermissionCodes.ProcessesRead, p => p.AddRequirements(new PermissionRequirement(PermissionCodes.ProcessesRead)))
            .AddPolicy(PermissionCodes.BrandsRead, p => p.AddRequirements(new PermissionRequirement(PermissionCodes.BrandsRead)))
            .AddPolicy(PermissionCodes.BrandsManage, p => p.AddRequirements(new PermissionRequirement(PermissionCodes.BrandsManage)))
            .AddPolicy(PermissionCodes.ContractsRead, p => p.AddRequirements(new PermissionRequirement(PermissionCodes.ContractsRead)))
            .AddPolicy(PermissionCodes.ContractsManage, p => p.AddRequirements(new PermissionRequirement(PermissionCodes.ContractsManage)))
            .AddPolicy(PermissionCodes.ContractsSign, p => p.AddRequirements(new PermissionRequirement(PermissionCodes.ContractsSign)))
            .AddPolicy(PermissionCodes.InvoicesRead, p => p.AddRequirements(new PermissionRequirement(PermissionCodes.InvoicesRead)))
            .AddPolicy(PermissionCodes.InvoicesManage, p => p.AddRequirements(new PermissionRequirement(PermissionCodes.InvoicesManage)))
            .AddPolicy(PermissionCodes.InvoicesPay, p => p.AddRequirements(new PermissionRequirement(PermissionCodes.InvoicesPay)));

        return services;
    }
}
