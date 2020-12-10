﻿// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Linq.Expressions;
// using System.Threading;
// using System.Threading.Tasks;
// using Elsa.Events;
// using Elsa.Exceptions;
// using Elsa.Extensions;
// using Elsa.Indexes;
// using Elsa.Messages;
// using Elsa.Models;
// using Elsa.Services.Models;
// using Elsa.Triggers;
// using MediatR;
// using Open.Linq.AsyncExtensions;
//
// namespace Elsa.Services
// {
//     public class WorkflowScheduler : IWorkflowScheduler, INotificationHandler<WorkflowCompleted>
//     {
//         private readonly ICommandSender _commandSender;
//         private readonly IWorkflowInstanceRepository _workflowInstanceManager;
//         private readonly IWorkflowFactory _workflowFactory;
//         private readonly IWorkflowSelector _workflowSelector;
//         private readonly IWorkflowRegistry _workflowRegistry;
//         private readonly IWorkflowSchedulerQueue _queue;
//
//         public WorkflowScheduler(
//             ICommandSender commandSender,
//             IWorkflowInstanceRepository workflowInstanceRepository,
//             IWorkflowFactory workflowFactory,
//             IWorkflowSelector workflowSelector,
//             IWorkflowRegistry workflowRegistry,
//             IWorkflowSchedulerQueue queue)
//         {
//             _commandSender = commandSender;
//             _workflowInstanceManager = workflowInstanceRepository;
//             _workflowFactory = workflowFactory;
//             _workflowSelector = workflowSelector;
//             _workflowRegistry = workflowRegistry;
//             _queue = queue;
//         }
//
//         public async Task ScheduleWorkflowInstanceAsync(
//             string instanceId,
//             string? activityId = default,
//             object? input = default,
//             CancellationToken cancellationToken = default)
//         {
//             await _commandSender.SendAsync(new RunWorkflow(instanceId, activityId, input));
//         }
//         
//         public async Task<IEnumerable<WorkflowInstance>> ScheduleWorkflowDefinitionAsync(
//             string definitionId,
//             string activityId,
//             string? tenantId,
//             object? input = default,
//             string? correlationId = default,
//             string? contextId = default,
//             CancellationToken cancellationToken = default)
//         {
//             var workflow = await _workflowRegistry.GetWorkflowAsync(
//                 definitionId,
//                 tenantId,
//                 VersionOptions.Published,
//                 cancellationToken);
//
//             if (workflow == null)
//                 throw new WorkflowException($"No workflow definition found by ID {definitionId}");
//
//             var activity = workflow.GetActivity(activityId)!;
//             var workflowInstances = new List<WorkflowInstance>();
//             var workflowInstance  = await ScheduleWorkflowAsync(workflow, activity, input, correlationId, contextId, cancellationToken);
//             
//             workflowInstances.Add(workflowInstance);
//             return workflowInstances;
//         }
//
//         public async Task<IEnumerable<WorkflowInstance>> ScheduleWorkflowDefinitionAsync(
//             string definitionId,
//             string? tenantId,
//             object? input = default,
//             string? correlationId = default,
//             string? contextId = default,
//             CancellationToken cancellationToken = default)
//         {
//             var workflow = await _workflowRegistry.GetWorkflowAsync(
//                 definitionId,
//                 tenantId,
//                 VersionOptions.Published,
//                 cancellationToken);
//
//             if (workflow == null)
//                 throw new WorkflowException($"No workflow definition found by ID {definitionId}");
//
//             var startActivities = workflow.GetStartActivities();
//             var workflowInstances = new List<WorkflowInstance>();
//
//             foreach (var activity in startActivities)
//             {
//                 var workflowInstance  = await ScheduleWorkflowAsync(workflow, activity, input, correlationId, contextId, cancellationToken);
//                 workflowInstances.Add(workflowInstance);
//             }
//
//             return workflowInstances;
//         }
//
//         public async Task TriggerWorkflowsAsync(
//             string activityType,
//             object? input = default,
//             string? correlationId = default,
//             string? contextId = default,
//             CancellationToken cancellationToken = default)
//         {
//             await ScheduleSuspendedWorkflowsAsync(
//                 activityType,
//                 input,
//                 correlationId,
//                 cancellationToken);
//
//             await ScheduleNewWorkflowsAsync(
//                 activityType,
//                 input,
//                 correlationId,
//                 contextId,
//                 cancellationToken);
//         }
//
//         public async Task TriggerWorkflowsAsync<TTrigger>(Func<TTrigger, bool> predicate, object? input = default, string? correlationId = default, string? contextId = default, CancellationToken cancellationToken = default)
//             where TTrigger : ITrigger
//         {
//             var results = await _workflowSelector.SelectWorkflowsAsync(predicate, cancellationToken).ToList();
//
//             foreach (var result in results)
//             {
//                 if (result.WorkflowInstanceId != null)
//                     await ScheduleWorkflowInstanceAsync(result.WorkflowInstanceId, result.ActivityId, input, cancellationToken);
//                 else
//                     await ScheduleWorkflowAsync(result.WorkflowBlueprint, result.WorkflowBlueprint.GetActivity(result.ActivityId)!, input, correlationId, contextId, cancellationToken);
//
//                 if (result.Trigger.IsOneOff)
//                     await _workflowSelector.RemoveTriggerAsync(result.Trigger, cancellationToken);
//             }
//         }
//
//         /// <summary>
//         /// Find workflows with the specified activity type as workflow triggers.
//         /// </summary>
//         private async Task ScheduleNewWorkflowsAsync(
//             string activityType,
//             object? input = default,
//             string? correlationId = default,
//             string? contextId = default,
//             CancellationToken cancellationToken = default)
//         {
//             var workflows = await _workflowRegistry.GetWorkflowsAsync(cancellationToken).ToListAsync(cancellationToken);
//
//             workflows = await FilterRunningSingletonsAsync(workflows).ToList();
//
//             var query =
//                 from workflow in workflows
//                 where workflow.IsPublished && workflow.IsEnabled
//                 from activity in workflow.GetStartActivities()
//                 where activity.Type == activityType
//                 select (workflow, activity);
//
//             var tuples = (IList<(IWorkflowBlueprint Workflow, IActivityBlueprint Activity)>)query.ToList();
//
//             foreach (var (workflow, activity) in tuples)
//             {
//                 var startedInstances = await GetStartedWorkflowsAsync(workflow).ToList();
//
//                 if (startedInstances.Any())
//                 {
//                     // There's already a workflow instance pending to be started, so queue this workflow for launch right after the current instance completes. 
//                     _queue.Enqueue(workflow, activity, input, correlationId, contextId);
//                 }
//                 else
//                 {
//                     var workflowInstance = await _workflowFactory.InstantiateAsync(
//                         workflow,
//                         correlationId,
//                         contextId,
//                         cancellationToken);
//
//                     await _workflowInstanceManager.SaveAsync(workflowInstance, cancellationToken);
//
//                     await ScheduleWorkflowInstanceAsync(
//                         workflowInstance.WorkflowInstanceId,
//                         activity.Id,
//                         input,
//                         cancellationToken);
//                 }
//             }
//         }
//
//         /// <summary>
//         /// Find suspended workflow instances that are blocked on activities with the specified activity type.
//         /// </summary>
//         private async Task ScheduleSuspendedWorkflowsAsync(
//             string activityType,
//             object? input,
//             string? correlationId,
//             CancellationToken cancellationToken)
//         {
//             Expression<Func<WorkflowInstanceBlockingActivitiesIndex, bool>> predicate;
//
//             if (!string.IsNullOrWhiteSpace(correlationId))
//                 predicate = x => x.ActivityType == activityType && x.CorrelationId == correlationId;
//             else
//                 predicate = x => x.ActivityType == activityType;
//
//             var query = _workflowInstanceManager.Query<WorkflowInstanceBlockingActivitiesIndex>().Where(predicate);
//             var workflowInstances = await query.ListAsync();
//             var tuples = workflowInstances.GetBlockingActivities();
//
//             foreach (var (workflowInstance, blockingActivity) in tuples)
//                 await ScheduleWorkflowInstanceAsync(
//                     workflowInstance.WorkflowInstanceId,
//                     blockingActivity.ActivityId,
//                     input,
//                     cancellationToken);
//         }
//
//         private async Task<WorkflowInstance> ScheduleWorkflowAsync(
//             IWorkflowBlueprint workflowBlueprint,
//             IActivityBlueprint activity,
//             object? input,
//             string? correlationId,
//             string? contextId,
//             CancellationToken cancellationToken)
//         {
//             var workflowInstance = await _workflowFactory.InstantiateAsync(workflowBlueprint, correlationId, contextId, cancellationToken);
//             await _workflowInstanceManager.SaveAsync(workflowInstance, cancellationToken);
//             await ScheduleWorkflowInstanceAsync(workflowInstance.WorkflowInstanceId, activity.Id, input, cancellationToken);
//             return workflowInstance;
//         }
//
//         private async Task<IEnumerable<IWorkflowBlueprint>> FilterRunningSingletonsAsync(IEnumerable<IWorkflowBlueprint> workflows)
//         {
//             var blueprints = workflows.ToList();
//             var transients = blueprints.Where(x => !x.IsSingleton).ToList();
//             var singletons = blueprints.Where(x => x.IsSingleton).ToList();
//             var result = transients.ToList();
//
//             foreach (var workflowBlueprint in singletons)
//             {
//                 var workflowDefinitionId = workflowBlueprint.Id;
//
//                 var instances = await _workflowInstanceManager
//                     .ListByDefinitionAndStatusAsync(workflowDefinitionId, workflowBlueprint.TenantId, WorkflowStatus.Suspended);
//
//                 if (!instances.Any())
//                     result.Add(workflowBlueprint);
//             }
//
//             return result;
//         }
//
//         private async Task<IEnumerable<WorkflowInstance>> GetStartedWorkflowsAsync(IWorkflowBlueprint workflowBlueprint)
//         {
//             var workflowDefinitionId = workflowBlueprint.Id;
//             var suspendedInstances = await _workflowInstanceManager.ListByDefinitionAndStatusAsync(workflowDefinitionId, workflowBlueprint.TenantId, WorkflowStatus.Suspended);
//             var idleInstances = await _workflowInstanceManager.ListByDefinitionAndStatusAsync(workflowDefinitionId, workflowBlueprint.TenantId, WorkflowStatus.Idle);
//             var startActivities = workflowBlueprint.GetStartActivities().Select(x => x.Id).ToList();
//             var startedInstances = suspendedInstances.Where(x => x.BlockingActivities.Any(y => startActivities.Contains(y.ActivityId))).ToList();
//             return idleInstances.Concat(startedInstances);
//         }
//
//         public async Task Handle(WorkflowCompleted notification, CancellationToken cancellationToken)
//         {
//             var workflowExecutionContext = notification.WorkflowExecutionContext;
//             var workflowDefinitionId = workflowExecutionContext.WorkflowBlueprint.Id;
//             var startActivityId = workflowExecutionContext.ExecutionLog.Select(x => x.ActivityId).FirstOrDefault();
//
//             if (startActivityId == null)
//                 return;
//
//             var entry = _queue.Dequeue(workflowDefinitionId, startActivityId);
//             if (entry == null)
//                 return;
//
//             await ScheduleWorkflowAsync(
//                 entry.Value.Workflow,
//                 entry.Value.Activity,
//                 entry.Value.Input,
//                 entry.Value.CorrelationId,
//                 entry.Value.ContextId,
//                 cancellationToken);
//         }
//     }
// }