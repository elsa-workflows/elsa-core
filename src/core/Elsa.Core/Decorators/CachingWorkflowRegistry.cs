using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Caching;
using Elsa.Events;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace Elsa.Decorators
{
    public class CachingWorkflowRegistry : IWorkflowRegistry, INotificationHandler<WorkflowDefinitionSaved>, INotificationHandler<WorkflowDefinitionDeleted>
    {
        public const string RootKey = "WorkflowRegistry";
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IMemoryCache _memoryCache;
        private readonly ICacheSignal _cacheSignal;

        public CachingWorkflowRegistry(IWorkflowRegistry workflowRegistry, IMemoryCache memoryCache, ICacheSignal cacheSignal)
        {
            _workflowRegistry = workflowRegistry;
            _memoryCache = memoryCache;
            _cacheSignal = cacheSignal;
        }

        public async Task<IWorkflowBlueprint?> FindAsync(string definitionId, VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"{RootKey}:definition:id:{definitionId}:{versionOptions}:{tenantId}";
            return await FindInternalAsync(cacheKey, () => _workflowRegistry.FindAsync(definitionId, versionOptions, tenantId, cancellationToken), cancellationToken);
        }

        public async Task<IWorkflowBlueprint?> FindByDefinitionVersionIdAsync(string definitionVersionId, string? tenantId = default, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"{RootKey}:definition-version:id:{definitionVersionId}:{tenantId}";
            return await FindInternalAsync(cacheKey, () => _workflowRegistry.FindByDefinitionVersionIdAsync(definitionVersionId, tenantId, cancellationToken), cancellationToken);
        }

        public async Task<IWorkflowBlueprint?> FindByNameAsync(string name, VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"{RootKey}:definition:name:{name}:{versionOptions}:{tenantId}";
            return await FindInternalAsync(cacheKey, () => _workflowRegistry.FindByNameAsync(name, versionOptions, tenantId, cancellationToken), cancellationToken);
        }

        public async Task<IWorkflowBlueprint?> FindByTagAsync(string tag, VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"{RootKey}:definition:name:{tag}:{versionOptions}:{tenantId}";
            return await FindInternalAsync(cacheKey, () => _workflowRegistry.FindByTagAsync(tag, versionOptions, tenantId, cancellationToken), cancellationToken);
        }

        public async Task<IEnumerable<IWorkflowBlueprint>> FindManyByTagAsync(string tag, VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default)
        {
            // TODO: Maybe cache this as well?
            return await _workflowRegistry.FindManyByTagAsync(tag, versionOptions, tenantId, cancellationToken);
        }

        public async Task<IEnumerable<IWorkflowBlueprint>> FindManyByDefinitionIds(IEnumerable<string> definitionIds, VersionOptions versionOptions, CancellationToken cancellationToken)
        {
            // TODO: Maybe cache this as well?
            return await _workflowRegistry.FindManyByDefinitionIds(definitionIds, versionOptions, cancellationToken);
        }

        public async Task<IEnumerable<IWorkflowBlueprint>> FindManyByDefinitionVersionIds(IEnumerable<string> definitionVersionIds, CancellationToken cancellationToken)
        {
            // TODO: Maybe cache this as well?
            return await _workflowRegistry.FindManyByDefinitionVersionIds(definitionVersionIds, cancellationToken);
        }

        public async Task<IEnumerable<IWorkflowBlueprint>> FindManyByNames(IEnumerable<string> names, CancellationToken cancellationToken = default)
        {
            // TODO: Maybe cache this as well?
            return await _workflowRegistry.FindManyByNames(names, cancellationToken);
        }

        public void Add(IWorkflowBlueprint workflowBlueprint) => _workflowRegistry.Add(workflowBlueprint);

        public async Task<IWorkflowBlueprint?> FindInternalAsync(string cacheKey, Func<Task<IWorkflowBlueprint?>> findAction, CancellationToken cancellationToken = default)
        {
            return await _memoryCache.GetOrCreateAsync(cacheKey, async entry =>
            {
                var workflowBlueprint = await findAction();

                if (workflowBlueprint != null)
                {
                    var key = GetEvictionKey(workflowBlueprint.Id);
                    entry.Monitor(_cacheSignal.GetToken(key));
                }

                return workflowBlueprint;
            });
        }

        private string GetEvictionKey(string definitionId) => $"{RootKey}:definition:id:{definitionId}";

        Task INotificationHandler<WorkflowDefinitionSaved>.Handle(WorkflowDefinitionSaved notification, CancellationToken cancellationToken)
        {
            _cacheSignal.TriggerTokenAsync(GetEvictionKey(notification.WorkflowDefinition.DefinitionId));
            return Task.CompletedTask;
        }

        Task INotificationHandler<WorkflowDefinitionDeleted>.Handle(WorkflowDefinitionDeleted notification, CancellationToken cancellationToken)
        {
            _cacheSignal.TriggerTokenAsync(GetEvictionKey(notification.WorkflowDefinition.DefinitionId));
            return Task.CompletedTask;
        }
    }
}