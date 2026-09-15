using Formarca.Application.Common.Exceptions;
using Formarca.Application.Common.Interfaces;
using Formarca.Application.Common.Models;
using Formarca.Application.Tenancy.Commands;
using Formarca.Application.Tenancy.Queries;
using Formarca.Domain.Brands;
using Formarca.Domain.IdentityAccess;
using Formarca.Domain.Tenancy;

namespace Formarca.Application.Tests;

public class RegistrationHandlerTests
{
    [Fact]
    public async Task Submit_Creates_Pending_Tenant_Without_User()
    {
        var repository = new InMemoryRepository();
        var identity = new FakeIdentityService();
        var handler = new SubmitRegistrationCommandHandler(repository, identity, new FakeTenantContext());

        var result = await handler.Handle(ValidSubmit(), CancellationToken.None);

        Assert.Equal(TenantStatus.Pending.ToString(), result.Status);
        Assert.Empty(identity.CreatedUsers);
        Assert.False(repository.Query<UserTenantMembership>().Any());
        var tenant = repository.Query<Tenant>().Single();
        Assert.Equal(TenantStatus.Pending, tenant.Status);
        Assert.Equal("maria@acme.com", tenant.Email);
        Assert.Equal("João Silva", tenant.LegalRepresentativeName);
        Assert.Equal("39053344705", tenant.LegalRepresentativeDocument);
        Assert.Equal(1500m, tenant.QuotedAmount);
        Assert.False(repository.Query<Brand>().Any());
    }

    [Fact]
    public async Task Submit_Creates_Brand_When_Brand_Fields_Are_Provided()
    {
        var repository = new InMemoryRepository();
        var identity = new FakeIdentityService();
        var handler = new SubmitRegistrationCommandHandler(repository, identity, new FakeTenantContext());

        await handler.Handle(
            ValidSubmit() with { BrandName = "Acme", BrandDescription = "Widgets" },
            CancellationToken.None);

        var tenant = repository.Query<Tenant>().Single();
        var brand = repository.Query<Brand>().Single();
        Assert.Equal(tenant.Id, brand.TenantId);
        Assert.Equal("Acme", brand.Name);
    }

    [Fact]
    public async Task Submit_Allows_Same_Document_On_Different_Tenants()
    {
        var repository = new InMemoryRepository();
        var identity = new FakeIdentityService();
        var handler = new SubmitRegistrationCommandHandler(repository, identity, new FakeTenantContext());

        await handler.Handle(ValidSubmit(), CancellationToken.None);
        await handler.Handle(
            ValidSubmit() with
            {
                Name = "Acme Brand Two",
                Email = "brand2@acme.com",
                BrandName = "Acme Two",
                BrandDescription = "Second brand",
            },
            CancellationToken.None);

        var tenants = repository.Query<Tenant>().ToList();
        Assert.Equal(2, tenants.Count);
        Assert.All(tenants, tenant => Assert.Equal("39053344705", tenant.Document));
        Assert.Equal(2, tenants.Select(t => t.Slug).Distinct().Count());
        Assert.Equal(1, repository.Query<Brand>().Count());
    }

    [Fact]
    public async Task Approve_Creates_User_And_Puts_Tenant_Onboarding()
    {
        var repository = new InMemoryRepository();
        foreach (var (code, name) in PermissionCodes.Catalog)
        {
            repository.Add(Permission.Create(code, name));
        }

        var tenant = Tenant.Register(
            "Acme Ltd",
            "acme-ltd",
            "maria@acme.com",
            "11987654321",
            "39053344705",
            Address.Create("01310100", "Av Paulista", "100", null, "Bela Vista", "São Paulo", "SP"));
        repository.Add(tenant);

        var identity = new FakeIdentityService();
        var handler = new ApproveTenantCommandHandler(repository, identity, new FakeTenantContext());

        var result = await handler.Handle(new ApproveTenantCommand(tenant.Id, "Password1"), CancellationToken.None);

        Assert.Equal(TenantStatus.Onboarding.ToString(), result.Status);
        Assert.Single(identity.CreatedUsers);
        Assert.Equal("maria@acme.com", identity.CreatedUsers[0].Email);
        Assert.Equal("Password1", identity.CreatedUsers[0].Password);
        Assert.True(repository.Query<UserTenantMembership>().Any(m => m.TenantId == tenant.Id && m.UserId == identity.NextUserId));
        Assert.True(repository.Query<UserGroupMembership>().Any(m => m.TenantId == tenant.Id && m.GroupId != Guid.Empty));
        Assert.Equal(TenantStatus.Onboarding, repository.Query<Tenant>().Single().Status);
        Assert.Contains(repository.Query<Group>(), g => g.Name == DefaultGroupNames.ClientOnboarding);
        var onboarding = repository.Query<Group>().Single(g => g.Name == DefaultGroupNames.ClientOnboarding);
        Assert.Contains(repository.Query<UserGroupMembership>(), m => m.GroupId == onboarding.Id);
        var onboardingPermissionIds = repository.Query<GroupPermission>()
            .Where(gp => gp.GroupId == onboarding.Id && !gp.IsDeleted)
            .Select(gp => gp.PermissionId)
            .ToHashSet();
        var codes = repository.Query<Permission>()
            .Where(p => onboardingPermissionIds.Contains(p.Id))
            .Select(p => p.Code)
            .ToHashSet();
        Assert.Contains(PermissionCodes.ContractsSign, codes);
        Assert.Contains(PermissionCodes.InvoicesPay, codes);
        Assert.DoesNotContain(
            repository.Query<UserGroupMembership>(),
            m => repository.Query<Group>().Any(g => g.Id == m.GroupId && g.Name == DefaultGroupNames.TenantAdmins));
    }

    [Fact]
    public async Task GetByTenantId_Allows_Own_Tenant()
    {
        var (repository, own, _) = SeedTwoOnboardingTenants();
        var handler = new GetRegistrationByTenantIdQueryHandler(
            OwnTenantUser(own.Id), repository, new FakeTenantContext());

        var result = await handler.Handle(new GetRegistrationByTenantIdQuery(own.Id), CancellationToken.None);

        Assert.Equal(own.Id, result.Id);
        Assert.Equal("maria@acme.com", result.Email);
    }

    [Fact]
    public async Task GetByTenantId_Forbids_Other_Tenant()
    {
        var (repository, own, other) = SeedTwoOnboardingTenants();
        var handler = new GetRegistrationByTenantIdQueryHandler(
            OwnTenantUser(own.Id), repository, new FakeTenantContext());

        var ex = await Assert.ThrowsAsync<ForbiddenException>(
            () => handler.Handle(new GetRegistrationByTenantIdQuery(other.Id), CancellationToken.None));

        Assert.Equal("User is not a member of the requested tenant.", ex.Message);
    }

    [Fact]
    public async Task GetByTenantId_Allows_PlatformAdmin_Other_Tenant()
    {
        var (repository, _, other) = SeedTwoOnboardingTenants();
        var handler = new GetRegistrationByTenantIdQueryHandler(
            PlatformAdminUser(), repository, new FakeTenantContext());

        var result = await handler.Handle(new GetRegistrationByTenantIdQuery(other.Id), CancellationToken.None);

        Assert.Equal(other.Id, result.Id);
        Assert.Equal("other@acme.com", result.Email);
    }

    [Fact]
    public async Task UpdateByTenantId_Allows_Own_Tenant()
    {
        var (repository, own, _) = SeedTwoOnboardingTenants();
        var handler = new UpdateRegistrationByTenantIdCommandHandler(
            OwnTenantUser(own.Id), repository, new FakeIdentityService(), new FakeTenantContext());

        var result = await handler.Handle(ValidUpdateByTenantId(own.Id, "Updated Ltd", "updated@acme.com"), CancellationToken.None);

        Assert.Equal("Updated Ltd", result.Name);
        Assert.Equal("updated@acme.com", result.Email);
        Assert.Equal("Updated Ltd", repository.Query<Tenant>().Single(t => t.Id == own.Id).Name);
        Assert.Equal("João Silva", result.LegalRepresentativeName);
    }

    [Fact]
    public async Task UpdateByTenantId_Forbids_Other_Tenant()
    {
        var (repository, own, other) = SeedTwoOnboardingTenants();
        var handler = new UpdateRegistrationByTenantIdCommandHandler(
            OwnTenantUser(own.Id), repository, new FakeIdentityService(), new FakeTenantContext());

        var ex = await Assert.ThrowsAsync<ForbiddenException>(
            () => handler.Handle(ValidUpdateByTenantId(other.Id, "Hacked", "hacked@acme.com"), CancellationToken.None));

        Assert.Equal("User is not a member of the requested tenant.", ex.Message);
        Assert.Equal("Other Ltd", repository.Query<Tenant>().Single(t => t.Id == other.Id).Name);
    }

    [Fact]
    public async Task UpdateByTenantId_Allows_PlatformAdmin_Other_Tenant()
    {
        var (repository, _, other) = SeedTwoOnboardingTenants();
        var handler = new UpdateRegistrationByTenantIdCommandHandler(
            PlatformAdminUser(), repository, new FakeIdentityService(), new FakeTenantContext());

        var result = await handler.Handle(
            ValidUpdateByTenantId(other.Id, "Admin Edit", "admin-edit@acme.com", "52998224725"),
            CancellationToken.None);

        Assert.Equal("Admin Edit", result.Name);
        Assert.Equal("admin-edit@acme.com", result.Email);
    }

    private static (IGenericRepository Repository, Tenant Own, Tenant Other) SeedTwoOnboardingTenants()
    {
        var repository = new InMemoryRepository();
        var own = RegisterApproved("Acme Ltd", "acme-ltd", "maria@acme.com", "39053344705");
        var other = RegisterApproved("Other Ltd", "other-ltd", "other@acme.com", "52998224725");
        repository.Add(own);
        repository.Add(other);
        return (repository, own, other);
    }

    private static Tenant RegisterApproved(string name, string slug, string email, string document)
    {
        var tenant = Tenant.Register(
            name,
            slug,
            email,
            "11987654321",
            document,
            Address.Create("01310100", "Av Paulista", "100", null, "Bela Vista", "São Paulo", "SP"));
        tenant.Approve();
        return tenant;
    }

    private static ICurrentUser OwnTenantUser(Guid tenantId) =>
        new FakeCurrentUser { TenantId = tenantId, IsPlatformAdmin = false, IsAuthenticated = true };

    private static ICurrentUser PlatformAdminUser() =>
        new FakeCurrentUser { TenantId = null, IsPlatformAdmin = true, IsAuthenticated = true };

    private static UpdateRegistrationByTenantIdCommand ValidUpdateByTenantId(
        Guid tenantId,
        string name,
        string email,
        string document = "39053344705") =>
        new(
            tenantId,
            name,
            email,
            "11987654321",
            document,
            new AddressDto("01310100", "Av Paulista", "100", null, "Bela Vista", "São Paulo", "SP"),
            "João Silva",
            "39053344705",
            1500m,
            "Initial budget");

    private static SubmitRegistrationCommand ValidSubmit() =>
        new(
            "Acme Ltd",
            "maria@acme.com",
            "11987654321",
            "39053344705",
            new AddressDto("01310100", "Av Paulista", "100", null, "Bela Vista", "São Paulo", "SP"),
            BrandName: null,
            BrandDescription: null,
            LegalRepresentativeName: "João Silva",
            LegalRepresentativeDocument: "39053344705",
            QuotedAmount: 1500m,
            BudgetNotes: "Initial budget");
}
