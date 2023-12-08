using System.Reflection;

namespace Elsa.Workflows.Core.Contracts;

public interface IPropertyUIHandler
{
    string Name { get; }
    ValueTask<object?> GetUIProperties(PropertyInfo propertyInfo, object context, CancellationToken cancellatonToken = default);
}

public interface IPropertyUIHandlerResolver
{
    ValueTask<IDictionary<string,object>?> GetUIProperties(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken = default);
}