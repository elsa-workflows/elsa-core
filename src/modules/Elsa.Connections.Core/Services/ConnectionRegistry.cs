using System.Collections.Concurrent;
using System.Reflection;
using Elsa.Connections.Attributes;
using Elsa.Connections.Contracts;
using Elsa.Connections.Models;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Options;

namespace Elsa.Connections.Services;

public class ConnectionRegistry(IOptions<ConnectionOptions> options, IActivityDescriber describer) : IConnectionDescriptorRegistry
{
    private readonly ConcurrentDictionary<Type, ConnectionDescriptor> _connectionDescriptors = new();
    private readonly ConnectionOptions _options = options.Value;

    public void Add(Type connectionType, ConnectionDescriptor connectionDescriptor)
    {
        var descriptor = DescribeConnection(connectionType);

        _connectionDescriptors.TryAdd(connectionType, descriptor);
    }

    public IEnumerable<ConnectionDescriptor> ListAll()
    {
        foreach (var connectionType in _options.ConnectionTypes)
            yield return DescribeConnection(connectionType);
    }

    public void Remove(Type connectionType, ActivityDescriptor descriptor)
    {
        throw new NotImplementedException();
    }

    public Type? Get(string type)
    {
        return _options.ConnectionTypes.FirstOrDefault(c => c.ToString() == type);
    }

    public async Task<IEnumerable<InputDescriptor>> GetConnectionDescriptorAsync(string activityType, CancellationToken cancellationToken = default)
    {
        var propertyType = Get(activityType);

        if (propertyType == null)
            return [];

        var connectionInputProperty = describer.GetInputProperties(propertyType);
        var connectionInputDescriptor = await DescribeInputPropertiesAsync(connectionInputProperty, cancellationToken);

        return connectionInputDescriptor;
    }

    private async Task<IEnumerable<InputDescriptor>> DescribeInputPropertiesAsync(IEnumerable<PropertyInfo> properties, CancellationToken cancellationToken = default)
    {
        return await Task.WhenAll(properties.Select(async x => await describer.DescribeInputPropertyAsync(x, cancellationToken)));
    }

    private ConnectionDescriptor DescribeConnection(Type connectionType)
    {
        var connectionAttribute = connectionType.GetCustomAttribute<ConnectionPropertyAttribute>();

        if (connectionAttribute == null)
            throw new ArgumentNullException($"{connectionType} is not a valid connection, make sure [ConnectionPropertyAttribute] is set ");

        return new(
            connectionType.ToString(),
            connectionAttribute.Description,
            connectionAttribute.Namespace,
            connectionAttribute.DisplayName
        );
    }
}