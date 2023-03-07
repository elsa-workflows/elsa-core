using Elsa.Dsl.Contracts;
using Elsa.Workflows.Core.Models;

namespace Elsa.Dsl.Services;

public class TypeSystem : ITypeSystem
{
    private readonly IDictionary<string, TypeDescriptor> _typeNameLookup = new Dictionary<string, TypeDescriptor>();
    private readonly IDictionary<Type, TypeDescriptor> _typeLookup = new Dictionary<Type, TypeDescriptor>();
        
    public void Register(TypeDescriptor descriptor)
    {
        _typeNameLookup[descriptor.Name] = descriptor;
        _typeLookup[descriptor.Type] = descriptor;
    }
    
    public TypeDescriptor? ResolveTypeName(string typeName) => _typeNameLookup.TryGetValue(typeName, out var descriptor) ? descriptor : default;
    public TypeDescriptor? ResolveType(Type type) => _typeLookup.TryGetValue(type, out var descriptor) ? descriptor : default;
}