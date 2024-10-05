using System.ComponentModel;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Enums;
using Elsa.Alterations.Core.Models;
using Elsa.Common;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Models;

namespace Elsa.Alterations.Activities;

/// <summary>
/// Submits an alteration plan for execution.
/// </summary>
[Browsable(false)]
[Output(
    DisplayName = "Plan ID",
    Description = "The ID of the submitted alteration plan."
)]
[Activity("Elsa", "Alterations", "Submits an Alteration Plan", Kind = ActivityKind.Task)]
public class SubmitAlterationPlan : CodeActivity<string>
{
    /// <summary>
    /// The parameters for the alteration plan to be submitted.
    /// </summary>
    public Input<AlterationPlanParams> Params { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var systemClock = context.GetRequiredService<ISystemClock>();
        var identityGenerator = context.GetRequiredService<IIdentityGenerator>();
        var now = systemClock.UtcNow;
        var planParams = context.Get(Params)!;

        var plan = new AlterationPlan
        {
            Id = string.IsNullOrWhiteSpace(planParams.Id) ? identityGenerator.GenerateId() : planParams.Id,
            Alterations = planParams.Alterations,
            WorkflowInstanceFilter = planParams.Filter,
            Status = AlterationPlanStatus.Pending,
            CreatedAt = now
        };

        var alterationPlanStore = context.GetRequiredService<IAlterationPlanStore>();
        var cancellationToken = context.CancellationToken;
        await alterationPlanStore.SaveAsync(plan, cancellationToken);
        Result.Set(context, plan.Id);
    }
}