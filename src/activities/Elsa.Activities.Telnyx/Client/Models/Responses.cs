namespace Elsa.Activities.Telnyx.Client.Models
{
    public record DialResponse(string CallControlId, string CallLegId, string CallSessionId, bool IsAlive, string RecordType);
}