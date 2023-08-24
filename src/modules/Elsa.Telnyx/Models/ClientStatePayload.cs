using System.Text;
using System.Text.Json;

namespace Elsa.Telnyx.Models;

/// <summary>
/// Represents the client state payload to correlate commands and events.
/// </summary>
/// <param name="WorkflowInstanceId">The correlation ID.</param>
/// <param name="ActivityInstanceId">An optional activity instance ID.</param>
public record ClientStatePayload(string WorkflowInstanceId, string? ActivityInstanceId = default)
{
    /// <summary>
    /// Deserializes a <see cref="ClientStatePayload"/> from the specified base64 string.
    /// </summary>
    public static ClientStatePayload FromBase64(string base64)
    {
        var bytes = Convert.FromBase64String(base64);
        var json = Encoding.UTF8.GetString(bytes);
        return JsonSerializer.Deserialize<ClientStatePayload>(json)!;
    }
        
    /// <summary>
    /// Serializes the <see cref="ClientStatePayload"/> to a base64 string.
    /// </summary>
    /// <returns></returns>
    public string ToBase64()
    {
        var json = JsonSerializer.Serialize(this);
        var bytes = Encoding.UTF8.GetBytes(json);
        return Convert.ToBase64String(bytes);
    }
}