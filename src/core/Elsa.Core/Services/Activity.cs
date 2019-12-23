using System;
using System.Collections.Generic;
using Elsa.Models;
using Elsa.Results;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public abstract class Activity : ActivityBase
    {
        protected SuspendWorkflowResult Halt(bool continueOnFirstPass = false) => new SuspendWorkflowResult(continueOnFirstPass);
        protected OutcomeResult Outcomes(IEnumerable<string> names) => new OutcomeResult(names);
        protected OutcomeResult Outcome(string name) => Outcomes(new[] { name });
        protected OutcomeResult Outcome(string name, object output) => Outcome(name, Variable.From(output));
        protected OutcomeResult Done() => Outcome(OutcomeNames.Done);
        protected OutcomeResult Done(object output) => Outcome(OutcomeNames.Done, output);
        protected OutcomeResult Outcome(string name, Variable output)
        {
            Output = output;
            return Outcome(name);
        }
        
        protected ScheduleActivityResult ScheduleActivity(IActivity activity, Variable input = null) => new ScheduleActivityResult(activity, input);
        protected CompleteWorkflowResult Finish() => new CompleteWorkflowResult();
        protected FaultWorkflowResult Fault(string errorMessage) => new FaultWorkflowResult(errorMessage);
        protected FaultWorkflowResult Fault(Exception exception) => new FaultWorkflowResult(exception);
        protected CombinedResult Combine(params IActivityExecutionResult[] results) => new CombinedResult(results);
    }
}