using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Elsa.Activities.Timers.Services;
using Elsa.Services.Models;

using NodaTime;

namespace Elsa.Activities.Timers.Hangfire.Services
{
    public class HangfireWorkflowScheduler : IWorkflowScheduler
    {
        public Task ScheduleWorkflowAsync(IWorkflowBlueprint workflowBlueprint, string activityId, Instant startAt, Duration interval, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task ScheduleWorkflowAsync(IWorkflowBlueprint workflowBlueprint, string activityId, Instant startAt, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task ScheduleWorkflowAsync(IWorkflowBlueprint workflowBlueprint, string activityId, string cronExpression, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task ScheduleWorkflowAsync(IWorkflowBlueprint workflowBlueprint, string workflowInstanceId, string activityId, Instant startAt, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
