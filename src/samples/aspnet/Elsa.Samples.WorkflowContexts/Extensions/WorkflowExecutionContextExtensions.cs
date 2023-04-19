using Elsa.Extensions;
using Elsa.Samples.WorkflowContexts.Providers;
using Elsa.Workflows.Core.Models;

namespace Elsa.Samples.WorkflowContexts.Extensions;

public static class WorkflowExecutionContextExtensions
{
    public static string? GetCustomerId(this WorkflowExecutionContext context) => context.GetWorkflowContextParameter<CustomerWorkflowContextProvider, string>();
    public static void SetCustomerId(this WorkflowExecutionContext context, string? customerId) => context.SetWorkflowContextParameter<CustomerWorkflowContextProvider>(customerId);
}