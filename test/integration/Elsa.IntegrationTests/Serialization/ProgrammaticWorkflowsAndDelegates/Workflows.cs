using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;

namespace Elsa.IntegrationTests.Serialization.ProgrammaticWorkflowsAndDelegates;

/// <inheritdoc />
public class GreeterWorkflow : WorkflowBase
{
    /// <inheritdoc />
    protected override void Build(IWorkflowBuilder builder)
    {
        var messageInput = builder.WithInput<string>("Message", "The message to write to the console.");
        
        builder.Name = "Greeter Workflow";
        builder.Inputs.Add(messageInput);
        builder.Root = new WriteLine(context => context.GetInput<string>(messageInput));
    }
}