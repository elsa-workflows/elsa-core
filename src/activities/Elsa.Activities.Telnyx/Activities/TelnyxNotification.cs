using System.ComponentModel;
using Elsa.Attributes;
using Elsa.Services;

namespace Elsa.Activities.Telnyx.Activities
{
    [Trigger(Category = "Telnyx", Outcomes = new[] { OutcomeNames.Done })]
    [Browsable(false)]
    public class TelnyxNotification : Activity
    {
        public string EventType { get; set; } = default!;
    }
}