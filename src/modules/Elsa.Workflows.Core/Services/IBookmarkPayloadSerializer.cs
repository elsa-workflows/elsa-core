namespace Elsa.Workflows.Core.Services;

public interface IBookmarkPayloadSerializer
{
    T Deserialize<T>(string json) where T : notnull;
    string Serialize<T>(T payload) where T : notnull;
}