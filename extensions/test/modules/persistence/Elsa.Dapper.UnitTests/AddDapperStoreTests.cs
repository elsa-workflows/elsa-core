using Elsa.Common.Multitenancy;
using Elsa.Persistence.Dapper.Contracts;
using Elsa.Persistence.Dapper.Extensions;
using Elsa.Persistence.Dapper.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using DapperRecord = Elsa.Persistence.Dapper.Records.Record;

namespace Elsa.Dapper.UnitTests;

public class AddDapperStoreTests
{
    [Fact(DisplayName = "A custom Dapper store resolves in a host that has not enabled multitenancy")]
    public void AddDapperStore_WithoutMultitenancy_ResolvesStoreAgainstDefaultTenantAccessor()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IDbConnectionProvider>());
        services.AddDapperStore<ProbeStore, DapperRecord>("Probes");
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Act
        var store = scope.ServiceProvider.GetRequiredService<ProbeStore>();

        // Assert
        Assert.NotNull(store.Inner);
        Assert.IsType<DefaultTenantAccessor>(scope.ServiceProvider.GetRequiredService<ITenantAccessor>());
    }

    public class ProbeStore(Store<DapperRecord> inner)
    {
        public Store<DapperRecord> Inner { get; } = inner;
    }
}
