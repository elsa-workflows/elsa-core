using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Exceptions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services.Models;
using Elsa.Services.WorkflowStorage;
using Open.Linq.AsyncExtensions;

namespace Elsa.Services.Workflows
{
    public class WorkflowResumer : IFindsAndResumesWorkflows, IResumesWorkflows, IBuildsAndResumesWorkflow, IResumesWorkflow
    {
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IBookmarkFinder _bookmarkFinder;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IWorkflowStorageService _workflowStorageService;
        private readonly Func<IWorkflowBuilder> _workflowBuilderFactory;
        private readonly IWorkflowRunner _workflowRunner;

        public WorkflowResumer(
            IWorkflowRegistry workflowRegistry,
            IBookmarkFinder bookmarkFinder,
            IWorkflowInstanceStore workflowInstanceStore,
            IWorkflowStorageService workflowStorageService,
            Func<IWorkflowBuilder> workflowBuilderFactory,
            IWorkflowRunner workflowRunner)
        {
            _workflowRegistry = workflowRegistry;
            _bookmarkFinder = bookmarkFinder;
            _workflowInstanceStore = workflowInstanceStore;
            _workflowStorageService = workflowStorageService;
            _workflowBuilderFactory = workflowBuilderFactory;
            _workflowRunner = workflowRunner;
        }

        public async Task FindAndResumeWorkflowsAsync(
            string activityType,
            IBookmark bookmark,
            string? tenantId,
            WorkflowInput? input = default,
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default)
        {
            var results = await _bookmarkFinder.FindBookmarksAsync(activityType, bookmark, correlationId, tenantId, cancellationToken: cancellationToken).ToList();
            await ResumeWorkflowsAsync(results, input, cancellationToken);
        }

        public async Task ResumeWorkflowsAsync(IEnumerable<BookmarkFinderResult> results, WorkflowInput? input = default, CancellationToken cancellationToken = default)
        {
            foreach (var result in results)
            {
                var workflowInstance = await _workflowInstanceStore.FindByIdAsync(result.WorkflowInstanceId, cancellationToken);

                if (workflowInstance?.WorkflowStatus == WorkflowStatus.Suspended)
                {
                    await _workflowStorageService.UpdateInputAsync(workflowInstance, input, cancellationToken);
                    await ResumeWorkflowAsync(workflowInstance!, result.ActivityId, cancellationToken);
                }
            }
        }

        public async Task<RunWorkflowResult> BuildAndResumeWorkflowAsync<T>(WorkflowInstance workflowInstance, string? activityId = default, CancellationToken cancellationToken = default) where T : IWorkflow
        {
            var workflowBlueprint = _workflowBuilderFactory().Build<T>();
            return await _workflowRunner.RunWorkflowAsync(workflowBlueprint, workflowInstance, activityId, cancellationToken);
        }

        public async Task<RunWorkflowResult> BuildAndResumeWorkflowAsync(IWorkflow workflow, WorkflowInstance workflowInstance, string? activityId = default, CancellationToken cancellationToken = default)
        {
            var workflowBlueprint = _workflowBuilderFactory().Build(workflow);
            return await _workflowRunner.RunWorkflowAsync(workflowBlueprint, workflowInstance, activityId, cancellationToken);
        }

        public async Task<RunWorkflowResult> ResumeWorkflowAsync(WorkflowInstance workflowInstance, string? activityId = default, CancellationToken cancellationToken = default)
        {
            var workflowBlueprint = await _workflowRegistry.FindAsync(
                workflowInstance.DefinitionId,
                VersionOptions.SpecificVersion(workflowInstance.Version),
                workflowInstance.TenantId,
                cancellationToken);

            if (workflowBlueprint == null)
                throw new WorkflowException($"Workflow instance {workflowInstance.Id} references workflow definition {workflowInstance.DefinitionId} version {workflowInstance.Version}, but no such workflow definition was found.");

            if (workflowBlueprint.IsDisabled)
            {
                return new RunWorkflowResult(workflowInstance, activityId, null, false);
            }

            return await _workflowRunner.RunWorkflowAsync(workflowBlueprint, workflowInstance, activityId, cancellationToken);
        }
    }
}