using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Workflows.Sinks.Models;
using Elsa.Workflows.Sinks.Services;

namespace Elsa.Samples.WorkflowSinks.Sinks;

/// <summary>
/// A sample sink that extracts customer information from a running workflow.
/// </summary>
public class CustomerDetailsSink : IWorkflowSink
{
    public Task HandleWorkflowAsync(WorkflowSinkContext context, CancellationToken cancellationToken = default)
    {
        var workflow = context.Workflow;

        if (workflow.WorkflowMetadata.Name != "SubmitOrder")
            return Task.CompletedTask;

        var workflowState = context.WorkflowState;
        var customerName = "TODO";
        
        Console.WriteLine("Customer name: {0}", customerName);
        
        return Task.CompletedTask;
    }
}