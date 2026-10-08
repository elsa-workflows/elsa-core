using System.ComponentModel;
using System.Runtime.CompilerServices;
using Elsa.Alterations.Bookmarks;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;

namespace Elsa.Alterations.Activities;

/// <summary>
/// Submits an alteration plan for execution.
/// </summary>
[Browsable(false)]
[Activity("Elsa", "Alterations", "Triggered when an Alteration Plan completed")]
public class AlterationPlanCompleted : Trigger
{
    /// <inheritdoc />
    public AlterationPlanCompleted(Variable<string> planId, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
        PlanId = new Input<string>(planId);
    }

    /// <inheritdoc />
    public AlterationPlanCompleted([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
    }

    /// <summary>
    /// The ID of the alteration plan.
    /// </summary>
    public Input<string> PlanId { get; set; } = null!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        if (context.IsTriggerOfWorkflow())
        {
            await context.CompleteActivityAsync();
            return;
        }
        
        var planId = context.Get(PlanId)!;
        var bookmarkPayload = new AlterationPlanCompletedPayload(planId);
        context.CreateBookmark(bookmarkPayload, false);
    }

    /// <inheritdoc />
    protected override object GetTriggerPayload(TriggerIndexingContext context)
    {
        var planId = context.Get(PlanId)!;
        return new AlterationPlanCompletedPayload(planId);
    }
}