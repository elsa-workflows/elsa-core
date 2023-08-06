using Elsa.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Samples.ConsoleApp.WorkflowFunctions.Workflows;

/// <summary>
/// A simple workflow that takes two numbers as runtime workflow input and adds them together and returns the result as the workflow's result.
/// </summary>
public class AddInputsWorkflow : WorkflowBase<float>
{
    protected override void Build(IWorkflowBuilder builder)
    {
        // Define the sum logic.
        builder.Root = new SetVariable
        {
            Variable = Result,
            Value = new (context => context.GetInput<float>("a") + context.GetInput<float>("b"))
        };
    }
}