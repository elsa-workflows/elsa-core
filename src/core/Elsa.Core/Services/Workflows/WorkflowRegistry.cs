using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Models;
using Elsa.Providers.Workflows;
using Elsa.Services.Models;
using MediatR;

namespace Elsa.Services.Workflows
{
    public class WorkflowRegistry : IWorkflowRegistry
    {
        private readonly ListWorkflowProvider _listWorkflowProvider = new();
        private readonly ICollection<IWorkflowProvider> _workflowProviders;
        private readonly IMediator _mediator;

        public WorkflowRegistry(IEnumerable<IWorkflowProvider> workflowProviders, IMediator mediator)
        {
            _workflowProviders = workflowProviders.Append(_listWorkflowProvider).ToList();
            _mediator = mediator;
        }

        public async Task<IWorkflowBlueprint?> FindAsync(string definitionId, VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default) =>
            await FindInternalAsync(provider => provider.FindAsync(definitionId, versionOptions, tenantId, cancellationToken), cancellationToken);

        public async Task<IWorkflowBlueprint?> FindByDefinitionVersionIdAsync(string definitionVersionId, string? tenantId = default, CancellationToken cancellationToken = default) =>
            await FindInternalAsync(provider => provider.FindByDefinitionVersionIdAsync(definitionVersionId, tenantId, cancellationToken), cancellationToken);

        public async Task<IWorkflowBlueprint?> FindByNameAsync(string name, VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default) =>
            await FindInternalAsync(provider => provider.FindByNameAsync(name, versionOptions, tenantId, cancellationToken), cancellationToken);

        public async Task<IWorkflowBlueprint?> FindByTagAsync(string tag, VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default) =>
            await FindInternalAsync(provider => provider.FindByTagAsync(tag, versionOptions, tenantId, cancellationToken), cancellationToken);

        public async Task<IEnumerable<IWorkflowBlueprint>> FindManyByTagAsync(string tag, VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default) =>
            await FindManyInternalAsync(provider => provider.FindManyByTagAsync(tag, versionOptions, tenantId, cancellationToken), cancellationToken).ToListAsync(cancellationToken);

        public async Task<IEnumerable<IWorkflowBlueprint>> FindManyByDefinitionIds(IEnumerable<string> definitionIds, VersionOptions versionOptions, CancellationToken cancellationToken) =>
            await FindManyInternalAsync(provider => provider.FindManyByDefinitionIds(definitionIds, versionOptions, cancellationToken), cancellationToken).ToListAsync(cancellationToken);
        
        public async Task<IEnumerable<IWorkflowBlueprint>> FindManyByDefinitionVersionIds(IEnumerable<string> definitionVersionIds, CancellationToken cancellationToken) =>
            await FindManyInternalAsync(provider => provider.FindManyByDefinitionVersionIds(definitionVersionIds, cancellationToken), cancellationToken).ToListAsync(cancellationToken);

        public async Task<IEnumerable<IWorkflowBlueprint>> FindManyByNames(IEnumerable<string> names, CancellationToken cancellationToken = default) =>
            await FindManyInternalAsync(provider => provider.FindManyByNames(names, cancellationToken), cancellationToken).ToListAsync(cancellationToken);

        public void Add(IWorkflowBlueprint workflowBlueprint) => _listWorkflowProvider.Add(workflowBlueprint);

        private async Task<IWorkflowBlueprint?> FindInternalAsync(Func<IWorkflowProvider, ValueTask<IWorkflowBlueprint?>> providerAction, CancellationToken cancellationToken = default)
        {
            var providers = _workflowProviders;

            foreach (var provider in providers)
            {
                var workflow = await providerAction(provider);

                if (workflow == null)
                    continue;

                await _mediator.Publish(new WorkflowBlueprintLoaded(workflow), cancellationToken);
                return workflow;
            }

            return null;
        }

        private async IAsyncEnumerable<IWorkflowBlueprint> FindManyInternalAsync(
            Func<IWorkflowProvider, ValueTask<IEnumerable<IWorkflowBlueprint>>> providerAction,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var providers = _workflowProviders;

            foreach (var provider in providers)
            {
                var workflows = await providerAction(provider);

                foreach (var workflow in workflows)
                {
                    await _mediator.Publish(new WorkflowBlueprintLoaded(workflow), cancellationToken);
                    yield return workflow;
                }
            }
        }
    }
}