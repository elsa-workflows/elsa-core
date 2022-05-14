using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Primitives;

[Activity(Category = "Primitives", Description = "Put the workflow in a faulted state.", Outcomes = new string[0])]
public class Fault : Activity
{
    /// <summary>
    /// Optional. The message to store as the reason for faulting.
    /// </summary>
    [ActivityInput(Hint = "Optional. The message to store as the reason for the fault.", SupportedSyntaxes = new[] { SyntaxNames.Liquid, SyntaxNames.JavaScript })]
    public string? Message { get; set; }

    protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
    {
        var message = Message ?? "Custom fault";
        context.JournalData.Add("Error", message);
        return new FaultResult(message);
    }
}