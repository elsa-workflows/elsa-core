namespace Elsa.Activities.Telnyx.Webhooks.Models
{
    public abstract class TelnyxRecord
    {
        public string RecordType { get; set; } = default!;
    }
}