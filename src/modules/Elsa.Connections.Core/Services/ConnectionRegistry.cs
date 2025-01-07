using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Elsa.Connections.Attributes;
using Elsa.Connections.Contracts;
using Elsa.Connections.Features;
using Elsa.Connections.Models;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Options;

namespace Elsa.Connections.Services;
public class ConnectionRegistry : IConnectionDescriptorRegistry
{

    private readonly ConcurrentDictionary<Type, ConnectionDescriptor> _connectionDescriptors = new();
    private readonly ConnectionOptions _options;
    private readonly IActivityDescriber _describer;

    public ConnectionRegistry(IOptions<ConnectionOptions> options, IActivityDescriber describer)
    {
        _options = options.Value;
        _describer = describer;
    }
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

    public Type Get(string type)
    {
        var item = _options.ConnectionTypes.Where(c => c.ToString() == type).FirstOrDefault();
        return item;
    }

    public async Task<IEnumerable<InputDescriptor>> GetConnectionDescriptor(string activityType, CancellationToken cancellationToken = default)
    {
        var propertyType2 = Type.GetType(activityType);

        var propertyType = Get(activityType);

        if (propertyType == null)
            return null;

        var connectionInputProperty = _describer.GetInputProperties(propertyType);
        var connectionInputDescriptor = await DescribeInputPropertiesAsync(connectionInputProperty);

        return connectionInputDescriptor;
    }

    private async Task<IEnumerable<InputDescriptor>> DescribeInputPropertiesAsync(IEnumerable<PropertyInfo> properties, CancellationToken cancellationToken = default)
    {
        return await Task.WhenAll(properties.Select(async x => await _describer.DescribeInputPropertyAsync(x, cancellationToken)));
    }
    private ConnectionDescriptor DescribeConnection(Type connectionType)
    {
        var connectionAttribute = connectionType.GetCustomAttribute<ConnectionPropertyAttribute>();

        if (connectionAttribute == null)
            throw new ArgumentNullException($"{connectionType} is not a valid connection, make sure [ConnectionPropertyAttribute] is set ");

        return new ConnectionDescriptor(
            connectionType.ToString(),
            connectionAttribute?.Description,
            connectionAttribute.Namespace, 
            connectionAttribute.DisplayName
            );
    }
}
