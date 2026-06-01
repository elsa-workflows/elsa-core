using Elsa.Common;
using Elsa.Extensions;
using Elsa.ModularPersistence.Contracts;
using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Extensions;
using Elsa.ModularPersistence.Planning;
using Elsa.ModularPersistence.Services;
using Elsa.ModularPersistence.Validation;
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
    public async Task StartupTaskRetriesMaterializationFailuresBeforeSucceeding()
    {
        var services = new ServiceCollection();
        var module = services.CreateModule();
        var manifest = CreateManifest("sample.secrets");
        var materializer = new FakeStorageManifestMaterializer("Retry") { FailuresBeforeSuccess = 1 };

        module.UseModularPersistence(feature =>
        {
            feature.ConfigureOptions = options =>
            {
                options.MaterializationRetryCount = 2;
                options.MaterializationRetryDelay = TimeSpan.Zero;
            };
            feature.RegisterManifest(manifest);
        });
        services.AddSingleton<IStorageManifestMaterializer>(materializer);
        module.Apply();

        var serviceProvider = services.BuildServiceProvider();
        var startupTask = serviceProvider.GetServices<IStartupTask>().OfType<Elsa.ModularPersistence.Services.ModularPersistenceMaterializationStartupTask>().Single();
        await startupTask.ExecuteAsync(CancellationToken.None);

        var tracker = serviceProvider.GetRequiredService<IStorageManifestMaterializationTracker>();
        Assert.Equal(2, materializer.Attempts);
        Assert.Equal([manifest], materializer.MaterializedManifests);
        Assert.Single(tracker.Records, x => !x.Succeeded);
        Assert.Single(tracker.Records, x => x.Succeeded);
    }

    [Fact]
    public async Task StartupTaskWrapsFinalFailureWithActionableContext()
    {
        var services = new ServiceCollection();
        var module = services.CreateModule();
        var manifest = CreateManifest("sample.secrets");
        var materializer = new FakeStorageManifestMaterializer("Retry") { FailuresBeforeSuccess = 10 };

        module.UseModularPersistence(feature =>
        {
            feature.ConfigureOptions = options =>
            {
                options.MaterializationRetryCount = 2;
                options.MaterializationRetryDelay = TimeSpan.Zero;
            };
            feature.RegisterManifest(manifest);
        });
        services.AddSingleton<IStorageManifestMaterializer>(materializer);
        module.Apply();

        var serviceProvider = services.BuildServiceProvider();
        var startupTask = serviceProvider.GetServices<IStartupTask>().OfType<Elsa.ModularPersistence.Services.ModularPersistenceMaterializationStartupTask>().Single();
        var exception = await Assert.ThrowsAsync<StorageManifestMaterializationException>(async () => await startupTask.ExecuteAsync(CancellationToken.None));
        var tracker = serviceProvider.GetRequiredService<IStorageManifestMaterializationTracker>();

        Assert.Equal("Retry", exception.ProviderName);
        Assert.Equal("sample.secrets", exception.SchemaName);
        Assert.Equal("1.0.0", exception.Version);
        Assert.Equal(3, exception.AttemptCount);
        Assert.Contains("idempotent schema/history operations", exception.Message);
        Assert.Equal(3, materializer.Attempts);
        Assert.Equal(3, tracker.Records.Count(x => !x.Succeeded));
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
        services.AddStorageProviderCapabilities("Failing", ProviderCapabilities.PortableDocument);
        module.Apply();

        var serviceProvider = services.BuildServiceProvider();
        var startupTask = serviceProvider.GetServices<IStartupTask>().OfType<Elsa.ModularPersistence.Services.ModularPersistenceMaterializationStartupTask>().Single();
        await Assert.ThrowsAsync<StorageManifestMaterializationException>(async () => await startupTask.ExecuteAsync(CancellationToken.None));

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
        var physicalizationPlan = Assert.Single(diagnostics.PhysicalizationPlans);
        Assert.Equal("Failing", physicalizationPlan.ProviderName);
        Assert.True(physicalizationPlan.IsSupported);
        var failure = Assert.Single(diagnostics.MaterializationFailures);
        Assert.Equal("Failing", failure.ProviderName);
        Assert.Equal("sample.secrets", failure.SchemaName);
        Assert.Equal("InvalidOperationException", failure.ErrorType);
    }

    [Fact]
    public void DiagnosticsReportsUnsupportedPhysicalizationPlans()
    {
        var services = new ServiceCollection();
        var module = services.CreateModule();
        var manifest = new StorageManifestDescriptor(
            "sample.optimized",
            new StorageManifestVersion(1),
            [
                new StorageUnitDescriptor(
                    "Customers",
                    [
                        new StorageFieldDescriptor("Id", StorageFieldType.String, true),
                        new StorageFieldDescriptor("Email", StorageFieldType.String)
                    ],
                    [
                        new StorageKeyDescriptor("PK_Customers", ["Id"])
                    ],
                    [
                        new StorageIndexDescriptor("IX_Customers_Email", [new StorageIndexFieldDescriptor("Email")], physicalizationIntent: PhysicalizationIntent.OptimizedIndexes)
                    ])
            ]);

        module.UseModularPersistence(feature =>
        {
            feature.UseProvider("Portable");
            feature.RegisterManifest(manifest);
        });
        services.AddSingleton<IStorageManifestMaterializer>(new FakeStorageManifestMaterializer("Portable"));
        services.AddStorageProviderCapabilities("Portable", ProviderCapabilities.PortableDocument);
        module.Apply();

        var diagnostics = services.BuildServiceProvider().GetRequiredService<IModularPersistenceDiagnosticsService>().GetDiagnostics();

        var plan = Assert.Single(diagnostics.PhysicalizationPlans);
        Assert.False(plan.IsSupported);
        var unsupported = Assert.Single(plan.Items, x => x.Status == PhysicalizationPlanStatus.Unsupported);
        Assert.Equal("IX_Customers_Email", unsupported.Name);
        Assert.Contains("OptimizedIndexes", unsupported.Message);
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

        public int FailuresBeforeSuccess { get; set; }

        public int Attempts { get; private set; }

        public bool CanMaterialize(StorageManifestDescriptor manifest) => true;

        public ValueTask MaterializeAsync(StorageManifestDescriptor manifest, CancellationToken cancellationToken = default)
        {
            Attempts++;
            if (ThrowOnMaterialize)
                throw new InvalidOperationException("Materialization failed.");
            if (Attempts <= FailuresBeforeSuccess)
                throw new InvalidOperationException($"Materialization failed on attempt {Attempts}.");

            MaterializedManifests.Add(manifest);
            return ValueTask.CompletedTask;
        }
    }
}
