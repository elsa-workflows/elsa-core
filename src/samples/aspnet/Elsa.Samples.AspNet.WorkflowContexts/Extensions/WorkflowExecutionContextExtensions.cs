using Elsa.Extensions;
using Elsa.Samples.AspNet.WorkflowContexts.Providers;
using Elsa.Workflows.Core;

namespace Elsa.Samples.AspNet.WorkflowContexts.Extensions;

public static class WorkflowExecutionContextExtensions
{
    public static string? GetCustomerId(this WorkflowExecutionContext context) => context.GetWorkflowContextParameter<CustomerWorkflowContextProvider, string>();
    public static void SetCustomerId(this WorkflowExecutionContext context, string? customerId) => context.SetWorkflowContextParameter<CustomerWorkflowContextProvider>(customerId);
}