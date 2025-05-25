using System.ComponentModel;
using System.Runtime.CompilerServices;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Enums;
using Elsa.Alterations.Core.Filters;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Exceptions;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;

namespace Elsa.Alterations.Activities;

/// <summary>
/// Submits an alteration plan for execution.
/// </summary>
[Browsable(false)]
[Activity("Elsa", "Alterations", "Dispatches jobs for the specified Alteration Plan", Kind = ActivityKind.Task)]
public class DispatchAlterationJobs : CodeActivity
{
    /// <inheritdoc />
    public DispatchAlterationJobs(Variable<string> planId, [CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
        PlanId = new Input<string>(planId);
    }

    /// <inheritdoc />
    public DispatchAlterationJobs([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
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
        var alterationPlanStore = context.GetRequiredService<IAlterationPlanStore>();
        var planFilter = new AlterationPlanFilter
        {
            Id = planId
        };
        var plan = await alterationPlanStore.FindAsync(planFilter, cancellationToken);

        if (plan == null)
            throw new FaultException(AlterationFaultCodes.PlanNotFound, AlterationFaultCategories.Alteration, DefaultFaultTypes.System, $"Alteration Plan with ID {planId} not found.");

        // Update status.
        plan.Status = AlterationPlanStatus.Dispatching;
        await alterationPlanStore.SaveAsync(plan, cancellationToken);

        // Find all jobs for the plan and dispatch them.
        var filter = new AlterationJobFilter
        {
            PlanId = plan.Id
        };
        var alterationJobStore = context.GetRequiredService<IAlterationJobStore>();
        var alterationJobIds = await alterationJobStore.FindManyIdsAsync(filter, cancellationToken);

        // Dispatch each job.
        var alterationJobDispatcher = context.GetRequiredService<IAlterationJobDispatcher>();
        foreach (var jobId in alterationJobIds)
            await alterationJobDispatcher.DispatchAsync(jobId, cancellationToken);

        // Update status.
        plan.Status = AlterationPlanStatus.Running;
        await alterationPlanStore.SaveAsync(plan, cancellationToken);
    }
}