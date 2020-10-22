using Elsa.Services.Models;

namespace Elsa.ActivityResults
{
    public class OutputResult : ActivityExecutionResult
    {
        public OutputResult(object? output) => Output = output;
        public object? Output { get; }
        protected override void Execute(ActivityExecutionContext activityExecutionContext) => activityExecutionContext.Output = Output;
    }
}