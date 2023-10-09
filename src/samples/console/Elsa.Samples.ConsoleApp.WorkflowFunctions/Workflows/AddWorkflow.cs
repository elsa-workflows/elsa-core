using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Memory;

namespace Elsa.Samples.ConsoleApp.WorkflowFunctions.Workflows;

/// <summary>
/// A simple workflow that takes two numbers as build-time input and adds them together and returns the result as the workflow's result.
/// </summary>
public class AddWorkflow : WorkflowBase<float>
{
    private readonly Variable<float> _firstNumber;
    private readonly Variable<float> _secondNumber;

    public AddWorkflow(float firstNumber, float secondNumber)
    {
        _firstNumber = new Variable<float>(firstNumber);
        _secondNumber = new Variable<float>(secondNumber);
    }

    protected override void Build(IWorkflowBuilder builder)
    {
        // Attach workflow variables.
        builder.WithVariables(_firstNumber, _secondNumber);

        // Define the sum logic using the SetVariable activity that simply assigns the sum to the Result variable inherited from the base class.
        builder.Root = new SetVariable
        {
            Variable = Result,
            Value = new (context => _firstNumber.Get(context) + _secondNumber.Get(context))
        };
    }
}