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
        Assert.Equal("upper", (await store.FindByNameAsync("ADMIN"))?.Id);
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
}
