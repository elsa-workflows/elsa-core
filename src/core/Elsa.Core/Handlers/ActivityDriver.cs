using System;
using System.Collections.Generic;
using Elsa.Results;
using NodaTime;

namespace Elsa.Handlers
{
    public abstract class ActivityDriver<T> : ActivityDriverBase<T> where T : IActivity
    {
        protected HaltResult Halt(Instant instant) => new HaltResult(instant);
        protected TriggerEndpointsResult TriggerEndpoints(IEnumerable<string> names) => new TriggerEndpointsResult(names);
        protected TriggerEndpointsResult TriggerEndpoint(string name) => TriggerEndpoints(new[] { name });
        protected ScheduleActivityResult ScheduleActivity(IActivity activity) => new ScheduleActivityResult(activity);
        protected ReturnValueResult SetReturnValue(object value) => new ReturnValueResult(value);
        protected FinishWorkflowResult Finish(Instant instant) => new FinishWorkflowResult(instant);
        protected FaultWorkflowResult Fault(string errorMessage, Instant instant) => new FaultWorkflowResult(errorMessage, instant);
        protected FaultWorkflowResult Fault(Exception exception, Instant instant) => new FaultWorkflowResult(exception, instant);
    }
}