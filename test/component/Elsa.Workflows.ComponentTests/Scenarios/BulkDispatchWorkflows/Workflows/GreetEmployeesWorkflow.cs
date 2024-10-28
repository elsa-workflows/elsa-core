using Elsa.Workflows.Activities;

namespace Elsa.Workflows.ComponentTests.Scenarios.BulkDispatchWorkflows.Workflows;

public class GreetEmployeesWorkflow : WorkflowBase
{
    public static readonly string DefinitionId = Guid.NewGuid().ToString();

    protected override void Build(IWorkflowBuilder builder)
    {
        var employeeNames = new[]
        {
            "Alice", "Bob", "Charlie"
        };

        var inputEntries = employeeNames.Select(x => new Dictionary<string, object>
        {
            ["Employee"] = x
        });

        builder.WithDefinitionId(DefinitionId);
        builder.Root = new Sequence
        {
            Activities =
            {
                new Runtime.Activities.BulkDispatchWorkflows
                {
                    WorkflowDefinitionId = new(EmployeeGreetingWorkflow.DefinitionId),
                    Items = new(inputEntries),
                    WaitForCompletion = new(true)
                }
            }
        };
    }
}