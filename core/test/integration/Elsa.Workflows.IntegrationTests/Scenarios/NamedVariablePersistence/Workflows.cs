using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Workflows.IntegrationTests.Scenarios.NamedVariablePersistence;

class NamedVariableSurvivesSuspendWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var message = builder.WithVariable<string>("message", null!);

        builder.Root = new Sequence
        {
            Activities =
            {
                new SetVariable<string>(message, "hello"),
                new WriteLine(context => $"before suspend: {message.Get(context) ?? "<null>"}"),
                new Event("Resume") { Id = "Resume" },
                new WriteLine(context => $"after resume: {message.Get(context) ?? "<null>"}")
            }
        };
    }
}
