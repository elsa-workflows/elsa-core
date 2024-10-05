using System.ComponentModel;
using System.Runtime.CompilerServices;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Enums;
using Elsa.Alterations.Core.Filters;
using Elsa.Alterations.Core.Models;
using Elsa.Common;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Exceptions;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;

namespace Elsa.Alterations.Activities;

/// <summary>
/// Submits an alteration plan for execution.
/// </summary>
[Browsable(false)]
[Activity("Elsa", "Alterations", "Generates jobs for the specified Alteration Plan", Kind = ActivityKind.Job)]
public class GenerateAlterationJobs : CodeActivity<int>
{
    /// <inheritdoc />
    public GenerateAlterationJobs([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <inheritdoc />
    public GenerateAlterationJobs(Variable<string> planId, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
        PlanId = new Input<string>(planId);
    }

    /// <summary>
    /// The ID of the submitted alteration plan.
    /// </summary>
    public Input<string> PlanId { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var plan = await GetPlanAsync(context);
        await UpdatePlanStatusAsync(context, plan);
        var workflowInstanceIds = (await FindMatchingWorkflowInstanceIdsAsync(context, plan.WorkflowInstanceFilter)).ToList();

        if (workflowInstanceIds.Any())
            await GenerateJobsAsync(context, plan, workflowInstanceIds);

        context.SetResult(workflowInstanceIds.Count);
    }

    private async Task<AlterationPlan> GetPlanAsync(ActivityExecutionContext context)
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

        return plan;
    }

    private async Task UpdatePlanStatusAsync(ActivityExecutionContext context, AlterationPlan plan)
    {
        var cancellationToken = context.CancellationToken;
        var alterationPlanStore = context.GetRequiredService<IAlterationPlanStore>();
        plan.Status = AlterationPlanStatus.Generating;
        await alterationPlanStore.SaveAsync(plan, cancellationToken);
    }

    private async Task<IEnumerable<string>> FindMatchingWorkflowInstanceIdsAsync(ActivityExecutionContext context, AlterationWorkflowInstanceFilter filter)
    {
        var cancellationToken = context.CancellationToken;
        var workflowInstanceFinder = context.GetRequiredService<IWorkflowInstanceFinder>();
        return await workflowInstanceFinder.FindAsync(filter, cancellationToken);
    }

    private async Task GenerateJobsAsync(ActivityExecutionContext context, AlterationPlan plan, IEnumerable<string> workflowInstanceIds)
    {
        var cancellationToken = context.CancellationToken;
        var identityGenerator = context.GetRequiredService<IIdentityGenerator>();
        var systemClock = context.GetRequiredService<ISystemClock>();
        var jobs = workflowInstanceIds.Select(workflowInstanceId => new AlterationJob
            {
                Id = identityGenerator.GenerateId(),
                PlanId = plan.Id,
                Status = AlterationJobStatus.Pending,
                WorkflowInstanceId = workflowInstanceId,
                CreatedAt = systemClock.UtcNow
            })
            .ToList();

        var alterationJobStore = context.GetRequiredService<IAlterationJobStore>();
        await alterationJobStore.SaveManyAsync(jobs, cancellationToken);
    }
}