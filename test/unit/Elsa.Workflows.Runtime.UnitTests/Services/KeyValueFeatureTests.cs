using Elsa.Common.Services;
using Elsa.Features.Services;
using Elsa.KeyValues.Contracts;
using Elsa.KeyValues.Entities;
using Elsa.KeyValues.Models;
using Elsa.KeyValues.Stores;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using ModuleKeyValueFeature = Elsa.KeyValues.Features.KeyValueFeature;
using ShellKeyValueFeature = Elsa.KeyValues.ShellFeatures.KeyValueFeature;

namespace Elsa.Workflows.Runtime.UnitTests.Services;

public class KeyValueFeatureTests
{
    private readonly IServiceCollection _services = new ServiceCollection();
    private readonly IModule _module = Substitute.For<IModule>();

    public KeyValueFeatureTests()
    {
        _module.Services.Returns(_services);
    }

    [Test]
    public async Task ModuleFeature_DoesNotRegisterMemoryStore_WhenKeyValueStoreIsCustomized()
    {
        var feature = new ModuleKeyValueFeature(_module)
        {
            KeyValueStore = _ => new CustomKeyValueStore()
        };

        feature.Apply();

        await Assert.That(_services).DoesNotContain(x => x.ServiceType == typeof(MemoryStore<SerializedKeyValuePair>));
        await Assert.That(_services).Contains(x => x.ServiceType == typeof(IKeyValueStore));
    }

    [Test]
    public async Task ModuleFeature_CanExplicitlyRegisterMemoryStore_WhenKeyValueStoreIsCustomized()
    {
        var feature = new ModuleKeyValueFeature(_module)
        {
            KeyValueStore = sp => ActivatorUtilities.CreateInstance<MemoryKeyValueStore>(sp),
            RegisterMemoryStore = true
        };

        feature.Apply();

        await Assert.That(_services).Contains(x => x.ServiceType == typeof(MemoryStore<SerializedKeyValuePair>));
    }

    [Test]
    public async Task ModuleFeature_ConfiguredKeyValueStoreOverridesExistingRegistration()
    {
        _services.AddScoped<IKeyValueStore>(_ => new ExistingKeyValueStore());
        var feature = new ModuleKeyValueFeature(_module)
        {
            KeyValueStore = _ => new CustomKeyValueStore()
        };

        feature.Apply();

        using var provider = _services.BuildServiceProvider();
        var store = provider.GetRequiredService<IKeyValueStore>();
        await Assert.That(store).IsOfType(typeof(CustomKeyValueStore));
        _ = (CustomKeyValueStore)store;
    }

    [Test]
    public async Task ShellFeature_DoesNotRegisterMemoryStore_WhenKeyValueStoreIsCustomized()
    {
        var feature = new ShellKeyValueFeature
        {
            KeyValueStore = _ => new CustomKeyValueStore()
        };

        feature.ConfigureServices(_services);

        await Assert.That(_services).DoesNotContain(x => x.ServiceType == typeof(MemoryStore<SerializedKeyValuePair>));
        await Assert.That(_services).Contains(x => x.ServiceType == typeof(IKeyValueStore));
    }

    [Test]
    public async Task ShellFeature_CanExplicitlyRegisterMemoryStore_WhenKeyValueStoreIsCustomized()
    {
        var feature = new ShellKeyValueFeature
        {
            KeyValueStore = sp => ActivatorUtilities.CreateInstance<MemoryKeyValueStore>(sp),
            RegisterMemoryStore = true
        };

        feature.ConfigureServices(_services);

        await Assert.That(_services).Contains(x => x.ServiceType == typeof(MemoryStore<SerializedKeyValuePair>));
    }

    [Test]
    public async Task ShellFeature_ConfiguredKeyValueStoreOverridesExistingRegistration()
    {
        _services.AddScoped<IKeyValueStore>(_ => new ExistingKeyValueStore());
        var feature = new ShellKeyValueFeature
        {
            KeyValueStore = _ => new CustomKeyValueStore()
        };

        feature.ConfigureServices(_services);

        using var provider = _services.BuildServiceProvider();
        var store = provider.GetRequiredService<IKeyValueStore>();
        await Assert.That(store).IsOfType(typeof(CustomKeyValueStore));
        _ = (CustomKeyValueStore)store;
    }

    private class ExistingKeyValueStore : TestKeyValueStore;

    private class CustomKeyValueStore : TestKeyValueStore;

    private abstract class TestKeyValueStore : IKeyValueStore
    {
        public Task SaveAsync(SerializedKeyValuePair keyValuePair, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<SerializedKeyValuePair?> FindAsync(KeyValueFilter filter, CancellationToken cancellationToken) => Task.FromResult<SerializedKeyValuePair?>(null);

        public Task<IEnumerable<SerializedKeyValuePair>> FindManyAsync(KeyValueFilter filter, CancellationToken cancellationToken) => Task.FromResult<IEnumerable<SerializedKeyValuePair>>([]);

        public Task DeleteAsync(string key, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
