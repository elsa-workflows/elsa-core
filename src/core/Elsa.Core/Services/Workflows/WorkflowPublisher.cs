using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.WorkflowDefinitions;
using Elsa.Persistence.Specifications.WorkflowInstances;
using MediatR;
using NodaTime;
using Open.Linq.AsyncExtensions;
using WorkflowDefinitionIdSpecification = Elsa.Persistence.Specifications.WorkflowInstances.WorkflowDefinitionIdSpecification;
using WorkflowDefinitionIdSpecification2 = Elsa.Persistence.Specifications.WorkflowDefinitions.WorkflowDefinitionIdSpecification;

namespace Elsa.Services.Workflows
{
    public class WorkflowPublisher : IWorkflowPublisher
    {
        private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IIdGenerator _idGenerator;
        private readonly ICloner _cloner;
        private readonly IClock _clock;
        private readonly IMediator _mediator;

        public WorkflowPublisher(
            IWorkflowDefinitionStore workflowDefinitionStore,
            IWorkflowInstanceStore workflowInstanceStore,
            IIdGenerator idGenerator,
            ICloner cloner,
            IClock clock,
            IMediator mediator)
        {
            _workflowDefinitionStore = workflowDefinitionStore;
            _workflowInstanceStore = workflowInstanceStore;
            _idGenerator = idGenerator;
            _cloner = cloner;
            _clock = clock;
            _mediator = mediator;
        }

        public WorkflowDefinition New()
        {
            var definition = new WorkflowDefinition
            {
                Id = _idGenerator.Generate(),
                DefinitionId = _idGenerator.Generate(),
                Name = "New Workflow",
                Version = 1,
                IsLatest = true,
                IsPublished = false,
                IsSingleton = false,
                CreatedAt = _clock.GetCurrentInstant()
            };

            return definition;
        }

        public async Task<WorkflowDefinition?> PublishAsync(string workflowDefinitionId, CancellationToken cancellationToken = default)
        {
            var definition = await _workflowDefinitionStore.FindByDefinitionIdAsync(
                workflowDefinitionId,
                VersionOptions.Latest,
                cancellationToken);

            if (definition == null)
                return null;

            return await PublishAsync(definition, cancellationToken);
        }

        public async Task<WorkflowDefinition> PublishAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default)
        {
            var definitionId = workflowDefinition.DefinitionId;

            // Reset current latest and published definitions.
            var publishedAndOrLatestDefinitions = await _workflowDefinitionStore.FindManyAsync(new LatestOrPublishedWorkflowDefinitionIdSpecification(definitionId), cancellationToken: cancellationToken).ToList();

            foreach (var publishedAndOrLatestDefinition in publishedAndOrLatestDefinitions)
            {
                publishedAndOrLatestDefinition.IsPublished = false;
                publishedAndOrLatestDefinition.IsLatest = false;
                await _workflowDefinitionStore.SaveAsync(publishedAndOrLatestDefinition, cancellationToken);
            }

            if (workflowDefinition.IsPublished)
                workflowDefinition.Version++;
            else
                workflowDefinition.IsPublished = true;

            workflowDefinition.IsLatest = true;
            workflowDefinition = Initialize(workflowDefinition);

            await _mediator.Publish(new WorkflowDefinitionPublishing(workflowDefinition), cancellationToken);
            await _workflowDefinitionStore.SaveAsync(workflowDefinition, cancellationToken);
            await _mediator.Publish(new WorkflowDefinitionPublished(workflowDefinition), cancellationToken);
            return workflowDefinition;
        }

        public async Task<WorkflowDefinition?> RetractAsync(string workflowDefinitionId, CancellationToken cancellationToken = default)
        {
            var definition = await _workflowDefinitionStore.FindByDefinitionIdAsync(
                workflowDefinitionId,
                VersionOptions.Published,
                cancellationToken);

            if (definition == null)
                return null;

            return await RetractAsync(definition, cancellationToken);
        }

        public async Task<WorkflowDefinition> RetractAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default)
        {
            if (!workflowDefinition.IsPublished)
                throw new InvalidOperationException("Cannot unpublish an unpublished workflow definition.");

            workflowDefinition.IsPublished = false;
            workflowDefinition = Initialize(workflowDefinition);

            await _mediator.Publish(new WorkflowDefinitionRetracting(workflowDefinition), cancellationToken);
            await _workflowDefinitionStore.SaveAsync(workflowDefinition, cancellationToken);
            await _mediator.Publish(new WorkflowDefinitionRetracted(workflowDefinition), cancellationToken);
            return workflowDefinition;
        }

        public async Task<WorkflowDefinition?> GetDraftAsync(string workflowDefinitionId, VersionOptions? versionOptions = default, CancellationToken cancellationToken = default)
        {
            var definition = await _workflowDefinitionStore.FindByDefinitionIdAsync(
                workflowDefinitionId,
                versionOptions ?? VersionOptions.Latest,
                cancellationToken);

            if (definition == null)
                return null;

            if (definition.IsLatest && !definition.IsPublished)
                return definition;

            var latest = definition.IsLatest ? definition : (await _workflowDefinitionStore.FindByDefinitionIdAsync(workflowDefinitionId, VersionOptions.Latest, cancellationToken));

            if (latest == null)
            {
                latest = (await _workflowDefinitionStore.FindManyAsync(new Persistence.Specifications.WorkflowDefinitions.WorkflowDefinitionIdSpecification(workflowDefinitionId, VersionOptions.All),
                    new OrderBy<WorkflowDefinition>(x => x.Version, SortDirection.Descending), new Paging(0, 1), cancellationToken)).FirstOrDefault();

                if(latest != null)
                {
                    latest.IsLatest = true;
                    return latest;
                }
            }

            var draft = _cloner.Clone(definition);

            draft.Id = _idGenerator.Generate();
            draft.IsPublished = false;
            draft.IsLatest = true;
            draft.CreatedAt = _clock.GetCurrentInstant();
            draft.Version = latest != null ? latest.Version + 1 : 1;

            return draft;
        }

        public async Task<WorkflowDefinition> SaveDraftAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default)
        {
            var draft = workflowDefinition;

            var latestVersion = await _workflowDefinitionStore.FindByDefinitionIdAsync(
                workflowDefinition.DefinitionId,
                VersionOptions.Latest,
                cancellationToken);

            if (latestVersion != null)
            {
                latestVersion.IsLatest = false;
                await _workflowDefinitionStore.SaveAsync(latestVersion, cancellationToken);
            }

            draft.IsLatest = true;
            draft.IsPublished = false;
            draft = Initialize(draft);

            await _workflowDefinitionStore.SaveAsync(draft, cancellationToken);
            return draft;
        }

        public async Task DeleteAsync(string workflowDefinitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
        {
            if (versionOptions.AllVersions)
            {
                await _workflowInstanceStore.DeleteManyAsync(new WorkflowDefinitionIdSpecification(workflowDefinitionId), cancellationToken);
                await _workflowDefinitionStore.DeleteManyAsync(new Persistence.Specifications.WorkflowDefinitions.WorkflowDefinitionIdSpecification(workflowDefinitionId), cancellationToken);
            }
            else
            {
                var definition = await _workflowDefinitionStore.FindByDefinitionIdAsync(workflowDefinitionId, versionOptions, cancellationToken);

                if (definition != null)
                {
                    if (definition.IsLatest)
                    {
                        var otherVersions = await _workflowDefinitionStore.FindManyAsync(
                                                                              new WorkflowDefinitionIdSpecification2(workflowDefinitionId, VersionOptions.All),
                                                                              new OrderBy<WorkflowDefinition>(x => x.CreatedAt, SortDirection.Descending),
                                                                              cancellationToken: cancellationToken)
                                                                          .Where(d => d.Version != definition.Version);

                        var newLatest = otherVersions.FirstOrDefault(d => d.IsPublished) ?? otherVersions.FirstOrDefault();

                        if (newLatest != null)
                        {
                            newLatest.IsLatest = true;

                            await _workflowDefinitionStore.SaveAsync(newLatest, cancellationToken);
                        }
                    }

                    await DeleteAsync(definition, cancellationToken);
                }
            }
        }

        public async Task DeleteAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default)
        {
            await _workflowInstanceStore.DeleteManyAsync(new WorkflowDefinitionVersionIdsSpecification(new[] { workflowDefinition.VersionId }), cancellationToken);
            await _workflowDefinitionStore.DeleteAsync(workflowDefinition, cancellationToken);
        }

        private WorkflowDefinition Initialize(WorkflowDefinition workflowDefinition)
        {
            if (workflowDefinition.Id == null!)
                workflowDefinition.Id = _idGenerator.Generate();

            if (workflowDefinition.Version == 0)
                workflowDefinition.Version = 1;

            if (workflowDefinition.DefinitionId == null!)
                workflowDefinition.DefinitionId = _idGenerator.Generate();

            if (workflowDefinition.CreatedAt == Instant.MinValue || workflowDefinition.CreatedAt == Instant.FromDateTimeOffset(DateTimeOffset.UnixEpoch))
                workflowDefinition.CreatedAt = _clock.GetCurrentInstant();

            return workflowDefinition;
        }
    }
}