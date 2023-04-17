using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Samples.WorkflowContexts.Contracts;
using Elsa.Samples.WorkflowContexts.Providers;

namespace Elsa.Samples.WorkflowContexts.Extensions;

public static class ExpressionExecutionContextExtensions
{
    public static Customer GetCustomer(this ExpressionExecutionContext context) => context.GetWorkflowContext<CustomerWorkflowContextProvider, Customer>();
}