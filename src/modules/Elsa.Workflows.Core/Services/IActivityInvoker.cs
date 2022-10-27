using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Services;

public interface IActivityInvoker
{
    Task InvokeAsync(
        WorkflowExecutionContext workflowExecutionContext,
        IActivity activity,
        ActivityExecutionContext? owner = default,
        IEnumerable<MemoryBlockReference>? memoryReferences = default);

    Task InvokeAsync(ActivityExecutionContext activityExecutionContext);
}