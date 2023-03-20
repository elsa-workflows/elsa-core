using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Serialization.Converters;

namespace Elsa.Workflows.Api.Endpoints.Events.Trigger;

public class Request
{
    public string EventName { get; set; } = default!;
    public string? CorrelationId { get; set; }
    
    [JsonConverter(typeof(ExpandoObjectConverter))]
    public object? Input { get; set; }
}

public class Response
{
}