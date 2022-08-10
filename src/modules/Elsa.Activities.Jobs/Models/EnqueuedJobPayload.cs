using System.Text.Json.Serialization;

namespace Elsa.Activities.Jobs.Models;

public record EnqueuedJobPayload
{

    [JsonConstructor]
    public EnqueuedJobPayload()
    {
    }

    public EnqueuedJobPayload(string jobId)
    {
        JobId = jobId;
    }

    public string JobId { get; init; } = default!;
}