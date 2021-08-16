using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services.Bookmarks;
using Elsa.Services.Models;
using Elsa.Services.Triggers;
using Open.Linq.AsyncExtensions;

namespace Elsa.Services.Workflows
{
    public class WorkflowStarter : IFindsAndStartsWorkflows, IStartsWorkflows, IStartsWorkflow, IBuildsAndStartsWorkflow
    {
        private readonly ITriggerFinder _triggerFinder;
        private readonly IWorkflowFactory _workflowFactory;
        private readonly Func<IWorkflowBuilder> _workflowBuilderFactory;
        private readonly IWorkflowRunner _workflowRunner;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;

        public WorkflowStarter(ITriggerFinder triggerFinder, IWorkflowFactory workflowFactory, Func<IWorkflowBuilder> workflowBuilderFactory, IWorkflowRunner workflowRunner, IWorkflowInstanceStore workflowInstanceStore)
        {
            _triggerFinder = triggerFinder;
            _workflowFactory = workflowFactory;
            _workflowBuilderFactory = workflowBuilderFactory;
            _workflowRunner = workflowRunner;
            _workflowInstanceStore = workflowInstanceStore;
        }

        public async Task FindAndStartWorkflowsAsync(string activityType, IBookmark bookmark, string? tenantId, WorkflowInput? input = default, string? contextId = default, CancellationToken cancellationToken = default)
        {
            var results = await _triggerFinder.FindTriggersAsync(activityType, bookmark, tenantId, cancellationToken).ToList();
            await StartWorkflowsAsync(results, input, contextId, cancellationToken);
        }

        public async Task StartWorkflowsAsync(
            string activityType,
            IBookmark bookmark,
            string? tenantId,
            WorkflowInput? input = default,
            string? contextId = default,
            CancellationToken cancellationToken = default)
        {
            var results = await _triggerFinder.FindTriggersAsync(activityType, bookmark, tenantId, cancellationToken).ToList();
            await StartWorkflowsAsync(results, input, contextId, cancellationToken);
        }

        public async Task<IEnumerable<RunWorkflowResult>> StartWorkflowsAsync(
            IEnumerable<TriggerFinderResult> results,
            WorkflowInput? input = default,
            string? contextId = default,
            CancellationToken cancellationToken = default)
        {
            var runWorkflowResults = new List<RunWorkflowResult>();
            
            foreach (var result in results)
            {
                var workflowBlueprint = result.WorkflowBlueprint;
                var runWorkflowResult = await StartWorkflowAsync(workflowBlueprint, result.ActivityId, input, contextId: contextId, cancellationToken: cancellationToken);
                runWorkflowResults.Add(runWorkflowResult);
            }

            return runWorkflowResults;
        }

        public async Task<IEnumerable<RunWorkflowResult>> StartWorkflowsAsync(
            IEnumerable<IWorkflowBlueprint> workflowBlueprints, 
            WorkflowInput? input = default, 
            string? contextId = default, 
            CancellationToken cancellationToken = default)
        {
            var runWorkflowResults = new List<RunWorkflowResult>();
            
            foreach (var workflowBlueprint in workflowBlueprints)
            {
                var runWorkflowResult = await StartWorkflowAsync(workflowBlueprint, null, input, contextId: contextId, cancellationToken: cancellationToken);
                runWorkflowResults.Add(runWorkflowResult);
            }

            return runWorkflowResults;
        }

        public async Task<RunWorkflowResult> StartWorkflowAsync(
            IWorkflowBlueprint workflowBlueprint,
            string? activityId = default,
            WorkflowInput? input = default,
            string? correlationId = default,
            string? contextId = default,
            string? tenantId = default,
            CancellationToken cancellationToken = default)
        {
            var workflowInstance = await _workflowFactory.InstantiateAsync(
                workflowBlueprint,
                correlationId,
                contextId,
                tenantId,
                cancellationToken: cancellationToken);

            await _workflowInstanceStore.SaveAsync(workflowInstance, cancellationToken);
            return await _workflowRunner.RunWorkflowAsync(workflowBlueprint, workflowInstance, activityId, input, cancellationToken);
        }

        public async Task<RunWorkflowResult> BuildAndStartWorkflowAsync<T>(
            string? activityId = default,
            WorkflowInput? input = default,
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default) where T : IWorkflow
        {
            var workflowBlueprint = _workflowBuilderFactory().Build<T>();
            return await StartWorkflowAsync(workflowBlueprint, activityId, input, correlationId, contextId, cancellationToken: cancellationToken);
        }

        public async Task<RunWorkflowResult> BuildAndStartWorkflowAsync(
            IWorkflow workflow,
            string? activityId = default,
            WorkflowInput? input = default,
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default)
        {
            var workflowBlueprint = _workflowBuilderFactory().Build(workflow);
            return await StartWorkflowAsync(workflowBlueprint, activityId, input, correlationId, contextId, cancellationToken: cancellationToken);
        }
    }
}