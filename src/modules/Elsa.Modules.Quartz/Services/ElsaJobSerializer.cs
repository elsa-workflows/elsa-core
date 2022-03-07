using System.Text.Json;
using Elsa.Jobs.Contracts;
using Elsa.Modules.Quartz.Contracts;

namespace Elsa.Modules.Quartz.Services;

public class ElsaJobSerializer : IElsaJobSerializer
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