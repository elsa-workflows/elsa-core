namespace Elsa.Services;

public interface IBookmarkDataSerializer
{
    T Deserialize<T>(string json) where T : notnull;
    string Serialize<T>(T payload) where T : notnull;
}