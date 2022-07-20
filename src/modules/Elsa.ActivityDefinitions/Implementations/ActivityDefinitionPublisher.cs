using System.Text.Json;
using Elsa.ActivityDefinitions.Entities;
using Elsa.ActivityDefinitions.Notifications;
using Elsa.ActivityDefinitions.Services;
using Elsa.Mediator.Services;
using Elsa.Persistence.Common.Models;
using Elsa.Workflows.Core.Activities.Flowchart.Activities;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.Services;

namespace Elsa.ActivityDefinitions.Implementations
{
    public class ActivityDefinitionPublisher : IActivityDefinitionPublisher
    {
        private readonly IActivityDefinitionStore _activityDefinitionStore;
        private readonly IEventPublisher _eventPublisher;
        private readonly IIdentityGenerator _identityGenerator;
        private readonly SerializerOptionsProvider _serializerOptionsProvider;
        private readonly ISystemClock _systemClock;

        public ActivityDefinitionPublisher(
            IActivityDefinitionStore activityDefinitionStore, 
            IEventPublisher eventPublisher,
            IIdentityGenerator identityGenerator, 
            SerializerOptionsProvider serializerOptionsProvider, 
            ISystemClock systemClock)
        {
            _activityDefinitionStore = activityDefinitionStore;
            _eventPublisher = eventPublisher;
            _identityGenerator = identityGenerator;
            _serializerOptionsProvider = serializerOptionsProvider;
            _systemClock = systemClock;
        }

        public ActivityDefinition New()
        {
            var id = _identityGenerator.GenerateId();
            var definitionId = _identityGenerator.GenerateId();
            const int version = 1;

            return new ActivityDefinition
            {
                Id = id,
                DefinitionId = definitionId,
                Version = version,
                IsLatest = true,
                IsPublished = false,
                CreatedAt = _systemClock.UtcNow,
                Data = JsonSerializer.Serialize(new Flowchart(), _serializerOptionsProvider.CreateDefaultOptions()),
            };
        }

        public async Task<ActivityDefinition?> PublishAsync(string definitionId, CancellationToken cancellationToken = default)
        {
            var definition = await _activityDefinitionStore.FindByDefinitionIdAsync(definitionId, VersionOptions.Latest, cancellationToken);

            if (definition == null)
                return null;

            return await PublishAsync(definition, cancellationToken);
        }

        public async Task<ActivityDefinition> PublishAsync(ActivityDefinition definition, CancellationToken cancellationToken = default)
        {
            var definitionId = definition.DefinitionId;

            // Reset current latest and published definitions.
            var publishedAndOrLatestWorkflows = await _activityDefinitionStore.FindLatestAndPublishedByDefinitionIdAsync(definitionId, cancellationToken);

            foreach (var publishedAndOrLatestWorkflow in publishedAndOrLatestWorkflows)
            {
                publishedAndOrLatestWorkflow.IsPublished = false;
                publishedAndOrLatestWorkflow.IsLatest = false;
                await _activityDefinitionStore.SaveAsync(publishedAndOrLatestWorkflow, cancellationToken);
            }

            if (definition.IsPublished)
                definition.Version++;
            else
                definition.IsPublished = true;

            definition.IsLatest = true;
            definition = Initialize(definition);

            await _eventPublisher.PublishAsync(new ActivityDefinitionPublishing(definition), cancellationToken);
            await _activityDefinitionStore.SaveAsync(definition, cancellationToken);
            await _eventPublisher.PublishAsync(new ActivityDefinitionPublished(definition), cancellationToken);
            return definition;
        }

        public async Task<ActivityDefinition?> RetractAsync(string definitionId, CancellationToken cancellationToken = default)
        {
            var definition = await _activityDefinitionStore.FindByDefinitionIdAsync(definitionId, VersionOptions.Published, cancellationToken);

            if (definition == null)
                return null;

            return await RetractAsync(definition, cancellationToken);
        }

        public async Task<ActivityDefinition> RetractAsync(ActivityDefinition definition, CancellationToken cancellationToken = default)
        {
            if (!definition.IsPublished)
                throw new InvalidOperationException("Cannot retract an unpublished workflow definition.");

            definition.IsPublished = false;
            definition = Initialize(definition);

            await _eventPublisher.PublishAsync(new ActivityDefinitionRetracting(definition), cancellationToken);
            await _activityDefinitionStore.SaveAsync(definition, cancellationToken);
            await _eventPublisher.PublishAsync(new ActivityDefinitionRetracted(definition), cancellationToken);
            return definition;
        }

        public async Task<ActivityDefinition?> GetDraftAsync(string definitionId, CancellationToken cancellationToken = default)
        {
            var definition = await _activityDefinitionStore.FindByDefinitionIdAsync(definitionId, VersionOptions.Latest, cancellationToken);

            if (definition == null)
                return null;

            if (!definition.IsPublished)
                return definition;

            var draft = definition.ShallowClone();

            draft.Version++;
            draft.Id = _identityGenerator.GenerateId();
            draft.IsLatest = true;

            return draft;
        }

        public async Task<ActivityDefinition> SaveDraftAsync(ActivityDefinition definition, CancellationToken cancellationToken = default)
        {
            var draft = definition;
            var definitionId = definition.DefinitionId;
            var latestVersion = await _activityDefinitionStore.FindByDefinitionIdAsync(definitionId, VersionOptions.Latest, cancellationToken);

            if (latestVersion is { IsPublished: true, IsLatest: true })
            {
                latestVersion.IsLatest = false;
                await _activityDefinitionStore.SaveAsync(latestVersion, cancellationToken);
            }

            draft.IsLatest = true;
            draft.IsPublished = false;
            draft = Initialize(draft);

            await _activityDefinitionStore.SaveAsync(draft, cancellationToken);
            return draft;
        }

        private ActivityDefinition Initialize(ActivityDefinition definition)
        {
            if (definition.Id == null!)
                definition.Id = _identityGenerator.GenerateId();

            if (definition.DefinitionId == null!)
                definition.DefinitionId = _identityGenerator.GenerateId();

            if (definition.Version == 0)
                definition.Version = 1;

            return definition;
        }
    }
}