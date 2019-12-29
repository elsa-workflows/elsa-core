using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Results
{
    public class FaultWorkflowResult : ActivityExecutionResult
    {
        private readonly string errorMessage;

        public FaultWorkflowResult(Exception exception) : this(exception.Message)
        {
        }

        public FaultWorkflowResult(string errorMessage)
        {
            this.errorMessage = errorMessage;
        }

        protected override void Execute(IProcessRunner runner, ProcessExecutionContext processContext)
        {
            var currentActivity = processContext.ScheduledActivity.Activity;

            processContext.Fault(currentActivity, errorMessage);
        }
    }
}