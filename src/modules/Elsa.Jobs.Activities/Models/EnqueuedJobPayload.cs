using System.Text.Json.Serialization;

namespace Elsa.Jobs.Activities.Models;

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