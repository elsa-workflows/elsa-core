using System.ComponentModel;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Telnyx.Activities
{
    [Trigger(Category = "Telnyx", Outcomes = new[] { OutcomeNames.Done })]
    [Browsable(false)]
    public class TelnyxNotification : Activity
    {
        public string EventType { get; set; } = default!;

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => context.WorkflowExecutionContext.IsFirstPass ? ExecuteInternal() : Suspend();

        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context) => ExecuteInternal();
        
        private IActivityExecutionResult ExecuteInternal()
        {
            return Done();
        }
    }
}