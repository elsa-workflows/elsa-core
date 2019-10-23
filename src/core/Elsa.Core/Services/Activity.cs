using System;
using System.Collections.Generic;
using Elsa.Results;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public abstract class Activity : ActivityBase
    {
        protected HaltResult Halt(bool continueOnFirstPass = false) => new HaltResult(continueOnFirstPass);
        protected ActivityExecutionResult Outcomes(IEnumerable<string> names) => new OutcomeResult(names);
        protected ActivityExecutionResult Outcome(string name) => Outcomes(new[] { name });
        protected ActivityExecutionResult Done() => Outcome(OutcomeNames.Done);
        protected ScheduleActivityResult ScheduleActivity(IActivity activity) => new ScheduleActivityResult(activity);
        protected ReturnValueResult SetReturnValue(object value) => new ReturnValueResult(value);
        protected FinishWorkflowResult Finish() => new FinishWorkflowResult();
        protected FaultWorkflowResult Fault(string errorMessage) => new FaultWorkflowResult(errorMessage);
        protected FaultWorkflowResult Fault(Exception exception) => new FaultWorkflowResult(exception);
    }
}