namespace Elsa.Workflows.Core.Contracts;

public interface IBookmarkPayloadSerializer
{
    T Deserialize<T>(string json) where T : notnull;
    object Deserialize(string json, Type type);
    string Serialize<T>(T payload) where T : notnull;
}