using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Providers.WorkflowContext;
using Elsa.Services.Models;

namespace Elsa.Services.WorkflowContexts
{
    public class WorkflowContextManager : IWorkflowContextManager
    {
        private readonly ICollection<ILoadWorkflowContext> _refreshers;
        private readonly ICollection<ISaveWorkflowContext> _persisters;

        public WorkflowContextManager(IEnumerable<IWorkflowContextProvider> providers)
        {
            var list = providers.ToList();
            _refreshers = list.Where(x => x is ILoadWorkflowContext).Cast<ILoadWorkflowContext>().ToList();
            _persisters = list.Where(x => x is ISaveWorkflowContext).Cast<ISaveWorkflowContext>().ToList();
        }
        
        public async ValueTask<object?> LoadContext(LoadWorkflowContext context, CancellationToken cancellationToken = default)
        {
            var provider = context.ContextType != null ? GetRefresher(context.ContextType) : null;
            return provider == null ? null : await provider.LoadAsync(context, cancellationToken);
        }

        public async ValueTask<string?> SaveContextAsync(SaveWorkflowContext context, CancellationToken cancellationToken = default)
        {
            var provider = GetPersister(context.ContextType);
            return provider == null ? context.ContextId : await provider.SaveAsync(context, cancellationToken);
        }
        
        public ILoadWorkflowContext? GetRefresher(Type contextType) => GetProvider(contextType, _refreshers);
        public ISaveWorkflowContext? GetPersister(Type contextType) => GetProvider(contextType, _persisters);

        public TProvider GetProvider<TProvider>(Type contextType, IEnumerable<TProvider> providers) where TProvider:IWorkflowContextProvider
        {
            var supportedProviders = providers.Where(x => x.SupportedTypes.Contains(contextType)).ToList();

            if (!supportedProviders.Any())
                return default!;
            
            if(supportedProviders.Count > 1)
                throw new Exception($"Multiple providers found for type {contextType}");

            return supportedProviders.Single();
        }
    }
}