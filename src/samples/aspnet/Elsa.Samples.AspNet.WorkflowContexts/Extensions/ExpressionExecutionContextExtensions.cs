using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Samples.AspNet.WorkflowContexts.Entities;
using Elsa.Samples.AspNet.WorkflowContexts.Providers;

namespace Elsa.Samples.AspNet.WorkflowContexts.Extensions;

public static class ExpressionExecutionContextExtensions
{
    public static Customer GetCustomer(this ExpressionExecutionContext context) => context.GetWorkflowContext<CustomerWorkflowContextProvider, Customer>();
}