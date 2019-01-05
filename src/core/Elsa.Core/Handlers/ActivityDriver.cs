using System;
using System.Collections.Generic;
using Elsa.Results;

namespace Elsa.Handlers
{
    public abstract class ActivityDriver<T> : ActivityDriverBase<T> where T : IActivity
    {
        protected HaltResult Halt() => new HaltResult();
        protected TriggerEndpointsResult TriggerEndpoints(IEnumerable<string> names) => new TriggerEndpointsResult(names);
        protected TriggerEndpointsResult TriggerEndpoint(string name) => TriggerEndpoints(new[] { name });
        protected ScheduleActivityResult ScheduleActivity(IActivity activity) => new ScheduleActivityResult(activity);
        protected ReturnValueResult SetReturnValue(object value) => new ReturnValueResult(value);
        protected FinishWorkflowResult Finish() => new FinishWorkflowResult();
        protected FaultWorkflowResult Fault(string errorMessage) => new FaultWorkflowResult(errorMessage);
        protected FaultWorkflowResult Fault(Exception exception) => new FaultWorkflowResult(exception);
    }
}