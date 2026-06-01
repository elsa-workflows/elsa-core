using Elsa.Common;
using Elsa.Extensions;
using Elsa.ModularPersistence.Contracts;
using Elsa.ModularPersistence.Descriptors;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ModularPersistence.UnitTests.Features;

public class ModularPersistenceFeatureTests
{
    [Fact]
    public void UseModularPersistenceRegistersManifests()
    {
        var services = new ServiceCollection();
        var module = services.CreateModule();
        var manifest = CreateManifest("sample.secrets");

        module.UseModularPersistence(feature => feature.RegisterManifest(manifest));
        module.Apply();

        var registry = services.BuildServiceProvider().GetRequiredService<IStorageManifestRegistry>();
        var registered = Assert.Single(registry.Manifests);
        Assert.Same(manifest, registered);
    }

    [Fact]
    public async Task StartupTaskMaterializesRegisteredManifestsWithSelectedProvider()
    {
        var services = new ServiceCollection();
        var module = services.CreateModule();
        var manifest = CreateManifest("sample.secrets");
        var materializer = new FakeStorageManifestMaterializer();

        module.UseModularPersistence(feature => feature.RegisterManifest(manifest));
        services.AddSingleton<IStorageManifestMaterializer>(materializer);
        module.Apply();

        var startupTask = services.BuildServiceProvider().GetServices<IStartupTask>().OfType<Elsa.ModularPersistence.Services.ModularPersistenceMaterializationStartupTask>().Single();
        await startupTask.ExecuteAsync(CancellationToken.None);

        Assert.Equal([manifest], materializer.MaterializedManifests);
    }

    [Fact]
    public async Task StartupTaskHonorsMaterializeOnStartupOption()
    {
        var services = new ServiceCollection();
        var module = services.CreateModule();
        var manifest = CreateManifest("sample.secrets");
        var materializer = new FakeStorageManifestMaterializer();

        module.UseModularPersistence(feature =>
        {
            feature.MaterializeOnStartup = false;
            feature.RegisterManifest(manifest);
        });
        services.AddSingleton<IStorageManifestMaterializer>(materializer);
        module.Apply();

        var startupTask = services.BuildServiceProvider().GetServices<IStartupTask>().OfType<Elsa.ModularPersistence.Services.ModularPersistenceMaterializationStartupTask>().Single();
        await startupTask.ExecuteAsync(CancellationToken.None);

        Assert.Empty(materializer.MaterializedManifests);
    }

    [Fact]
    public async Task StartupTaskFailsWhenManifestHasNoMaterializer()
    {
        var services = new ServiceCollection();
        var module = services.CreateModule();
        var manifest = CreateManifest("sample.secrets");

        module.UseModularPersistence(feature => feature.RegisterManifest(manifest));
        module.Apply();

        var startupTask = services.BuildServiceProvider().GetServices<IStartupTask>().OfType<Elsa.ModularPersistence.Services.ModularPersistenceMaterializationStartupTask>().Single();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await startupTask.ExecuteAsync(CancellationToken.None));

        Assert.Contains("sample.secrets", exception.Message);
    }

    private static StorageManifestDescriptor CreateManifest(string schemaName) =>
        new(
            schemaName,
            new StorageManifestVersion(1),
            [
                new StorageUnitDescriptor(
                    "Secrets",
                    [
                        new StorageFieldDescriptor("Id", StorageFieldType.String, true)
                    ],
                    [
                        new StorageKeyDescriptor("PK_Secrets", ["Id"])
                    ],
                    [])
            ]);

    private sealed class FakeStorageManifestMaterializer : IStorageManifestMaterializer
    {
        public List<StorageManifestDescriptor> MaterializedManifests { get; } = [];

        public bool CanMaterialize(StorageManifestDescriptor manifest) => true;

        public ValueTask MaterializeAsync(StorageManifestDescriptor manifest, CancellationToken cancellationToken = default)
        {
            MaterializedManifests.Add(manifest);
            return ValueTask.CompletedTask;
        }
    }
}
