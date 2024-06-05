namespace Elsa.Shells;

public interface IShellFeatureTypesProvider
{
    IEnumerable<Type> GetFeatureTypes();
}