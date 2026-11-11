using Elsa.Common.Multitenancy;
using Elsa.Tenants.Options;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Elsa.Tenants.UnitTests.Services;

public class DefaultTenantResolverPipelineInvokerTests
{
    [Fact]
    public async Task InvokePipelineAsync_WithEmptyStringTenantId_FindsDefaultTenant()
    {
        // Arrange
        var tenants = CreateDefaultTenantList();
        var (invoker, _) = CreateInvoker(tenants, TenantResolverResult.Resolved(""));

        // Act
        var result = await invoker.InvokePipelineAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Default", result.Name);
    }

    [Fact]
    public async Task InvokePipelineAsync_WithValidTenantId_FindsCorrectTenant()
    {
        // Arrange
        var tenants = CreateDefaultTenantList();
        var (invoker, _) = CreateInvoker(tenants, TenantResolverResult.Resolved("tenant1"));

        // Act
        var result = await invoker.InvokePipelineAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Tenant 1", result.Name);
    }

    [Fact]
    public async Task InvokePipelineAsync_WithNonExistentTenantId_ReturnsNull()
    {
        // Arrange
        var tenants = CreateDefaultTenantList();
        var (invoker, _) = CreateInvoker(tenants, TenantResolverResult.Resolved("non-existent"));

        // Act
        var result = await invoker.InvokePipelineAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task InvokePipelineAsync_WithNullTenantIdsInList_DoesNotThrowDictionaryException()
    {
        // Arrange - Simulates legacy data with null IDs
        var tenants = new List<Tenant>
        {
            new() { Id = null!, Name = "Legacy Null Tenant" },
            new() { Id = "tenant1", Name = "Tenant 1" }
        };
        var (invoker, _) = CreateInvoker(tenants, TenantResolverResult.Unresolved());

        // Act & Assert - Should not throw
        var result = await invoker.InvokePipelineAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task InvokePipelineAsync_WithUnresolvedResult_ReturnsNull()
    {
        // Arrange
        var tenants = CreateDefaultTenantList();
        var (invoker, _) = CreateInvoker(tenants, TenantResolverResult.Unresolved());

        // Act
        var result = await invoker.InvokePipelineAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task InvokePipelineAsync_WithMultipleResolvers_UsesFirstResolvedResult()
    {
        // Arrange
        var tenants = CreateDefaultTenantList();
        var mockResolver1 = CreateMockResolver(TenantResolverResult.Resolved("tenant1"));
        var mockResolver2 = CreateMockResolver(TenantResolverResult.Resolved("tenant2"));
        var invoker = CreateInvokerWithMultipleResolvers(tenants, mockResolver1, mockResolver2);

        // Act
        var result = await invoker.InvokePipelineAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("tenant1", result.Id);
        await mockResolver2.DidNotReceive().ResolveAsync(Arg.Any<TenantResolverContext>());
    }

    [Fact]
    public async Task InvokePipelineAsync_WithLegacyNullTenantIds_NormalizesAndFindsDefaultTenant()
    {
        // Arrange - Simulates legacy data with null ID that gets normalized
        var tenants = new List<Tenant>
        {
            new() { Id = null!, Name = "Legacy" }, // Will be normalized to ""
            new() { Id = "tenant1", Name = "Tenant 1" }
        };
        var (invoker, _) = CreateInvoker(tenants, TenantResolverResult.Resolved(""));

        // Act
        var result = await invoker.InvokePipelineAsync();

        // Assert - The null tenant ID gets normalized to "" in dictionary, so it should be found
        Assert.NotNull(result);
        Assert.Equal("Legacy", result.Name); // Should find the Legacy tenant (normalized from null)
    }

    [Fact]
    public async Task InvokePipelineAsync_WhenOnlyTheDefaultResolverResolves_ReturnsDefaultTenantByDefault()
    {
        // Arrange
        var invoker = CreateInvokerWithMultipleResolvers(
            CreateDefaultTenantList(),
            rejectUnresolvedTenants: false,
            CreateMockResolver(TenantResolverResult.Unresolved()),
            new DefaultTenantResolver());

        // Act
        var result = await invoker.InvokePipelineAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(Tenant.DefaultTenantId, result.Id);
    }

    [Fact]
    public async Task InvokePipelineAsync_WhenRejectingUnresolvedTenants_DoesNotFallBackToTheDefaultResolver()
    {
        // Arrange
        var invoker = CreateInvokerWithMultipleResolvers(
            CreateDefaultTenantList(),
            rejectUnresolvedTenants: true,
            CreateMockResolver(TenantResolverResult.Unresolved()),
            new DefaultTenantResolver());

        // Act
        var result = await invoker.InvokePipelineAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task InvokePipelineAsync_WhenRejectingUnresolvedTenants_StillResolvesAnExplicitDefaultTenant()
    {
        // Arrange
        var invoker = CreateInvokerWithMultipleResolvers(
            CreateDefaultTenantList(),
            rejectUnresolvedTenants: true,
            CreateMockResolver(TenantResolverResult.Resolved(string.Empty)),
            new DefaultTenantResolver());

        // Act
        var result = await invoker.InvokePipelineAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(Tenant.DefaultTenantId, result.Id);
    }

    [Fact]
    public async Task InvokePipelineAsync_WhenRejectingUnresolvedTenants_ReturnsNullForUnknownTenantId()
    {
        // Arrange
        var invoker = CreateInvokerWithMultipleResolvers(
            CreateDefaultTenantList(),
            rejectUnresolvedTenants: true,
            CreateMockResolver(TenantResolverResult.Resolved("non-existent")),
            new DefaultTenantResolver());

        // Act
        var result = await invoker.InvokePipelineAsync();

        // Assert
        Assert.Null(result);
    }

    // Helper methods
    private static List<Tenant> CreateDefaultTenantList() => new()
    {
        new() { Id = Tenant.DefaultTenantId, Name = "Default" },
        new() { Id = "tenant1", Name = "Tenant 1" },
        new() { Id = "tenant2", Name = "Tenant 2" }
    };

    private static ITenantService CreateTenantService(List<Tenant> tenants)
    {
        var tenantsById = tenants.ToDictionary(x => x.Id.NormalizeTenantId());
        var tenantService = Substitute.For<ITenantService>();
        tenantService.FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => tenantsById.GetValueOrDefault(call.Arg<string>().NormalizeTenantId()));
        tenantService.ListAsync(Arg.Any<CancellationToken>()).Returns(tenants);
        return tenantService;
    }

    private static ITenantResolver CreateMockResolver(TenantResolverResult result)
    {
        var mockResolver = Substitute.For<ITenantResolver>();
        mockResolver.ResolveAsync(Arg.Any<TenantResolverContext>()).Returns(result);
        return mockResolver;
    }

    private static (DefaultTenantResolverPipelineInvoker Invoker, ITenantResolver Resolver) CreateInvoker(
        List<Tenant> tenants,
        TenantResolverResult resolverResult)
    {
        var tenantService = CreateTenantService(tenants);

        var mockResolver = CreateMockResolver(resolverResult);

        // Use a mock pipeline builder that directly returns our mock resolver
        var pipelineBuilder = Substitute.For<ITenantResolverPipelineBuilder>();
        pipelineBuilder.Build(Arg.Any<IServiceProvider>()).Returns(new[] { mockResolver });

        var options = Microsoft.Extensions.Options.Options.Create(new MultitenancyOptions
        {
            TenantResolverPipelineBuilder = pipelineBuilder
        });

        var serviceProvider = Substitute.For<IServiceProvider>();
        var logger = NullLogger<DefaultTenantResolverPipelineInvoker>.Instance;
        var invoker = new DefaultTenantResolverPipelineInvoker(options, tenantService, serviceProvider, logger);

        return (invoker, mockResolver);
    }

    private static DefaultTenantResolverPipelineInvoker CreateInvokerWithMultipleResolvers(
        List<Tenant> tenants,
        params ITenantResolver[] resolvers) => CreateInvokerWithMultipleResolvers(tenants, rejectUnresolvedTenants: false, resolvers);

    private static DefaultTenantResolverPipelineInvoker CreateInvokerWithMultipleResolvers(
        List<Tenant> tenants,
        bool rejectUnresolvedTenants,
        params ITenantResolver[] resolvers)
    {
        var tenantService = CreateTenantService(tenants);

        // Use a mock pipeline builder that directly returns our mock resolvers
        var pipelineBuilder = Substitute.For<ITenantResolverPipelineBuilder>();
        pipelineBuilder.Build(Arg.Any<IServiceProvider>()).Returns(resolvers);

        var options = Microsoft.Extensions.Options.Options.Create(new MultitenancyOptions
        {
            TenantResolverPipelineBuilder = pipelineBuilder,
            RejectUnresolvedTenants = rejectUnresolvedTenants
        });

        var serviceProvider = Substitute.For<IServiceProvider>();
        var logger = NullLogger<DefaultTenantResolverPipelineInvoker>.Instance;
        return new(options, tenantService, serviceProvider, logger);
    }
}
