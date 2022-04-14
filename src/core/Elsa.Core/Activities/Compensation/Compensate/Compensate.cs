using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Exceptions;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Compensation;

[Activity(Category = "Compensation", Description = "Invoke a specific compensable activity.", Outcomes = new[] { OutcomeNames.Done })]
public class Compensate : Activity
{
    /// <summary>
    /// The name of the <see cref="Compensable"/> activity to invoke.
    /// </summary>
    [ActivityInput(Hint = "The name of the compensable activity to invoke.", SupportedSyntaxes = new[]{ SyntaxNames.Liquid, SyntaxNames.JavaScript })]
    public string CompensableActivityName { get; set; } = default!;
    
    /// <summary>
    /// Optional. The message to store as the reason for compensation.
    /// </summary>
    [ActivityInput(Hint = "Optional. The message to store as the reason for compensation.", SupportedSyntaxes = new[]{ SyntaxNames.Liquid, SyntaxNames.JavaScript })]
    public string? Message { get; set; }

    protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
    {
        if (string.IsNullOrWhiteSpace(CompensableActivityName))
            throw new WorkflowException("No name specified");
        
        var message = Message ?? "Compensating";
        var compensableActivity = context.WorkflowExecutionContext.GetActivityBlueprintByName(CompensableActivityName);
        
        if(compensableActivity == null)
            throw new WorkflowException($"No activity with name {CompensableActivityName} could be found");

        return new CompensateResult(message, compensableActivityId: compensableActivity.Id);
    }
}