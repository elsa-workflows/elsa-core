using System.Reflection;

namespace Elsa.Management.Services;

public interface IActivityPropertyOptionsProvider
{
    object? GetOptions(PropertyInfo property);
}