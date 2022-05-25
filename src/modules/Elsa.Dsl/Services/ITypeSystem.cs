using Elsa.Workflows.Core.Models;

namespace Elsa.Dsl.Services;

public interface ITypeSystem
{
    void Register(TypeDescriptor descriptor);
    TypeDescriptor? ResolveTypeName(string typeName);
}