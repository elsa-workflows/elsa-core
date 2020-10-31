using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public class WorkflowContextManager : IWorkflowContextManager
    {
        private readonly IEnumerable<IWorkflowContextProvider> _providers;

        public WorkflowContextManager(IEnumerable<IWorkflowContextProvider> providers)
        {
            _providers = providers;
        }
        
        public async ValueTask<object?> LoadContext(LoadWorkflowContext context, CancellationToken cancellationToken = default)
        {
            var provider = GetProvider(context.ContextType);
            return provider == null ? null : await provider.LoadContextAsync(context, cancellationToken);
        }

        public async ValueTask<string?> SaveContextAsync(SaveWorkflowContext context, CancellationToken cancellationToken = default)
        {
            var provider = GetProvider(context.ContextType);
            return provider == null ? null : await provider.SaveContextAsync(context, cancellationToken);
        }
        
        public IWorkflowContextProvider? GetProvider(Type contextType)
        {
            var providers = _providers.Where(x => x.SupportedTypes.Contains(contextType)).ToList();

            if (!providers.Any())
                return null;
            
            if(providers.Count > 1)
                throw new Exception($"Multiple context providers found for type {contextType}");

            return providers.Single();
        }
    }
}