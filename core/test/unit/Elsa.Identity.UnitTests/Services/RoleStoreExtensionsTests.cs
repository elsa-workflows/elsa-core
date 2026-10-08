using Elsa.Common.Services;
using Elsa.Extensions;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Services;
using Elsa.Testing.Shared.Multitenancy;
using NSubstitute;

namespace Elsa.Identity.UnitTests.Services;

public class RoleStoreExtensionsTests
{
    [Fact]
    public async Task FindByNameAsyncFindsTheRoleByNameNotById()
    {
        var store = new MemoryRoleStore(new MemoryStore<Role>(), TestTenantAccessor.Default);
        await store.SaveAsync(new Role { Id = "generated-id", Name = "Operators" });
        await store.SaveAsync(new Role { Id = "operators", Name = "Something else" });

        var role = await store.FindByNameAsync("Operators");

        Assert.Equal("generated-id", role?.Id);
    }

    [Fact]
    public async Task FindByNameAsyncIgnoresRolesAStoreReturnsDespiteTheNameFilter()
    {
        // A third-party store that predates RoleFilter.Name returns every role for a name-only filter.
        var store = Substitute.For<IRoleStore>();
        store.FindManyAsync(Arg.Any<RoleFilter>(), Arg.Any<CancellationToken>()).Returns(
        [
            new Role { Id = "auditors", Name = "Auditors" },
            new Role { Id = "operators", Name = "Operators" }
        ]);

        Assert.Equal("operators", (await store.FindByNameAsync("Operators"))?.Id);
        Assert.Null(await store.FindByNameAsync("admin"));
    }

    [Fact]
    public async Task FindByNameAsyncPrefersAnExactMatchOverACaseInsensitiveOne()
    {
        var store = Substitute.For<IRoleStore>();
        store.FindManyAsync(Arg.Any<RoleFilter>(), Arg.Any<CancellationToken>()).Returns(
        [
            new Role { Id = "upper", Name = "Admin" },
            new Role { Id = "lower", Name = "admin" }
        ]);

        Assert.Equal("lower", (await store.FindByNameAsync("admin"))?.Id);
        // Neither matches exactly, so the case-insensitive tie is broken by ID.
        Assert.Equal("lower", (await store.FindByNameAsync("ADMIN"))?.Id);
    }

    [Fact]
    public void RoleFilterAppliesTheName()
    {
        var roles = new[]
        {
            new Role { Id = "1", Name = "Operators" },
            new Role { Id = "2", Name = "Auditors" }
        }.AsQueryable();

        var filtered = new RoleFilter { Name = "Auditors" }.Apply(roles).ToList();

        Assert.Equal("2", Assert.Single(filtered).Id);
    }

    [Fact]
    public async Task FindByNameAsyncFindsANameThatDiffersOnlyInCaseOnACaseSensitiveStore()
    {
        var store = new MemoryRoleStore(new MemoryStore<Role>(), TestTenantAccessor.Default);
        await store.SaveAsync(new Role { Id = "generated-id", Name = "Admin" });

        Assert.Equal("generated-id", (await store.FindByNameAsync("admin"))?.Id);
    }

    [Fact]
    public async Task FindByNameAsyncCanSkipTenantAgnosticRoles()
    {
        var store = new MemoryRoleStore(new MemoryStore<Role>(), new TestTenantAccessor("tenant-a"));
        await store.SaveAsync(new Role { Id = "shared", Name = "admin", TenantId = Elsa.Common.Multitenancy.Tenant.AgnosticTenantId });

        Assert.Equal("shared", (await store.FindByNameAsync("admin"))?.Id);
        Assert.Null(await store.FindByNameAsync("admin", includeTenantAgnostic: false));
    }

    [Theory]
    [InlineData(false, "Operators")]
    [InlineData(true, "Operators")]
    [InlineData(false, "operators")]
    [InlineData(true, "operators")]
    public async Task FindByNameAsyncPrefersTheTenantsOwnRoleOverATenantAgnosticOneWhateverTheStoreOrder(bool agnosticFirst, string requestedName)
    {
        // The tenant role's ID sorts after the shared one's, so only the ownership preference can make it win.
        var tenantRole = new Role { Id = "z-tenant", Name = "Operators", TenantId = "tenant-a" };
        var sharedRole = new Role { Id = "a-shared", Name = "Operators", TenantId = Elsa.Common.Multitenancy.Tenant.AgnosticTenantId };
        var store = Substitute.For<IRoleStore>();
        store.FindManyAsync(Arg.Any<RoleFilter>(), Arg.Any<CancellationToken>())
            .Returns(agnosticFirst ? [sharedRole, tenantRole] : [tenantRole, sharedRole]);

        Assert.Equal("z-tenant", (await store.FindByNameAsync(requestedName))?.Id);
        Assert.Equal("z-tenant", (await store.FindByNameAsync(requestedName, includeTenantAgnostic: false))?.Id);
    }

    [Fact]
    public async Task FindByNameAsyncPrefersTheTenantsOwnRoleOverAnExactTenantAgnosticMatch()
    {
        var store = Substitute.For<IRoleStore>();
        store.FindManyAsync(Arg.Any<RoleFilter>(), Arg.Any<CancellationToken>()).Returns(
        [
            new Role { Id = "a-shared", Name = "operators", TenantId = Elsa.Common.Multitenancy.Tenant.AgnosticTenantId },
            new Role { Id = "z-tenant", Name = "Operators", TenantId = "tenant-a" }
        ]);

        Assert.Equal("z-tenant", (await store.FindByNameAsync("operators"))?.Id);
    }

    [Fact]
    public async Task FindByNameAsyncPrefersTheTenantsOwnRoleOnTheMemoryStore()
    {
        var store = new MemoryRoleStore(new MemoryStore<Role>(), new TestTenantAccessor("tenant-a"));
        await store.SaveAsync(new Role { Id = "a-shared", Name = "Operators", TenantId = Elsa.Common.Multitenancy.Tenant.AgnosticTenantId });
        await store.SaveAsync(new Role { Id = "z-tenant", Name = "Operators", TenantId = "tenant-a" });

        Assert.Equal("z-tenant", (await store.FindByNameAsync("Operators"))?.Id);
    }

    [Fact]
    public async Task FindByNameAsyncBreaksTiesById()
    {
        var store = Substitute.For<IRoleStore>();
        store.FindManyAsync(Arg.Any<RoleFilter>(), Arg.Any<CancellationToken>()).Returns(
        [
            new Role { Id = "role-b", Name = "Operators", TenantId = "tenant-a" },
            new Role { Id = "role-a", Name = "Operators", TenantId = "tenant-a" }
        ]);

        Assert.Equal("role-a", (await store.FindByNameAsync("Operators"))?.Id);
    }

    [Fact]
    public async Task FindByNameAsyncKeepsTheNameFilterMatchWhenTheScanOmitsIt()
    {
        // A store may return a role for the name filter that its unfiltered listing leaves out; the scan is ranked
        // together with the name filter's candidates, so that match is not lost.
        var sharedRole = new Role { Id = "shared", Name = "Operators", TenantId = Elsa.Common.Multitenancy.Tenant.AgnosticTenantId };
        var store = Substitute.For<IRoleStore>();
        store.FindManyAsync(Arg.Is<RoleFilter>(x => x.Name != null), Arg.Any<CancellationToken>()).Returns([sharedRole]);
        store.FindManyAsync(Arg.Is<RoleFilter>(x => x.Name == null), Arg.Any<CancellationToken>()).Returns([]);

        Assert.Equal("shared", (await store.FindByNameAsync("Operators"))?.Id);
        Assert.Null(await store.FindByNameAsync("Operators", includeTenantAgnostic: false));
    }

    [Fact]
    public async Task FindByNameAsyncPrefersATenantCaseVariantFoundByTheScanOverATenantAgnosticExactMatch()
    {
        var sharedRole = new Role { Id = "a-shared", Name = "operators", TenantId = Elsa.Common.Multitenancy.Tenant.AgnosticTenantId };
        var tenantRole = new Role { Id = "z-tenant", Name = "Operators", TenantId = "tenant-a" };
        var store = Substitute.For<IRoleStore>();
        // An exact-match store: the name filter only finds the shared role.
        store.FindManyAsync(Arg.Is<RoleFilter>(x => x.Name != null), Arg.Any<CancellationToken>()).Returns([sharedRole]);
        store.FindManyAsync(Arg.Is<RoleFilter>(x => x.Name == null), Arg.Any<CancellationToken>()).Returns([sharedRole, tenantRole]);

        Assert.Equal("z-tenant", (await store.FindByNameAsync("operators"))?.Id);
    }
}
