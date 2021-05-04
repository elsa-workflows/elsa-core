using System;

namespace ElsaDashboard.Application.Models
{
    public class AddActivityInvokedEventArgs : EventArgs
    {
        public AddActivityInvokedEventArgs(string? sourceActivityId, string? targetActivityId, string? outcome)
        {
            SourceActivityId = sourceActivityId;
            TargetActivityId = targetActivityId;
            Outcome = outcome;
        }

        public string? SourceActivityId { get; }
        public string? TargetActivityId { get; }
        public string? Outcome { get; }
    }
}