using Elsa.Common.Multitenancy;
using System.Threading.Tasks;

namespace Elsa.Common.UnitTests.Multitenancy;

public class TenantResolverContextTests
{
    [Test]
    [Arguments(null, "Default")]
    [Arguments("", "Default")]
    [Arguments("tenant1", "Tenant 1")]
    [Arguments("tenant2", "Tenant 2")]
    public async Task FindTenant_ById_FindsCorrectTenant(string? tenantId, string expectedName)
    {
        // Arrange
        var context = CreateContext();

        // Act
        var result = context.FindTenant(tenantId!);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Name).IsEqualTo(expectedName);
    }

    [Test]
    public async Task FindTenant_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var result = context.FindTenant("non-existent");

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    [Arguments("Alpha", "tenant1", "Tenant Alpha")]
    [Arguments("Beta", "tenant2", "Tenant Beta")]
    public async Task FindTenant_WithPredicate_FindsMatchingTenant(string searchTerm, string expectedId, string expectedName)
    {
        // Arrange
        var context = CreateContextWithNamedTenants();

        // Act
        var result = context.FindTenant(t => t.Name.Contains(searchTerm));

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Id).IsEqualTo(expectedId);
        await Assert.That(result.Name).IsEqualTo(expectedName);
    }

    [Test]
    public async Task FindTenant_WithPredicate_NoMatch_ReturnsNull()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var result = context.FindTenant(t => t.Name == "NonExistent");

        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Constructor_StoresCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        var context = new TenantResolverContext(new Dictionary<string, Tenant>(), cts.Token);

        // Assert
        await Assert.That(context.CancellationToken).IsEqualTo(cts.Token);
    }

    [Test]
    public async Task FindTenant_NormalizesNullAndEmptyStringToSameValue()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var resultFromNull = context.FindTenant((string?)null);
        var resultFromEmptyString = context.FindTenant(string.Empty);

        // Assert
        await Assert.That(resultFromNull).IsNotNull();
        await Assert.That(resultFromEmptyString).IsNotNull();
        await Assert.That(resultFromEmptyString).IsSameReferenceAs(resultFromNull);
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