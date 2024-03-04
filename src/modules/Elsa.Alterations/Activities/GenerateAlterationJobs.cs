using System.ComponentModel;
using System.Runtime.CompilerServices;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Entities;
using Elsa.Alterations.Core.Enums;
using Elsa.Alterations.Core.Filters;
using Elsa.Common.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Exceptions;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.Alterations.Activities;

/// <summary>
/// Submits an alteration plan for execution.
/// </summary>
[Browsable(false)]
[Activity("Elsa", "Alterations", "Generates jobs for the specified Alteration Plan", Kind = ActivityKind.Job)]
public class GenerateAlterationJobs : CodeActivity
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
        var cancellationToken = context.CancellationToken;
        var systemClock = context.GetRequiredService<ISystemClock>();
        var identityGenerator = context.GetRequiredService<IIdentityGenerator>();
        var now = systemClock.UtcNow;
        var planId = context.Get(PlanId)!;
        var alterationPlanStore = context.GetRequiredService<IAlterationPlanStore>();
        var planFilter = new AlterationPlanFilter
        {
            Id = planId
        };
        var plan = await alterationPlanStore.FindAsync(planFilter, cancellationToken);

        if (plan == null)
            throw new FaultException($"Alteration Plan with ID {planId} not found.");

        // Update status.
        plan.Status = AlterationPlanStatus.Generating;
        await alterationPlanStore.SaveAsync(plan, cancellationToken);

        // Process all matching workflow instances and generate jobs.
        var filter = plan.WorkflowInstanceFilter;
        var workflowInstanceFilter = new WorkflowInstanceFilter
        {
            Ids = filter.WorkflowInstanceIds?.ToList(),
            DefinitionVersionIds = filter.DefinitionVersionIds?.ToList(),
            CorrelationIds = filter.CorrelationIds?.ToList(),
            HasIncidents = filter.HasIncidents,
            TimestampFilters = filter.TimestampFilters?.ToList(),
        };
        var activityExecutionFilters = filter.ActivityFilters?.Select(x => new ActivityExecutionRecordFilter
        {
            ActivityId = x.Id,
            ActivityNodeId = x.NodeId,
            Name = x.Name,
            Status = x.Status,
        }).ToList();

        var workflowInstanceStore = context.GetRequiredService<IWorkflowInstanceStore>();
        var activityExecutionStore = context.GetRequiredService<IActivityExecutionStore>();
        var workflowInstanceIds = workflowInstanceFilter.IsEmpty ? Enumerable.Empty<string>().ToHashSet() : (await workflowInstanceStore.FindManyIdsAsync(workflowInstanceFilter, cancellationToken)).ToHashSet();

        if (activityExecutionFilters != null)
        {
            foreach (ActivityExecutionRecordFilter activityExecutionFilter in activityExecutionFilters.Where(x => !x.IsEmpty))
            {
                var activityExecutionRecords = await activityExecutionStore.FindManySummariesAsync(activityExecutionFilter, cancellationToken);
                var matchingWorkflowInstanceIds = activityExecutionRecords.Select(x => x.WorkflowInstanceId).ToHashSet();
                workflowInstanceIds.UnionWith(matchingWorkflowInstanceIds);
            }
        }

        var jobs = workflowInstanceIds.Select(workflowInstanceId => new AlterationJob
            {
                Id = identityGenerator.GenerateId(),
                PlanId = plan.Id,
                Status = AlterationJobStatus.Pending,
                WorkflowInstanceId = workflowInstanceId,
                CreatedAt = now
            })
            .ToList();

        var alterationJobStore = context.GetRequiredService<IAlterationJobStore>();
        await alterationJobStore.SaveManyAsync(jobs, cancellationToken);
    }
}