using Formarca.Application.IdentityAccess.Auth.Commands;
using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Domain.IdentityAccess;
using Formarca.Domain.Tenancy;

namespace Formarca.Application.Tests;

public class LoginHandlerTests
{
    [Fact]
    public async Task Login_Allows_Onboarding_Tenant()
    {
        var repository = new InMemoryRepository();
        var tenant = TenantTestsTenant();
        tenant.Approve();
        repository.Add(tenant);

        var userId = Guid.NewGuid();
        repository.Add(UserTenantMembership.Create(userId, tenant.Id, isDefault: true));

        var identity = new FakeIdentityService
        {
            User = new IdentityUserInfo(userId, "maria@acme.com", "Maria", false, true, true),
            PasswordValid = true
        };

        var handler = new LoginCommandHandler(identity, repository, new FakeJwtTokenService(), new FakeTenantContext());
        var result = await handler.Handle(new LoginCommand("maria@acme.com", "Password1"), CancellationToken.None);

        Assert.Equal(userId, result.UserId);
        Assert.Contains(result.Tenants, t => t.Id == tenant.Id && t.Status == TenantStatus.Onboarding.ToString());
    }

    [Fact]
    public async Task Login_Rejects_Pending_Tenant()
    {
        var repository = new InMemoryRepository();
        var tenant = TenantTestsTenant();
        repository.Add(tenant);

        var userId = Guid.NewGuid();
        repository.Add(UserTenantMembership.Create(userId, tenant.Id, isDefault: true));

        var identity = new FakeIdentityService
        {
            User = new IdentityUserInfo(userId, "maria@acme.com", "Maria", false, true, true),
            PasswordValid = true
        };

        var handler = new LoginCommandHandler(identity, repository, new FakeJwtTokenService(), new FakeTenantContext());
        var ex = await Assert.ThrowsAsync<ForbiddenException>(
            () => handler.Handle(new LoginCommand("maria@acme.com", "Password1", tenant.Id), CancellationToken.None));
        Assert.Equal("User is not a member of the requested tenant.", ex.Message);
    }

    private static Tenant TenantTestsTenant() =>
        Tenant.Register(
            "Acme Ltd",
            "acme-ltd",
            "maria@acme.com",
            "11987654321",
            "39053344705",
            Address.Create("01310100", "Av Paulista", "100", null, "Bela Vista", "São Paulo", "SP"));
}
