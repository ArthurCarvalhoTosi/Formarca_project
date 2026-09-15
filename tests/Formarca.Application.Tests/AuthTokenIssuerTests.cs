using Formarca.Application.IdentityAccess.Auth;
using Formarca.Domain.IdentityAccess;
using Formarca.Domain.Tenancy;

namespace Formarca.Application.Tests;

public class AuthTokenIssuerTests
{
    [Fact]
    public async Task EnsureDefaultTenantGroups_Grants_Sign_And_Pay_To_TenantAdmins()
    {
        var repository = new InMemoryRepository();
        foreach (var (code, name) in PermissionCodes.Catalog)
        {
            repository.Add(Permission.Create(code, name));
        }

        var tenant = Tenant.Create("Acme", "acme");
        repository.Add(tenant);

        await AuthTokenIssuer.EnsureDefaultTenantGroupsAsync(repository, tenant.Id, CancellationToken.None);

        var codes = TenantAdminCodes(repository);
        Assert.Contains(PermissionCodes.ContractsSign, codes);
        Assert.Contains(PermissionCodes.InvoicesPay, codes);
    }

    [Fact]
    public async Task EnsureDefaultTenantGroups_Adds_Missing_Sign_And_Pay_To_Existing_TenantAdmins()
    {
        var repository = new InMemoryRepository();
        foreach (var (code, name) in PermissionCodes.Catalog)
        {
            repository.Add(Permission.Create(code, name));
        }

        var tenant = Tenant.Create("Acme", "acme");
        repository.Add(tenant);

        var group = Group.Create(DefaultGroupNames.TenantAdmins, tenant.Id, "Tenant administrators");
        repository.Add(group);
        var processesRead = repository.Query<Permission>().Single(p => p.Code == PermissionCodes.ProcessesRead);
        repository.Add(GroupPermission.Create(group.Id, processesRead.Id));

        await AuthTokenIssuer.EnsureDefaultTenantGroupsAsync(repository, tenant.Id, CancellationToken.None);

        Assert.Single(repository.Query<Group>().Where(g => g.Name == DefaultGroupNames.TenantAdmins && !g.IsDeleted));
        var codes = TenantAdminCodes(repository);
        Assert.Contains(PermissionCodes.ContractsSign, codes);
        Assert.Contains(PermissionCodes.InvoicesPay, codes);
        Assert.Contains(PermissionCodes.ProcessesRead, codes);
    }

    private static HashSet<string> TenantAdminCodes(InMemoryRepository repository)
    {
        var group = repository.Query<Group>().Single(g => g.Name == DefaultGroupNames.TenantAdmins && !g.IsDeleted);
        var permissionIds = repository.Query<GroupPermission>()
            .Where(gp => gp.GroupId == group.Id && !gp.IsDeleted)
            .Select(gp => gp.PermissionId)
            .ToHashSet();
        return repository.Query<Permission>()
            .Where(p => permissionIds.Contains(p.Id))
            .Select(p => p.Code)
            .ToHashSet();
    }
}
