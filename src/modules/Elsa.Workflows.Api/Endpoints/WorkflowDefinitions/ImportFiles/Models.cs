using System.Text.Json.Serialization;

namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.ImportFiles;

internal class Response(long importedCount)
{
    [JsonPropertyName("imported")] public long ImportedCount { get; } = importedCount;
}