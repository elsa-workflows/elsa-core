using Elsa.Workflows.Models;
using Elsa.Workflows.Options;

namespace Elsa.Workflows;

public interface IWorkflowExecutionContextSchedulerStrategy
{
    ActivityWorkItem Schedule(WorkflowExecutionContext context,
        ActivityNode activityNode,
        ActivityExecutionContext owner,
        ScheduleWorkOptions? options = null);
}