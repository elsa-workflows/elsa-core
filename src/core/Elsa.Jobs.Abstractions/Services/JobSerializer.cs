using System.Text.Json;
using Elsa.Jobs.Contracts;

namespace Elsa.Jobs.Services;

public class JobSerializer : IJobSerializer
{
    public string Serialize(IJob job)
    {
        var serializerOptions = CreateSerializerOptions();
        return JsonSerializer.Serialize(job, job.GetType(), serializerOptions);
    }

    public T Deserialize<T>(string json) where T : IJob
    {
        var serializerOptions = CreateSerializerOptions();
        return JsonSerializer.Deserialize<T>(json, serializerOptions)!;
    }

    private JsonSerializerOptions CreateSerializerOptions() => new();
}