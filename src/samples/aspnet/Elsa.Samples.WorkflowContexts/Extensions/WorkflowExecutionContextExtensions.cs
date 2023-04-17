using Elsa.Workflows.Core.Models;

namespace Elsa.Samples.WorkflowContexts.Extensions;

public static class WorkflowExecutionContextExtensions
{
    private const string CustomerIdKey = "CustomerId";
    public static string? GetCustomerId(this WorkflowExecutionContext context) => context.GetProperty<string>(CustomerIdKey);
    public static void SetCustomerId(this WorkflowExecutionContext context, string? customerId) => context.SetProperty(CustomerIdKey, customerId);
}