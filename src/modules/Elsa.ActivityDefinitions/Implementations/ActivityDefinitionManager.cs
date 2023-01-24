using Elsa.ActivityDefinitions.Entities;
using Elsa.ActivityDefinitions.Notifications;
using Elsa.ActivityDefinitions.Services;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Mediator.Services;
using Elsa.Workflows.Core.Services;

namespace Elsa.ActivityDefinitions.Implementations;

public class ActivityDefinitionManager : IActivityDefinitionManager
{
    private readonly IActivityDefinitionStore _store;
    private readonly IEventPublisher _eventPublisher;
    private readonly IActivityDefinitionPublisher _activityPublisher;
    private readonly IIdentityGenerator _identityGenerator;

    public ActivityDefinitionManager(
        IActivityDefinitionStore store, 
        IEventPublisher eventPublisher,
        IActivityDefinitionPublisher activityPublisher,
        IIdentityGenerator identityGenerator)
    {
        _store = store;
        _eventPublisher = eventPublisher;
        _activityPublisher = activityPublisher;
        _identityGenerator = identityGenerator;
    }

    public async Task<bool> DeleteVersionAsync(string definitionId, int versionToDelete, CancellationToken cancellationToken = default)
    {
        var workflows = (await _store.FindLatestAndPublishedByDefinitionIdAsync(definitionId, cancellationToken)).ToList();
        var latestVersion = workflows.WithVersion(VersionOptions.Latest).First();
        var publishedVersion = workflows.WithVersion(VersionOptions.Published).FirstOrDefault();

        if (versionToDelete == publishedVersion?.Version)
        {
            throw new Exception("Published version cannot be deleted");
        }
        
        var isDeleted = await _store.DeleteByDefinitionIdAndVersionAsync(definitionId, versionToDelete, cancellationToken) > 0;

        if (isDeleted)
        {
            await _eventPublisher.PublishAsync(new ActivityDefinitionVersionDeleted(definitionId, versionToDelete), cancellationToken);
            if (latestVersion.Version == versionToDelete)
            {
                var lastVersion = await _store.FindLastVersionByDefinitionIdAsync(definitionId, cancellationToken);
                if (lastVersion is not null)
                {
                    lastVersion.IsLatest = true;
                    await _store.SaveAsync(lastVersion, cancellationToken);
                }
            }
        }

        return isDeleted;
    }
    
    public async Task<ActivityDefinition> RevertVersionAsync(string definitionId, int version, CancellationToken cancellationToken = default)
    {
        var publishedAndLatestVersions = (await _store.FindLatestAndPublishedByDefinitionIdAsync(definitionId, cancellationToken)).ToList();
        if (publishedAndLatestVersions.Any(v => v.Version == version))
        {
            throw new Exception("Latest or published versions cannot be reverted");
        }
        
        var latestVersion = publishedAndLatestVersions.First(v => v.IsLatest);
        latestVersion.IsLatest = false;
        await _store.SaveAsync(latestVersion, cancellationToken);
        
        var draft = await _activityPublisher.GetDraftAsync(definitionId, version, cancellationToken);
        draft!.Id = _identityGenerator.GenerateId();
        draft.Version = latestVersion.Version + 1;
        draft.IsLatest = true;
        
        await _store.SaveAsync(draft, cancellationToken);
        return draft;
    }
}