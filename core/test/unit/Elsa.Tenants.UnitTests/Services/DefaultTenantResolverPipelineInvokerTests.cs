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

    // Helper methods
    private static List<Tenant> CreateDefaultTenantList() => new()
    {
        new() { Id = Tenant.DefaultTenantId, Name = "Default" },
        new() { Id = "tenant1", Name = "Tenant 1" },
        new() { Id = "tenant2", Name = "Tenant 2" }
    };

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
        var tenantsProvider = Substitute.For<ITenantsProvider>();
        tenantsProvider.ListAsync(Arg.Any<CancellationToken>()).Returns(tenants);

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
        var invoker = new DefaultTenantResolverPipelineInvoker(options, tenantsProvider, serviceProvider, logger);

        return (invoker, mockResolver);
    }

    private static DefaultTenantResolverPipelineInvoker CreateInvokerWithMultipleResolvers(
        List<Tenant> tenants,
        params ITenantResolver[] resolvers)
    {
        var tenantsProvider = Substitute.For<ITenantsProvider>();
        tenantsProvider.ListAsync(Arg.Any<CancellationToken>()).Returns(tenants);

        // Use a mock pipeline builder that directly returns our mock resolvers
        var pipelineBuilder = Substitute.For<ITenantResolverPipelineBuilder>();
        pipelineBuilder.Build(Arg.Any<IServiceProvider>()).Returns(resolvers);

        var options = Microsoft.Extensions.Options.Options.Create(new MultitenancyOptions
        {
            TenantResolverPipelineBuilder = pipelineBuilder
        });

        var serviceProvider = Substitute.For<IServiceProvider>();
        var logger = NullLogger<DefaultTenantResolverPipelineInvoker>.Instance;
        return new(options, tenantsProvider, serviceProvider, logger);
    }
}
