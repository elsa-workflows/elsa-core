using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Persistence.MongoDb.Common;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NSubstitute;

namespace Elsa.MongoDb.UnitTests;

public class MongoStoreRegistrationTests
{
    [Fact(DisplayName = "A MongoDB store resolves in a host that has not enabled multitenancy")]
    public void AddStore_WithoutMultitenancy_ResolvesStoreAgainstDefaultTenantAccessor()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped(_ => Substitute.For<IMongoCollection<ProbeDocument>>());
        new ProbeFeature(services.CreateModule()).Configure();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Act
        var store = scope.ServiceProvider.GetRequiredService<ProbeStore>();

        // Assert
        Assert.NotNull(store.Inner);
        Assert.IsType<DefaultTenantAccessor>(scope.ServiceProvider.GetRequiredService<ITenantAccessor>());
    }

    public class ProbeDocument;

    public class ProbeStore(MongoDbStore<ProbeDocument> inner)
    {
        public MongoDbStore<ProbeDocument> Inner { get; } = inner;
    }

    private class ProbeFeature(IModule module) : PersistenceFeatureBase(module)
    {
        public override void Configure() => AddStore<ProbeDocument, ProbeStore>();
    }
}
