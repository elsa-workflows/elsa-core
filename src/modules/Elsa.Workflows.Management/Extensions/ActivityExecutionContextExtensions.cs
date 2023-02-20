using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Activities;

namespace Elsa.Workflows.Management.Extensions;

/// <summary>
/// Extends the <see cref="ActivityExecutionContext"/>.
/// </summary>
public static class ActivityExecutionContextExtensions
{
    /// <summary>
    /// Returns the first <see cref="WorkflowDefinitionActivity"/> in scope of the specified <see cref="ActivityExecutionContext"/>.
    /// </summary>
    public static WorkflowDefinitionActivity? GetFirstWorkflowDefinitionActivity(this ActivityExecutionContext context)
    {
        var workflowDefinitionActivityContext = context.GetFirstWorkflowDefinitionActivityExecutionContext();
        return (WorkflowDefinitionActivity?)workflowDefinitionActivityContext?.Activity;
    }
    
    /// <summary>
    /// Returns the first <see cref="WorkflowDefinitionActivity"/> in scope of the specified <see cref="ActivityExecutionContext"/>.
    /// </summary>
    public static ActivityExecutionContext? GetFirstWorkflowDefinitionActivityExecutionContext(this ActivityExecutionContext context)
    {
        var currentContext = context;
        
        // Get the closest ancestor that is of type WorkflowDefinitionActivity.
        while (currentContext != null)
        {
            if (currentContext.Activity is WorkflowDefinitionActivity workflowDefinitionActivity)
                return currentContext;

            currentContext = currentContext.ParentActivityExecutionContext;
        }

        return null;
    }
}