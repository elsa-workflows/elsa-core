using System;
using Elsa.Services.Models;

namespace Elsa.ActivityResults
{
    [Obsolete("Use activity output properties to return output.")]
    public class OutputResult : ActivityExecutionResult
    {
        public OutputResult(object? output) => Output = output;
        public object? Output { get; }
        protected override void Execute(ActivityExecutionContext activityExecutionContext) => activityExecutionContext.Output = Output;
    }
}