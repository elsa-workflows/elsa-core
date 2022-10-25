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
    private readonly IServiceProvider _serviceProvider;

    public ActivityInvoker(IActivityExecutionPipeline pipeline, IServiceProvider serviceProvider)
    {
        _pipeline = pipeline;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(
        WorkflowExecutionContext workflowExecutionContext,
        IActivity activity,
        ActivityExecutionContext? owner,
        IEnumerable<MemoryBlockReference>? memoryReferences = default)
    {
        // Setup an activity execution context.
        var workflowMemory = workflowExecutionContext.MemoryRegister;
        var activityMemory = new MemoryRegister(workflowMemory);
        var activityExecutionContext = workflowExecutionContext.CreateActivityExecutionContext(activity, owner);

        // Declare memory.
        if (memoryReferences != null)
            activityMemory.Declare(memoryReferences);

        // Push the activity context into the workflow context.
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