namespace Elsa.Workflows;

public interface ICustomPropertiesProvider
{
    IDictionary<string, object> GetDefaultCustomProperties(Type type);
}