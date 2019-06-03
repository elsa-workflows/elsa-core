using System;
using System.Collections.Generic;
using Elsa.Core.Results;
using NodaTime;

namespace Elsa.Core.Handlers
{
    public abstract class ActivityDriver<T> : ActivityDriverBase<T> where T : IActivity
    {
        protected HaltResult Halt() => new HaltResult();
        protected TriggerEndpointsResult Endpoints(IEnumerable<string> names) => new TriggerEndpointsResult(names);
        protected TriggerEndpointsResult Endpoint(string name) => Endpoints(new[] { name });
        protected ScheduleActivityResult ScheduleActivity(IActivity activity) => new ScheduleActivityResult(activity);
        protected ReturnValueResult SetReturnValue(object value) => new ReturnValueResult(value);
        protected FinishWorkflowResult Finish(Instant instant) => new FinishWorkflowResult(instant);
        protected FaultWorkflowResult Fault(string errorMessage) => new FaultWorkflowResult(errorMessage);
        protected FaultWorkflowResult Fault(Exception exception) => new FaultWorkflowResult(exception);
    }
}