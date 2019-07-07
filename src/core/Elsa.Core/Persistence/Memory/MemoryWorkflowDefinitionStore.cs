using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence;

namespace Elsa.Core.Persistence.Memory
{
    public class MemoryWorkflowDefinitionStore : IWorkflowDefinitionStore
    {
        private readonly IDictionary<string, WorkflowDefinition> definitions;

        public MemoryWorkflowDefinitionStore()
        {
            definitions = new Dictionary<string, WorkflowDefinition>();
        }
        
        public Task AddAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
        {
            definitions[definition.Id] = definition;
            return Task.CompletedTask;
        }

        public Task<WorkflowDefinition> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var definition = definitions.ContainsKey(id) ? definitions[id] : default;
            return Task.FromResult(definition);
        }
    }
}