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

    [Fact]
    public async Task Login_Wrong_Password_Records_Failed_Access()
    {
        var repository = new InMemoryRepository();
        var userId = Guid.NewGuid();
        var identity = new FakeIdentityService
        {
            User = new IdentityUserInfo(userId, "maria@acme.com", "Maria", false, true, true),
            PasswordValid = false
        };

        var handler = new LoginCommandHandler(identity, repository, new FakeJwtTokenService(), new FakeTenantContext());
        var ex = await Assert.ThrowsAsync<UnauthorizedAppException>(
            () => handler.Handle(new LoginCommand("maria@acme.com", "WrongPassword"), CancellationToken.None));

        Assert.Equal("Invalid email or password.", ex.Message);
        Assert.Equal(1, identity.AccessFailedCallCount);
        Assert.Equal(0, identity.ResetAccessFailedCountCallCount);
    }

    [Fact]
    public async Task Login_Locked_Account_Rejects_Without_Password_Check()
    {
        var repository = new InMemoryRepository();
        var userId = Guid.NewGuid();
        var identity = new FakeIdentityService
        {
            User = new IdentityUserInfo(userId, "maria@acme.com", "Maria", false, true, true),
            PasswordValid = true,
            IsLockedOut = true
        };

        var handler = new LoginCommandHandler(identity, repository, new FakeJwtTokenService(), new FakeTenantContext());
        var ex = await Assert.ThrowsAsync<UnauthorizedAppException>(
            () => handler.Handle(new LoginCommand("maria@acme.com", "Password1"), CancellationToken.None));

        Assert.Equal("Invalid email or password.", ex.Message);
        Assert.Equal(0, identity.CheckPasswordCallCount);
        Assert.Equal(0, identity.AccessFailedCallCount);
        Assert.Equal(0, identity.ResetAccessFailedCountCallCount);
    }

    [Fact]
    public async Task Login_Success_Resets_Failed_Access_Count()
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
        await handler.Handle(new LoginCommand("maria@acme.com", "Password1"), CancellationToken.None);

        Assert.Equal(1, identity.ResetAccessFailedCountCallCount);
        Assert.Equal(0, identity.AccessFailedCallCount);
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
