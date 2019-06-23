using System;
using System.Collections.Generic;
using Elsa.Core.Results;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using NodaTime;

namespace Elsa.Core.Services
{
    public abstract class Activity : ActivityBase
    {
        protected HaltResult Halt(bool continueOnFirstPass = false) => new HaltResult(continueOnFirstPass);
        protected ActivityExecutionResult Outcomes(IEnumerable<string> names) => new OutcomeResult(names);
        protected ActivityExecutionResult Outcome(string name) => Outcomes(new[] { name });
        protected ActivityExecutionResult Done() => Outcome(OutcomeNames.Done);
        protected ScheduleActivityResult ScheduleActivity(IActivity activity) => new ScheduleActivityResult(activity);
        protected ReturnValueResult SetReturnValue(object value) => new ReturnValueResult(value);
        protected FinishWorkflowResult Finish(Instant instant) => new FinishWorkflowResult(instant);
        protected FaultWorkflowResult Fault(string errorMessage) => new FaultWorkflowResult(errorMessage);
        protected FaultWorkflowResult Fault(Exception exception) => new FaultWorkflowResult(exception);
    }
}