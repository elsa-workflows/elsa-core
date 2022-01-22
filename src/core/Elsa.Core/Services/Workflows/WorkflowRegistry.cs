using System;
using System.Collections.Generic;
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
        private readonly IEnumerable<IWorkflowProvider> _workflowProviders;
        private readonly IMediator _mediator;

        public WorkflowRegistry(IEnumerable<IWorkflowProvider> workflowProviders, IMediator mediator)
        {
            _workflowProviders = workflowProviders;
            _mediator = mediator;
        }

        public async Task<IWorkflowBlueprint?> FindAsync(string definitionId, VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default) => 
            await FindInternalAsync(provider => provider.FindAsync(definitionId, versionOptions, tenantId, cancellationToken), cancellationToken);

        public async Task<IWorkflowBlueprint?> FindByNameAsync(string name, VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default) =>
            await FindInternalAsync(provider => provider.FindByNameAsync(name, versionOptions, tenantId, cancellationToken), cancellationToken);

        public async Task<IWorkflowBlueprint?> FindByTagAsync(string tag, VersionOptions versionOptions, string? tenantId = default, CancellationToken cancellationToken = default) =>
            await FindInternalAsync(provider => provider.FindByTagAsync(tag, versionOptions, tenantId, cancellationToken), cancellationToken);

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
    }
}