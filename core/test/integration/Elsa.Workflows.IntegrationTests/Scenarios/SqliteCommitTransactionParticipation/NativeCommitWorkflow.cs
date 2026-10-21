using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Workflows.IntegrationTests.Scenarios.SqliteCommitTransactionParticipation;

internal sealed class NativeCommitWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var value = builder.WithVariable<string>("checkpoint", "created");

        builder.Root = new Sequence
        {
            Activities =
            {
                new SetVariable<string>(value, "created")
                {
                    Id = "SetInitialValue"
                },
                new Event("first") { Id = "FirstEvent" },
                new SetVariable<string>(value, "resumed")
                {
                    Id = "SetResumedValue"
                },
                new Event("second") { Id = "SecondEvent" }
            }
        };
    }
}
