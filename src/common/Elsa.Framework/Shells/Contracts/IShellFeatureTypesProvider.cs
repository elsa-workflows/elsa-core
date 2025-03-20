namespace Elsa.Framework.Shells;

public interface IShellFeatureTypesProvider
{
    IEnumerable<Type> GetFeatureTypes();
}