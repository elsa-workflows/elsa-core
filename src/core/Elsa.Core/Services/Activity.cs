using System;
using System.Collections.Generic;
using Elsa.Models;
using Elsa.Results;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public abstract class Activity : ActivityBase
    {
        protected HaltResult Halt(bool continueOnFirstPass = false) => new HaltResult(continueOnFirstPass);
        protected OutcomeResult Outcomes(IEnumerable<string> names) => new OutcomeResult(names);
        protected OutcomeResult Outcome(string name) => Outcomes(new[] { name });
        protected CombinedResult Outcome(string name, object output) => Combine(SetOutput(output), Outcome(name));
        protected CombinedResult Outcome(string name, Variable output) => Combine(SetOutput(output), Outcome(name));
        protected OutcomeResult Done() => Outcome(OutcomeNames.Done);
        protected CombinedResult Done(object output) => Combine(SetOutput(output), Done());
        protected OutputResult SetOutput(Variable value) => new OutputResult(value);
        protected OutputResult SetOutput(object value) => SetOutput(Variable.From(value));
        protected ScheduleActivityResult ScheduleActivity(IActivity activity) => new ScheduleActivityResult(activity);
        protected FinishWorkflowResult Finish() => new FinishWorkflowResult();
        protected FaultWorkflowResult Fault(string errorMessage) => new FaultWorkflowResult(errorMessage);
        protected FaultWorkflowResult Fault(Exception exception) => new FaultWorkflowResult(exception);
        protected CombinedResult Combine(params IActivityExecutionResult[] results) => new CombinedResult(results);
    }
}