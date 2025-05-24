using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Activities.SetOutput;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Expressions.Models;

namespace Elsa.Workflows.IntegrationTests.Scenarios.LiquidSubWorkflow;

/// <summary>
/// A sub-workflow that reads input using liquid expressions
/// </summary>
public class LiquidSubWorkflow : WorkflowBase
{
    public Input<string> PersonName { get; set; } = default!;
    public Input<string> PersonEmail { get; set; } = default!;
    public Output<string> Result { get; set; } = default!;

    protected override void Build(IWorkflowBuilder builder)
    {
        var resultVariable = new Variable<string>("Result");
        
        builder.Root = new Sequence
        {
            Variables = { resultVariable },
            Activities =
            {
                // Use liquid to read inputs
                new WriteLine(new Expression("Liquid", "Person: {{ Input.PersonName }}, Email: {{ Input.PersonEmail }}")),

                // Set the result with liquid expression
                new SetVariable
                {
                    Variable = resultVariable,
                    Value = new Expression("Liquid", "{{ Input.PersonName }} - {{ Input.PersonEmail }}")
                },
                
                // Set output
                new SetOutput
                {
                    OutputName = new("Result"),
                    OutputValue = new(resultVariable) 
                }
            }
        };
    }
}

/// <summary>
/// A main workflow that uses the sub-workflow as an activity
/// </summary>
public class LiquidParentWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var resultVariable = new Variable<string>("Result");
        
        var subWorkflow = new LiquidSubWorkflow
        {
            PersonName = new("John Doe"),
            PersonEmail = new("john@example.com"),
            Result = new(resultVariable)
        };

        builder.Variables.Add(resultVariable);

        builder.Root = new Sequence
        {
            Activities =
            {
                subWorkflow,
                new WriteLine(context => $"Sub workflow result: {resultVariable.Get(context)}")
            }
        };
    }
}