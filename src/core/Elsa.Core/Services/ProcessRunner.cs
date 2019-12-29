using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Messages;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services.Extensions;
using Elsa.Services.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using NodaTime;
using ProcessInstance = Elsa.Services.Models.ProcessInstance;
using ScheduledActivity = Elsa.Services.Models.ScheduledActivity;

namespace Elsa.Services
{
    public class ProcessRunner : IProcessRunner
    {
        private readonly IActivityInvoker activityInvoker;
        private readonly IProcessFactory processFactory;
        private readonly IProcessRegistry processRegistry;
        private readonly IWorkflowInstanceStore workflowInstanceStore;
        private readonly IClock clock;
        private readonly IMediator mediator;
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger logger;
        private readonly IExpressionEvaluator expressionEvaluator;

        public ProcessRunner(
            IActivityInvoker activityInvoker,
            IProcessFactory processFactory,
            IProcessRegistry processRegistry,
            IWorkflowInstanceStore workflowInstanceStore,
            IExpressionEvaluator expressionEvaluator,
            IClock clock,
            IMediator mediator,
            IServiceProvider serviceProvider,
            ILogger<ProcessRunner> logger)
        {
            this.activityInvoker = activityInvoker;
            this.processFactory = processFactory;
            this.processRegistry = processRegistry;
            this.workflowInstanceStore = workflowInstanceStore;
            this.clock = clock;
            this.mediator = mediator;
            this.serviceProvider = serviceProvider;
            this.logger = logger;
            this.expressionEvaluator = expressionEvaluator;
        }
        
        public async Task<IEnumerable<ProcessExecutionContext>> TriggerAsync(
            string activityType,
            Variable input = default,
            string correlationId = default,
            Func<Variables, bool> activityStatePredicate = default,
            CancellationToken cancellationToken = default)
        {
            var startedExecutionContexts = await RunManyAsync(
                activityType,
                input,
                correlationId,
                activityStatePredicate,
                cancellationToken
            );

            var resumedExecutionContexts = await ResumeManyAsync(
                activityType,
                input,
                correlationId,
                activityStatePredicate,
                cancellationToken
            );

            return startedExecutionContexts.Concat(resumedExecutionContexts);
        }

        public Task<ProcessExecutionContext> RunAsync(
            ProcessInstance processInstance,
            IEnumerable<IActivity>? startActivities = default,
            CancellationToken cancellationToken = default)
        {
            return RunAsync(processInstance, false, startActivities, cancellationToken);
        }

        public Task<ProcessExecutionContext> RunAsync(
            ProcessDefinitionVersion processDefinition,
            Variable? input = default,
            IEnumerable<string> startActivityIds = default,
            string? correlationId = default,
            CancellationToken cancellationToken = default)
        {
            var blueprint = processFactory.CreateProcess(processDefinition);
            return RunAsync(blueprint, input, startActivityIds, correlationId, cancellationToken);
        }
        
        public Task<ProcessExecutionContext> RunAsync(
            Process workflowDefinition,
            Variable? input = default,
            IEnumerable<string>? startActivityIds = default,
            string? correlationId = default,
            CancellationToken cancellationToken = default)
        {
            var workflow = processFactory.CreateProcessInstance(workflowDefinition, input, correlationId: correlationId);
            //var startActivities = workflow.Blueprint.Activities.Find(startActivityIds);
            var startActivities = new IActivity[0];

            return RunAsync(workflow, false, startActivities, cancellationToken);
        }

        public Task<ProcessExecutionContext> RunAsync(
            ProcessInstance processInstance,
            Variable? input = null,
            IEnumerable<string>? startActivityIds = default,
            CancellationToken cancellationToken = default)
        {
            return RunAsync(processInstance, false, input, startActivityIds, cancellationToken);
        }

        public Task<ProcessExecutionContext> ResumeAsync(
            ProcessInstance workflow,
            IEnumerable<IActivity>? startActivities = default,
            CancellationToken cancellationToken = default)
        {
            return RunAsync(workflow, true, startActivities, cancellationToken);
        }

        public Task<ProcessExecutionContext> ResumeAsync(
            ProcessInstance processInstance,
            Variable input = null,
            IEnumerable<string> startActivityIds = default,
            CancellationToken cancellationToken = default)
        {
            return RunAsync(processInstance, true, input, startActivityIds, cancellationToken);
        }
        
        private async Task<ProcessExecutionContext> RunAsync(
            ProcessInstance processInstance,
            bool resume,
            Variable input = null,
            IEnumerable<string> startActivityIds = default,
            CancellationToken cancellationToken = default)
        {
            var definition = await processRegistry.GetProcessAsync(
                processInstance.Blueprint.DefinitionId,
                VersionOptions.SpecificVersion(processInstance.Blueprint.Version),
                cancellationToken);
            var workflow = processFactory.CreateProcessInstance(definition, input, processInstance);
            return await RunAsync(workflow, resume, startActivityIds, cancellationToken);
        }

        private async Task<IEnumerable<ProcessExecutionContext>> ResumeManyAsync(
            string activityType,
            Variable input = default,
            string correlationId = default,
            Func<Variables, bool> activityStatePredicate = default,
            CancellationToken cancellationToken = default)
        {
            var workflowInstances = await workflowInstanceStore
                .ListByBlockingActivityAsync(activityType, correlationId, cancellationToken)
                .ToListAsync();

            if (activityStatePredicate != null)
                workflowInstances = workflowInstances.Where(x => activityStatePredicate(x.Item2.State)).ToList();

            var executionContexts = new List<ProcessExecutionContext>();
            // var workflowInstanceGroups = workflowInstances.GroupBy(x => x.Item1);
            //
            // foreach (var workflowInstanceGroup in workflowInstanceGroups)
            // {
            //     var workflowInstance = workflowInstanceGroup.Key;
            //
            //     var workflowDefinition = await processRegistry.GetProcessAsync(
            //         workflowInstance.DefinitionId,
            //         VersionOptions.SpecificVersion(workflowInstance.Version),
            //         cancellationToken
            //     );
            //
            //     var workflow = processFactory.CreateProcessInstance(workflowDefinition, input, workflowInstance);
            //
            //     foreach (var activity in workflowInstanceGroup)
            //     {
            //         var executionContext = await RunAsync(
            //             workflow,
            //             true,
            //             new[] { activity.Item2.Id },
            //             cancellationToken
            //         );
            //
            //         executionContexts.Add(executionContext);
            //     }
            // }

            return executionContexts;
        }

        private async Task<IEnumerable<ProcessExecutionContext>> RunManyAsync(
            string activityType,
            Variable input = default,
            string correlationId = default,
            Func<Variables, bool> activityStatePredicate = default,
            CancellationToken cancellationToken = default)
        {
            // var workflowDefinitions = await processRegistry.ListByStartActivityAsync(activityType, cancellationToken);
            //
            // if (activityStatePredicate != null)
            //     workflowDefinitions = workflowDefinitions.Where(x => activityStatePredicate(x.Item2.State));
            //
            // workflowDefinitions = await FilterRunningSingletonsAsync(
            //     workflowDefinitions,
            //     cancellationToken
            // );
            //
            // return await RunManyAsync(workflowDefinitions, input, correlationId, cancellationToken);
            
            throw new NotImplementedException();
        }

        private async Task<IEnumerable<ProcessExecutionContext>> RunManyAsync(
            IEnumerable<(Process, IActivity)> workflowDefinitions,
            Variable input,
            string correlationId,
            CancellationToken cancellationToken1)
        {
            var executionContexts = new List<ProcessExecutionContext>();

            // foreach (var (workflowDefinition, activityDefinition) in workflowDefinitions)
            // {
            //     var startActivityIds = workflowDefinition.Activities
            //         .Where(x => x.Id == activityDefinition.Id)
            //         .Select(x => x.Id);
            //
            //     var workflow = processFactory.CreateProcessInstance(workflowDefinition, input, correlationId: correlationId);
            //
            //     var executionContext = await RunAsync(
            //         workflow,
            //         false,
            //         startActivityIds,
            //         cancellationToken1
            //     );
            //     executionContexts.Add(executionContext);
            // }

            return executionContexts;
        }
        
        private Task<ProcessExecutionContext> RunAsync(
            ProcessInstance processInstance,
            bool resume,
            IEnumerable<string> startActivityIds = default,
            CancellationToken cancellationToken = default)
        {
            // var startActivities = startActivityIds != null
            //     ? processInstance.Blueprint.Activities.Find(startActivityIds)
            //     : Enumerable.Empty<IActivity>();
            var startActivities = Enumerable.Empty<string>();

            return RunAsync(processInstance, resume, startActivities, cancellationToken);
        }
        
        private async Task<ProcessExecutionContext> RunAsync(
            ProcessInstance processInstance,
            bool resume,
            IEnumerable<IActivity> startActivities = default,
            CancellationToken cancellationToken = default)
        {
            await mediator.Publish(new ExecutingProcess(processInstance), cancellationToken);
            
            var workflowExecutionContext = await CreateWorkflowExecutionContextAsync(
                processInstance,
                startActivities,
                cancellationToken
            );

            var start = !resume;

            while (workflowExecutionContext.HasScheduledActivities)
            {
                var nextActivity = workflowExecutionContext.PeekScheduledActivity();
                await mediator.Publish(new ExecutingActivity(processInstance, nextActivity.Activity), cancellationToken);
                var scheduledActivity = workflowExecutionContext.PopScheduledActivity();
                var activity = scheduledActivity.Activity;

                var result = start
                    ? await ExecuteActivityAsync(workflowExecutionContext, scheduledActivity, cancellationToken)
                    : await ResumeActivityAsync(workflowExecutionContext, scheduledActivity, cancellationToken);

                await mediator.Publish(new ActivityExecuted(processInstance, activity), cancellationToken);
                
                if (result != null)
                    await result.ExecuteAsync(this, workflowExecutionContext, cancellationToken);
                
                workflowExecutionContext.IsFirstPass = false;
                start = true;
            }
            
            // Determine new workflow state.
            UpdateWorkflowStatus(workflowExecutionContext);
            
            // Publish appropriate event depending on current workflow state.
            await PublishTransitionEvent(workflowExecutionContext, cancellationToken);

            return workflowExecutionContext;
        }

        private static void UpdateWorkflowStatus( ProcessExecutionContext processExecutionContext)
        {
            // Automatically transition to Suspended state if there are blocking activities.
            if (processExecutionContext.ProcessInstance.BlockingActivities.Any())
                processExecutionContext.Suspend();

            // Automatically transition to Completed state if there are no more blocking activities. 
            else if(!processExecutionContext.HasScheduledActivities)
                processExecutionContext.Complete();
        }

        private async Task PublishTransitionEvent(ProcessExecutionContext processExecutionContext, CancellationToken cancellationToken)
        {
            var workflow = processExecutionContext.ProcessInstance;
            
            // Publish Workflow Executed event.
            await mediator.Publish(new ProcessExecuted(workflow), cancellationToken);
            
            switch (workflow.Status)
            {
                case ProcessStatus.Cancelled:
                    await mediator.Publish(new ProcessCancelled(workflow), cancellationToken);
                    break;
                case ProcessStatus.Completed:
                    await mediator.Publish(new ProcessCompleted(workflow), cancellationToken);
                    break;
                case ProcessStatus.Faulted:
                    await mediator.Publish(new ProcessFaulted(workflow), cancellationToken);
                    break;
                case ProcessStatus.Suspended:
                    await mediator.Publish(new ProcessSuspended(workflow), cancellationToken);
                    break;
            }
        }

        private async Task<IActivityExecutionResult> ExecuteActivityAsync(
            ProcessExecutionContext processContext,
            ScheduledActivity scheduledActivity,
            CancellationToken cancellationToken)
        {
            return await InvokeActivityAsync(
                processContext,
                scheduledActivity.Activity,
                async () => await activityInvoker.ExecuteAsync(processContext, scheduledActivity.Activity, scheduledActivity.Input, cancellationToken),
                cancellationToken
            );
        }

        private async Task<IActivityExecutionResult> ResumeActivityAsync(
            ProcessExecutionContext processContext,
            ScheduledActivity scheduledActivity,
            CancellationToken cancellationToken)
        {
            return await InvokeActivityAsync(
                processContext,
                scheduledActivity.Activity,
                async () => await activityInvoker.ResumeAsync(processContext, scheduledActivity.Activity, scheduledActivity.Input, cancellationToken),
                cancellationToken
            );
        }

        private async Task<IActivityExecutionResult> InvokeActivityAsync(
            ProcessExecutionContext processContext,
            IActivity activity,
            Func<Task<IActivityExecutionResult>> executeAction,
            CancellationToken cancellationToken)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    processContext.ProcessInstance.Status = ProcessStatus.Cancelled;
                    processContext.ProcessInstance.CompletedAt = clock.GetCurrentInstant();
                    return null;
                }

                return await executeAction();
            }
            catch (Exception ex)
            {
                FaultWorkflow(processContext, activity, ex);
            }

            return null;
        }

        private void FaultWorkflow(ProcessExecutionContext processContext, IActivity activity, Exception ex)
        {
            logger.LogError(
                ex,
                "An unhandled error occurred while executing an activity. Putting the workflow in the faulted state."
            );
            processContext.Fault(activity, ex);
        }

        private async Task<ProcessExecutionContext> CreateWorkflowExecutionContextAsync(
            ProcessInstance workflow,
            IEnumerable<IActivity> startActivities,
            CancellationToken cancellationToken)
        {
            var workflowExecutionContext = new ProcessExecutionContext(workflow, expressionEvaluator, clock, serviceProvider);
            //var startActivityList = startActivities?.ToList() ?? workflow.GetStartActivities().Take(1).ToList();
            var startActivityList = startActivities?.ToList() ?? new List<IActivity>();

            foreach (var startActivity in startActivityList)
            {
                var activityExecutionContext = new ActivityExecutionContext(workflowExecutionContext, workflow.Input);
                if (await startActivity.CanExecuteAsync(activityExecutionContext, cancellationToken))
                    workflowExecutionContext.ScheduleActivity(startActivity, workflow.Input);
            }

            if (workflowExecutionContext.HasScheduledActivities)
            {
                workflow.BlockingActivities.RemoveWhere(startActivityList.Contains);
                workflowExecutionContext.Run();
            }

            return workflowExecutionContext;
        }

        private async Task<IEnumerable<(Process, IActivity)>> FilterRunningSingletonsAsync(
            IEnumerable<(Process, IActivity)> workflowDefinitions,
            CancellationToken cancellationToken)
        {
            var definitions = workflowDefinitions.ToList();
            var transients = definitions.Where(x => !x.Item1.IsSingleton).ToList();
            var singletons = definitions.Where(x => x.Item1.IsSingleton).ToList();
            var result = transients.ToList();

            foreach (var definition in singletons)
            {
                var instances = await workflowInstanceStore.ListByStatusAsync(
                    definition.Item1.DefinitionId,
                    ProcessStatus.Suspended,
                    cancellationToken
                );

                if (!instances.Any())
                {
                    result.Add(definition);
                }
            }

            return result;
        }
    }
}