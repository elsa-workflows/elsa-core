using System.ComponentModel;
using System.Runtime.CompilerServices;
using Elsa.Alterations.Core.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Exceptions;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;

namespace Elsa.Alterations.Activities;

/// <summary>
/// Marks an alteration plan as completed.
/// </summary>
[Browsable(false)]
[Activity("Elsa", "Alterations", "Dispatches jobs for the specified Alteration Plan", Kind = ActivityKind.Task)]
public class CompleteAlterationPlan : CodeActivity
{
    /// <inheritdoc />
    public CompleteAlterationPlan(Variable<string> planId, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
        PlanId = new Input<string>(planId);
    }

    /// <inheritdoc />
    public CompleteAlterationPlan([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
    }

    /// <summary>
    /// The ID of the alteration plan.
    /// </summary>
    public Input<string> PlanId { get; set; } = null!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var planId = context.Get(PlanId)!;
        var manager = context.GetRequiredService<IAlterationPlanManager>();
        var plan = await manager.GetPlanAsync(planId, cancellationToken);

        if (plan == null)
            throw new FaultException(AlterationFaultCodes.PlanNotFound, AlterationFaultCategories.Alteration, DefaultFaultTypes.System, $"Alteration Plan with ID {planId} not found.");

        await manager.CompletePlanAsync(plan, cancellationToken);
    }
}