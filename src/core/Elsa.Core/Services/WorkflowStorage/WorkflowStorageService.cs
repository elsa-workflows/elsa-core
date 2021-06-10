using System.Collections.Generic;
using System.Linq;
using Elsa.Providers.WorkflowStorage;

namespace Elsa.Services.WorkflowStorage
{
    public class WorkflowStorageService : IWorkflowStorageService
    {
        private readonly TransientWorkflowStorageProvider _defaultStorageProvider;
        private readonly IDictionary<string, IWorkflowStorageProvider> _providersLookup;

        public WorkflowStorageService(IEnumerable<IWorkflowStorageProvider> providers, TransientWorkflowStorageProvider defaultStorageProvider)
        {
            _defaultStorageProvider = defaultStorageProvider;
            _providersLookup = providers.ToDictionary(x => x.Name);
        }

        public IWorkflowStorageProvider GetProviderByNameOrDefault(string? name = default) => name != null ? _providersLookup.GetItem(name) ?? _defaultStorageProvider : _defaultStorageProvider;
    }
}