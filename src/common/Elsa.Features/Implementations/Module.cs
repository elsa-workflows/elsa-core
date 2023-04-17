using System.Reflection;
using Elsa.Extensions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Elsa.Features.Implementations;

/// <inheritdoc />
public class Module : IModule
{
    private record HostedServiceDescriptor(int Order, Type HostedServiceType);

    private IDictionary<Type, IFeature> _features = new Dictionary<Type, IFeature>();
    private readonly ISet<IFeature> _configuredFeatures = new HashSet<IFeature>();
    private readonly ICollection<HostedServiceDescriptor> _hostedServiceDescriptors = new List<HostedServiceDescriptor>();

    /// <summary>
    /// Constructor.
    /// </summary>
    public Module(IServiceCollection services)
    {
        Services = services;
    }

    /// <inheritdoc />
    public IServiceCollection Services { get; }

    /// <inheritdoc />
    public IDictionary<object, object> Properties { get; } = new Dictionary<object, object>();

    /// <inheritdoc />
    public T Configure<T>(Action<T>? configure = default) where T : class, IFeature
        => Configure(module => (T)Activator.CreateInstance(typeof(T), module)!, configure);

    /// <inheritdoc />
    public T Configure<T>(Func<IModule, T> factory, Action<T>? configure = default) where T : class, IFeature
    {
        if (!_features.TryGetValue(typeof(T), out var feature))
        {
            feature = factory(this);
            _features[typeof(T)] = feature;
        }

        configure?.Invoke((T)feature);

        if (!_isApplying)
            return (T)feature;

        var dependencies = GetDependencyTypes(feature.GetType()).ToHashSet();
        foreach (var dependency in dependencies.Select(GetOrCreateFeature))
            ConfigureFeature(dependency);

        ConfigureFeature(feature);
        return (T)feature;
    }

    /// <inheritdoc />
    public IModule ConfigureHostedService<T>(int priority = 0) where T : class, IHostedService
    {
        _hostedServiceDescriptors.Add(new HostedServiceDescriptor(priority, typeof(T)));
        return this;
    }

    private bool _isApplying;

    /// <inheritdoc />
    public void Apply()
    {
        _isApplying = true;
        var featureTypes = GetFeatureTypes();
        _features = featureTypes.ToDictionary(featureType => featureType, featureType => _features.TryGetValue(featureType, out var existingFeature) ? existingFeature : (IFeature)Activator.CreateInstance(featureType, this)!);

        // Iterate over a copy of the features to avoid concurrent modification exceptions.
        foreach (var feature in _features.Values.ToList())
        {
            // This will cause additional features to be added to _features.
            ConfigureFeature(feature);
        }

        foreach (var hostedServiceDescriptor in _hostedServiceDescriptors.OrderBy(x => x.Order))
            Services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IHostedService), hostedServiceDescriptor.HostedServiceType));

        // Make sure to use the complete list of features when applying them.
        foreach (var feature in _features.Values)
            feature.Apply();
    }

    private void ConfigureFeature(IFeature feature)
    {
        if (_configuredFeatures.Contains(feature))
            return;

        feature.Configure();
        feature.ConfigureHostedServices();
        _features[feature.GetType()] = feature;
        _configuredFeatures.Add(feature);
    }

    private IFeature GetOrCreateFeature(Type featureType)
    {
        return _features.TryGetValue(featureType, out var existingFeature) ? existingFeature : (IFeature)Activator.CreateInstance(featureType, this)!;
    }

    private ISet<Type> GetFeatureTypes()
    {
        var featureTypes = _features.Keys.ToHashSet();
        var featureTypesWithDependencies = featureTypes.Concat(featureTypes.SelectMany(GetDependencyTypes)).ToHashSet();
        return featureTypesWithDependencies.TSort(x => x.GetCustomAttributes<DependsOn>().Select(dependsOn => dependsOn.Type)).ToHashSet();
    }

    // Recursively get dependency types.
    private IEnumerable<Type> GetDependencyTypes(Type type)
    {
        var dependencies = type.GetCustomAttributes<DependsOn>().Select(dependsOn => dependsOn.Type).ToList();
        return dependencies.Concat(dependencies.SelectMany(GetDependencyTypes));
    }
}