using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Implementations;

public class ActivityInvoker : IActivityInvoker
{
    private readonly IActivityExecutionPipeline _pipeline;

    public ActivityInvoker(IActivityExecutionPipeline pipeline)
    {
        _pipeline = pipeline;
    }

    public async Task InvokeAsync(
        WorkflowExecutionContext workflowExecutionContext,
        IActivity activity,
        ActivityExecutionContext? owner,
        IEnumerable<MemoryBlockReference>? memoryReferences = default)
    {
        // Setup an activity execution context.
        var activityExecutionContext = workflowExecutionContext.CreateActivityExecutionContext(activity, owner);

        // Declare memory.
        if (memoryReferences != null)
        {
            var workflowMemory = workflowExecutionContext.MemoryRegister;
            var activityMemory = new MemoryRegister(workflowMemory);
            activityMemory.Declare(memoryReferences);
        }

        // Add the activity context to the workflow context.
        workflowExecutionContext.ActivityExecutionContexts.Add(activityExecutionContext);

        // Execute the activity execution pipeline.
        await InvokeAsync(activityExecutionContext);
    }

    public async Task InvokeAsync(ActivityExecutionContext activityExecutionContext)
    {
        // Execute the activity execution pipeline.
        await _pipeline.ExecuteAsync(activityExecutionContext);
    }
}