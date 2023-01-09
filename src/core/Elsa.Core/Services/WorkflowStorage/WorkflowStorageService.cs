using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Options;
using Elsa.Providers.WorkflowStorage;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Services.WorkflowStorage
{
    public class WorkflowStorageService : IWorkflowStorageService
    {
        private readonly IWorkflowStorageProvider _defaultStorageProvider;
        private readonly IDictionary<string, IWorkflowStorageProvider> _providersLookup;

        public WorkflowStorageService(IEnumerable<IWorkflowStorageProvider> providers, ElsaOptions elsaOptions, IServiceProvider serviceProvider)
        {
            _defaultStorageProvider = (IWorkflowStorageProvider?)ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, elsaOptions.DefaultWorkflowStorageProviderType)!;
            _providersLookup = providers.ToDictionary(x => x.Name);
        }

        public IWorkflowStorageProvider GetProviderByNameOrDefault(string? providerName = default) => providerName != null ? _providersLookup.GetItem(providerName) ?? _defaultStorageProvider : _defaultStorageProvider;
        public IEnumerable<IWorkflowStorageProvider> ListProviders() => _providersLookup.Values;

        public async ValueTask SaveAsync(string? providerName, WorkflowStorageContext context, string key, object? value, CancellationToken cancellationToken = default)
        {
            var provider = GetProviderByNameOrDefault(providerName);
            await provider.SaveAsync(context, key, value, cancellationToken);
        }

        public async ValueTask<object?> LoadAsync(string? providerName, WorkflowStorageContext context, string key, CancellationToken cancellationToken = default)
        {
            var provider = GetProviderByNameOrDefault(providerName);
            return await provider.LoadAsync(context, key, cancellationToken);
        }

        public async ValueTask DeleteAsync(string? providerName, WorkflowStorageContext context, string key, CancellationToken cancellationToken = default)
        {
            var provider = GetProviderByNameOrDefault(providerName);
            await provider.DeleteAsync(context, key, cancellationToken);
        }

        public async ValueTask DeleteAsync(string? providerName, WorkflowStorageContext context, CancellationToken cancellationToken = default)
        {
            var provider = GetProviderByNameOrDefault(providerName);
            await provider.DeleteAsync(context, cancellationToken);
        }

        public async ValueTask<WorkflowInputReference> SaveAsync(WorkflowInput input, WorkflowInstance workflowInstance, CancellationToken cancellationToken = default)
        {
            var workflowStorageContext = new WorkflowStorageContext(workflowInstance, workflowInstance.DefinitionId);
            var inputStorageProvider = GetProviderByNameOrDefault(input.StorageProviderName);
            await inputStorageProvider.SaveAsync(workflowStorageContext, nameof(WorkflowInstance.Input), input.Input, cancellationToken);
            return new WorkflowInputReference(inputStorageProvider.Name);
        }

        public async ValueTask<object?> LoadAsync(WorkflowInstance workflowInstance, CancellationToken cancellationToken = default)
        {
            var inputReference = workflowInstance.Input;

            if (inputReference == null)
                return null;
            
            var context = new WorkflowStorageContext(workflowInstance, workflowInstance.DefinitionId);
            return await LoadAsync(inputReference.ProviderName, context, nameof(WorkflowInstance.Input), cancellationToken);
        }
    }
}