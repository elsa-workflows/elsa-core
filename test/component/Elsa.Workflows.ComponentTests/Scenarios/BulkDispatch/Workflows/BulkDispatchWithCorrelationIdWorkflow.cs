using Elsa.Expressions.JavaScript.Models;
using Elsa.Extensions;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Workflows.ComponentTests.Scenarios.BulkDispatch.Workflows;

public class BulkDispatchWithCorrelationIdWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();

    protected override void Build(IWorkflowBuilder builder)
    {
        builder.WithDefinitionId(DefinitionId);

        var items = new object[] { 1, 2, 3 };

        builder.Root = new BulkDispatchWorkflows
        {
            WorkflowDefinitionId = new(BulkChildWorkflow.DefinitionId),
            Items = new(items),
            CorrelationIdFunction = new(JavaScriptExpression.Create("`correlation-${getItem()}`")),
            WaitForCompletion = new(true)
        };
    }
}
