using System.Text.Json.Serialization;

namespace Elsa.Jobs.Activities.Models;

public class BackgroundActivityPayload
{
    [JsonConstructor]
    public BackgroundActivityPayload()
    {
    }

    public BackgroundActivityPayload(string jobId)
    {
        JobId = jobId;
    }

    public string JobId { get; init; } = default!;
}