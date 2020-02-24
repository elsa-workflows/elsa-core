namespace Elsa.Serialization
{
    public interface IWorkflowSerializer
    {
        string Serialize<T>(T workflow, string format);
        T Deserialize<T>(string data, string format);
    }
}