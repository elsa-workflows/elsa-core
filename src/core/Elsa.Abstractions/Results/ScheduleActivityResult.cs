using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Results
{
    public class ScheduleActivityResult : ActivityExecutionResult
    {
        public ScheduleActivityResult(IActivity activity, Variable input = null)
        {
            Activity = activity;
            Input = input;
        }
        
        public IActivity Activity { get; }
        public Variable Input { get; }
        
        protected override void Execute(IProcessRunner runner, ProcessExecutionContext processContext)
        {
            processContext.ScheduleActivity(Activity, Input);
        }
    }
}