using System;
using Elsa.Persistence;
using Elsa.Persistence.Memory;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa
{
    public class ElsaOptions
    {
        public ElsaOptions(IServiceCollection services)
        {
            Services = services;
            
            WorkflowDefinitionStoreFactory = sp => new MemoryWorkflowDefinitionStore();
        }
        
        public IServiceCollection Services { get; }
        internal Func<IServiceProvider, IWorkflowDefinitionStore> WorkflowDefinitionStoreFactory { get; private set; }
        internal Func<IServiceProvider, IWorkflowInstanceStore> WorkflowInstanceStoreFactory { get; private set; }
        internal Func<IServiceProvider, IDistributedLockProvider> DistributedLockProviderFactory { get; private set; }

        public ElsaOptions UseWorkflowDefinitionStore(Func<IServiceProvider, IWorkflowDefinitionStore> factory)
        {
            WorkflowDefinitionStoreFactory = factory;
            return this;
        }
        
        public ElsaOptions UseWorkflowInstanceStore(Func<IServiceProvider, IWorkflowInstanceStore> factory)
        {
            WorkflowInstanceStoreFactory = factory;
            return this;
        }

        public ElsaOptions UseDistributedLockProvider(Func<IServiceProvider, IDistributedLockProvider> factory)
        {
            DistributedLockProviderFactory = factory;
            return this;
        }
    }
}