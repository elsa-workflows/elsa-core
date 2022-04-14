using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Exceptions;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Compensation;

[Activity(Category = "Compensation", Description = "Confirm a specific compensable activity.", Outcomes = new[] { OutcomeNames.Done })]
public class Confirm : Activity
{
    /// <summary>
    /// The name of the <see cref="Compensable"/> activity to invoke.
    /// </summary>
    [ActivityInput(Hint = "The name of the compensable activity to confirm.", SupportedSyntaxes = new[]{ SyntaxNames.Liquid, SyntaxNames.JavaScript })]
    public string CompensableActivityName { get; set; } = default!;
    
    protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
    {
        if (string.IsNullOrWhiteSpace(CompensableActivityName))
            throw new WorkflowException("No name specified");
        
        var compensableActivity = context.WorkflowExecutionContext.GetActivityBlueprintByName(CompensableActivityName);
        
        if(compensableActivity == null)
            throw new WorkflowException($"No activity with name {CompensableActivityName} could be found");

        return new ConfirmResult(compensableActivity.Id);
    }
}