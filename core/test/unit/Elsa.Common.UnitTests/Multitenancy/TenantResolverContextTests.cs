using Elsa.Common.Multitenancy;
using NSubstitute;

namespace Elsa.Common.UnitTests.Multitenancy;

public class TenantResolverContextTests
{
    [Theory]
    [InlineData(null, "Default")]
    [InlineData("", "Default")]
    [InlineData("tenant1", "Tenant 1")]
    [InlineData("tenant2", "Tenant 2")]
    public void FindTenant_ById_FindsCorrectTenant(string? tenantId, string expectedName)
    {
        // Arrange
        var context = CreateContext();

        // Act
        var result = context.FindTenant(tenantId!);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedName, result.Name);
    }

    [Fact]
    public void FindTenant_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var result = context.FindTenant("non-existent");

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("Alpha", "tenant1", "Tenant Alpha")]
    [InlineData("Beta", "tenant2", "Tenant Beta")]
    public void FindTenant_WithPredicate_FindsMatchingTenant(string searchTerm, string expectedId, string expectedName)
    {
        // Arrange
        var context = CreateContextWithNamedTenants();

        // Act
        var result = context.FindTenant(t => t.Name.Contains(searchTerm));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedId, result.Id);
        Assert.Equal(expectedName, result.Name);
    }

    [Fact]
    public void FindTenant_WithPredicate_NoMatch_ReturnsNull()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var result = context.FindTenant(t => t.Name == "NonExistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Constructor_StoresCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        var context = new TenantResolverContext(new Dictionary<string, Tenant>(), cts.Token);

        // Assert
        Assert.Equal(cts.Token, context.CancellationToken);
    }

    [Fact]
    public void FindTenant_NormalizesNullAndEmptyStringToSameValue()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var resultFromNull = context.FindTenant((string?)null);
        var resultFromEmptyString = context.FindTenant(string.Empty);

        // Assert
        Assert.NotNull(resultFromNull);
        Assert.NotNull(resultFromEmptyString);
        Assert.Same(resultFromNull, resultFromEmptyString);
    }

    [Fact]
    public void FindTenant_ById_WithTenantService_NormalizesAndReadsFromTheService()
    {
        // Arrange
        var tenantService = Substitute.For<ITenantService>();
        tenantService.FindAsync(string.Empty, Arg.Any<CancellationToken>()).Returns(Tenant.Default);
        var context = new TenantResolverContext(tenantService, CancellationToken.None);

        // Act
        var result = context.FindTenant((string?)null);

        // Assert
        Assert.Same(Tenant.Default, result);
    }

    [Fact]
    public async Task FindTenantAsync_WithTenantService_FindsMatchingTenantAndOnlyListsWhenAsked()
    {
        // Arrange
        var tenant = new Tenant { Id = "tenant1", Name = "Tenant 1" };
        var tenantService = Substitute.For<ITenantService>();
        tenantService.ListAsync(Arg.Any<CancellationToken>()).Returns([Tenant.Default, tenant]);
        var context = new TenantResolverContext(tenantService, CancellationToken.None);
        await tenantService.DidNotReceive().ListAsync(Arg.Any<CancellationToken>());

        // Act
        var result = await context.FindTenantAsync(x => x.Name == "Tenant 1");

        // Assert
        Assert.Same(tenant, result);
    }

    // Helper methods
    private static TenantResolverContext CreateContext()
    {
        var tenants = new Dictionary<string, Tenant>
        {
            {
                Tenant.DefaultTenantId, new()
                {
                    Id = Tenant.DefaultTenantId,
                    Name = "Default"
                }
            },
            {
                "tenant1", new()
                {
                    Id = "tenant1",
                    Name = "Tenant 1"
                }
            },
            {
                "tenant2", new()
                {
                    Id = "tenant2",
                    Name = "Tenant 2"
                }
            }
        };
        return new(tenants, CancellationToken.None);
    }

    private static TenantResolverContext CreateContextWithNamedTenants()
    {
        var tenants = new Dictionary<string, Tenant>
        {
            {
                Tenant.DefaultTenantId, new()
                {
                    Id = Tenant.DefaultTenantId,
                    Name = "Default"
                }
            },
            {
                "tenant1", new()
                {
                    Id = "tenant1",
                    Name = "Tenant Alpha"
                }
            },
            {
                "tenant2", new()
                {
                    Id = "tenant2",
                    Name = "Tenant Beta"
                }
            }
        };
        return new(tenants, CancellationToken.None);
    }
}