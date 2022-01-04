using Elsa.Models;

namespace Elsa.Dsl.Abstractions;

public interface ITypeSystem
{
    void Register(TypeDescriptor descriptor);
    TypeDescriptor? ResolveTypeName(string typeName);
}