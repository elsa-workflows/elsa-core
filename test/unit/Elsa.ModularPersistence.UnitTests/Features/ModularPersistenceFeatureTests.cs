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
        var materializer = new FakeStorageManifestMaterializer("Fake");

        module.UseModularPersistence(feature => feature.RegisterManifest(manifest));
        services.AddSingleton<IStorageManifestMaterializer>(materializer);
        module.Apply();

        var startupTask = services.BuildServiceProvider().GetServices<IStartupTask>().OfType<Elsa.ModularPersistence.Services.ModularPersistenceMaterializationStartupTask>().Single();
        await startupTask.ExecuteAsync(CancellationToken.None);

        Assert.Equal([manifest], materializer.MaterializedManifests);
    }

    [Fact]
    public async Task StartupTaskMaterializesOnlyConfiguredProvider()
    {
        var services = new ServiceCollection();
        var module = services.CreateModule();
        var manifest = CreateManifest("sample.secrets");
        var selectedMaterializer = new FakeStorageManifestMaterializer("Selected");
        var skippedMaterializer = new FakeStorageManifestMaterializer("Skipped");

        module.UseModularPersistence(feature =>
        {
            feature.UseProvider("Selected");
            feature.RegisterManifest(manifest);
        });
        services.AddSingleton<IStorageManifestMaterializer>(selectedMaterializer);
        services.AddSingleton<IStorageManifestMaterializer>(skippedMaterializer);
        module.Apply();

        var startupTask = services.BuildServiceProvider().GetServices<IStartupTask>().OfType<Elsa.ModularPersistence.Services.ModularPersistenceMaterializationStartupTask>().Single();
        await startupTask.ExecuteAsync(CancellationToken.None);

        Assert.Equal([manifest], selectedMaterializer.MaterializedManifests);
        Assert.Empty(skippedMaterializer.MaterializedManifests);
    }

    [Fact]
    public async Task StartupTaskHonorsMaterializeOnStartupOption()
    {
        var services = new ServiceCollection();
        var module = services.CreateModule();
        var manifest = CreateManifest("sample.secrets");
        var materializer = new FakeStorageManifestMaterializer("Fake");

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

    [Fact]
    public async Task DiagnosticsReportsConfiguredProviderManifestVersionsAndFailures()
    {
        var services = new ServiceCollection();
        var module = services.CreateModule();
        var manifest = CreateManifest("sample.secrets");
        var materializer = new FakeStorageManifestMaterializer("Failing") { ThrowOnMaterialize = true };

        module.UseModularPersistence(feature =>
        {
            feature.UseProvider("Failing");
            feature.RegisterManifest(manifest);
        });
        services.AddSingleton<IStorageManifestMaterializer>(materializer);
        module.Apply();

        var serviceProvider = services.BuildServiceProvider();
        var startupTask = serviceProvider.GetServices<IStartupTask>().OfType<Elsa.ModularPersistence.Services.ModularPersistenceMaterializationStartupTask>().Single();
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await startupTask.ExecuteAsync(CancellationToken.None));

        var diagnostics = serviceProvider.GetRequiredService<IModularPersistenceDiagnosticsService>().GetDiagnostics();

        Assert.Equal("Failing", diagnostics.SelectedProviderName);
        Assert.True(diagnostics.MaterializeOnStartup);
        var provider = Assert.Single(diagnostics.Providers);
        Assert.Equal("Failing", provider.ProviderName);
        Assert.True(provider.IsSelected);
        Assert.Equal(1, provider.MaterializableManifestCount);
        var manifestDiagnostic = Assert.Single(diagnostics.Manifests);
        Assert.Equal("sample.secrets", manifestDiagnostic.SchemaName);
        Assert.Equal("1.0.0", manifestDiagnostic.Version);
        var failure = Assert.Single(diagnostics.MaterializationFailures);
        Assert.Equal("Failing", failure.ProviderName);
        Assert.Equal("sample.secrets", failure.SchemaName);
        Assert.Equal("InvalidOperationException", failure.ErrorType);
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

    private sealed class FakeStorageManifestMaterializer(string providerName) : IStorageManifestMaterializer
    {
        public List<StorageManifestDescriptor> MaterializedManifests { get; } = [];

        public string ProviderName { get; } = providerName;

        public bool ThrowOnMaterialize { get; set; }

        public bool CanMaterialize(StorageManifestDescriptor manifest) => true;

        public ValueTask MaterializeAsync(StorageManifestDescriptor manifest, CancellationToken cancellationToken = default)
        {
            if (ThrowOnMaterialize)
                throw new InvalidOperationException("Materialization failed.");

            MaterializedManifests.Add(manifest);
            return ValueTask.CompletedTask;
        }
    }
}
