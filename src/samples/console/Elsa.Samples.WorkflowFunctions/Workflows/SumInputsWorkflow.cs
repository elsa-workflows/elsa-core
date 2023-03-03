using Elsa.Extensions;
using Elsa.Workflows.Core.Abstractions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Samples.WorkflowFunctions.Workflows;

/// <summary>
/// A workflow that takes a list of numbers as runtime workflow input and returns the sum.
/// </summary>
public class SumInputsWorkflow : WorkflowBase<float>
{
    protected override void Build(IWorkflowBuilder builder)
    {
        // Attach workflow variables.
        var currentSum = new Variable<float>();
        var currentValue = new Variable<float>();

        builder.WithVariable(currentSum);

        // Define the sum logic.
        builder.Root = new Sequence
        {
            Variables = { currentValue },
            Activities =
            {
                new ForEach<float>(new Input<ICollection<float>>(context => context.GetInput<float[]>("numbers")!))
                {
                    CurrentValue = new Output<float>(currentValue),
                    Body = new Sequence
                    {
                        Activities =
                        {
                            new WriteLine(context => $"Adding {currentValue.Get(context)}"),
                            new SetVariable
                            {
                                Variable = currentSum,
                                Value = new Input<object?>(context => currentSum.Get(context) + currentValue.Get(context))
                            }
                        }
                    }
                },
                new SetVariable
                {
                    Variable = Result,
                    Value = new Input<object?>(currentSum)
                }
            }
        };
    }
}