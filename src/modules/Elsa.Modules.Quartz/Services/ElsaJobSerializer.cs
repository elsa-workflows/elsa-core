using System.Text.Json;
using Elsa.Modules.Quartz.Contracts;
using IElsaJob = Elsa.Scheduling.Contracts.IJob;

namespace Elsa.Modules.Quartz.Services;

public class ElsaJobSerializer : IElsaJobSerializer
{
    public string Serialize(IElsaJob job)
    {
        var serializerOptions = CreateSerializerOptions();
        return JsonSerializer.Serialize(job, job.GetType(), serializerOptions);
    }

    public T Deserialize<T>(string json) where T : IElsaJob
    {
        var serializerOptions = CreateSerializerOptions();
        return JsonSerializer.Deserialize<T>(json, serializerOptions)!;
    }

    private JsonSerializerOptions CreateSerializerOptions() => new();
}