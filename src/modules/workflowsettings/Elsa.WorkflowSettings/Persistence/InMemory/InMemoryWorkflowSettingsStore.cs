using Elsa.Persistence.InMemory;
using Elsa.Services;
using Elsa.WorkflowSettings.Abstractions.Persistence;
using Elsa.WorkflowSettings.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Elsa.WorkflowSettings.Persistence.InMemory
{
    public class InMemoryWorkflowSettingsStore : InMemoryStore<WorkflowSetting>, IWorkflowSettingsStore
    {
        public InMemoryWorkflowSettingsStore(IMemoryCache memoryCache, IIdGenerator idGenerator) : base(memoryCache, idGenerator)
        {
        }
    }
}
