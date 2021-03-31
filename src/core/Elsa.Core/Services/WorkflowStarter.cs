using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Bookmarks;
using Elsa.Builders;
using Elsa.Models;
using Elsa.Services.Models;
using Elsa.Triggers;
using Open.Linq.AsyncExtensions;

namespace Elsa.Services
{
    public class WorkflowStarter : IFindsAndStartsWorkflows, IStartsWorkflows, IStartsWorkflow, IBuildsAndStartsWorkflow
    {
        private readonly ITriggerFinder _triggerFinder;
        private readonly IWorkflowFactory _workflowFactory;
        private readonly Func<IWorkflowBuilder> _workflowBuilderFactory;
        private readonly IWorkflowRunner _workflowRunner;

        public WorkflowStarter(ITriggerFinder triggerFinder, IWorkflowFactory workflowFactory, Func<IWorkflowBuilder> workflowBuilderFactory, IWorkflowRunner workflowRunner)
        {
            _triggerFinder = triggerFinder;
            _workflowFactory = workflowFactory;
            _workflowBuilderFactory = workflowBuilderFactory;
            _workflowRunner = workflowRunner;
        }

        public async Task FindAndStartWorkflowsAsync(string activityType, IBookmark bookmark, string? tenantId, object? input = default, string? contextId = default, CancellationToken cancellationToken = default)
        {
            var results = await _triggerFinder.FindTriggersAsync(activityType, bookmark, tenantId, cancellationToken).ToList();
            await StartWorkflowsAsync(results, input, contextId, cancellationToken);
        }

        public async Task StartWorkflowsAsync(
            string activityType,
            IBookmark bookmark,
            string? tenantId,
            object? input = default,
            string? contextId = default,
            CancellationToken cancellationToken = default)
        {
            var results = await _triggerFinder.FindTriggersAsync(activityType, bookmark, tenantId, cancellationToken).ToList();
            await StartWorkflowsAsync(results, input, contextId, cancellationToken);
        }

        public async Task StartWorkflowsAsync(
            IEnumerable<TriggerFinderResult> results,
            object? input = default,
            string? contextId = default,
            CancellationToken cancellationToken = default)
        {
            foreach (var result in results)
            {
                var workflowBlueprint = result.WorkflowBlueprint;
                await StartWorkflowAsync(workflowBlueprint, result.ActivityId, input, contextId: contextId, cancellationToken: cancellationToken);
            }
        }

        public async Task<WorkflowInstance> StartWorkflowAsync(
            IWorkflowBlueprint workflowBlueprint,
            string? activityId = default,
            object? input = default,
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default)
        {
            var workflowInstance = await _workflowFactory.InstantiateAsync(
                workflowBlueprint,
                correlationId,
                contextId,
                cancellationToken);

            return await _workflowRunner.RunWorkflowAsync(workflowBlueprint, workflowInstance, activityId, input, cancellationToken);
        }

        public async Task<WorkflowInstance> BuildAndStartWorkflowAsync<T>(
            string? activityId = default,
            object? input = default,
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default) where T : IWorkflow
        {
            var workflowBlueprint = _workflowBuilderFactory().Build<T>();
            return await StartWorkflowAsync(workflowBlueprint, activityId, input, correlationId, contextId, cancellationToken);
        }

        public async Task<WorkflowInstance> BuildAndStartWorkflowAsync(
            IWorkflow workflow,
            string? activityId = default,
            object? input = default,
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default)
        {
            var workflowBlueprint = _workflowBuilderFactory().Build(workflow);
            return await StartWorkflowAsync(workflowBlueprint, activityId, input, correlationId, contextId, cancellationToken);
        }
    }
}