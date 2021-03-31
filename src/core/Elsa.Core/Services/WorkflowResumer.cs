﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Bookmarks;
using Elsa.Builders;
using Elsa.Exceptions;
using Elsa.Models;
using Elsa.Persistence;
using Open.Linq.AsyncExtensions;

namespace Elsa.Services
{
    public class WorkflowResumer : IFindsAndResumesWorkflows, IResumesWorkflows, IBuildsAndResumesWorkflow, IResumesWorkflow
    {
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IBookmarkFinder _bookmarkFinder;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly Func<IWorkflowBuilder> _workflowBuilderFactory;
        private readonly IWorkflowRunner _workflowRunner;

        public WorkflowResumer(IWorkflowRegistry workflowRegistry, IBookmarkFinder bookmarkFinder, IWorkflowInstanceStore workflowInstanceStore, Func<IWorkflowBuilder> workflowBuilderFactory, IWorkflowRunner workflowRunner)
        {
            _workflowRegistry = workflowRegistry;
            _bookmarkFinder = bookmarkFinder;
            _workflowInstanceStore = workflowInstanceStore;
            _workflowBuilderFactory = workflowBuilderFactory;
            _workflowRunner = workflowRunner;
        }

        public async Task FindAndResumeWorkflowsAsync(
            string activityType,
            IBookmark bookmark,
            string? tenantId,
            object? input = default,
            string? correlationId = default,
            string? contextId = default,
            CancellationToken cancellationToken = default)
        {
            var results = await _bookmarkFinder.FindBookmarksAsync(activityType, bookmark, tenantId, cancellationToken).ToList();
            await ResumeWorkflowsAsync(results, input, correlationId, contextId, cancellationToken);
        }

        public async Task ResumeWorkflowsAsync(IEnumerable<BookmarkFinderResult> results, object? input = default, string? correlationId = default, string? contextId = default, CancellationToken cancellationToken = default)
        {
            foreach (var result in results)
            {
                var workflowInstance = await _workflowInstanceStore.FindByIdAsync(result.WorkflowInstanceId, cancellationToken);

                if (workflowInstance?.WorkflowStatus == WorkflowStatus.Suspended)
                    await ResumeWorkflowAsync(workflowInstance!, result.ActivityId, input, cancellationToken);
            }
        }

        public async Task<WorkflowInstance> BuildAndResumeWorkflowAsync<T>(WorkflowInstance workflowInstance, string? activityId = default, object? input = default, CancellationToken cancellationToken = default) where T : IWorkflow
        {
            var workflowBlueprint = _workflowBuilderFactory().Build<T>();
            return await _workflowRunner.RunWorkflowAsync(workflowBlueprint, workflowInstance, activityId, input, cancellationToken);
        }

        public async Task<WorkflowInstance> BuildAndResumeWorkflowAsync(IWorkflow workflow, WorkflowInstance workflowInstance, string? activityId = default, object? input = default, CancellationToken cancellationToken = default)
        {
            var workflowBlueprint = _workflowBuilderFactory().Build(workflow);
            return await _workflowRunner.RunWorkflowAsync(workflowBlueprint, workflowInstance, activityId, input, cancellationToken);
        }

        public async Task<WorkflowInstance> ResumeWorkflowAsync(WorkflowInstance workflowInstance, string? activityId = default, object? input = default, CancellationToken cancellationToken = default)
        {
            var workflowBlueprint = await _workflowRegistry.GetAsync(
                workflowInstance.DefinitionId,
                workflowInstance.TenantId,
                VersionOptions.SpecificVersion(workflowInstance.Version),
                cancellationToken);

            if (workflowBlueprint == null)
                throw new WorkflowException($"Workflow instance {workflowInstance.Id} references workflow definition {workflowInstance.DefinitionId} version {workflowInstance.Version}, but no such workflow definition was found.");

            return await _workflowRunner.RunWorkflowAsync(workflowBlueprint, workflowInstance, activityId, input, cancellationToken);
        }
    }
}