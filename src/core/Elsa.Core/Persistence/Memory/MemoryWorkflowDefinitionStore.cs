using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Persistence.Memory
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

        public Task<WorkflowDefinition> GetByIdAsync(string id, VersionOptions version, CancellationToken cancellationToken = default)
        {
            var query = definitions.Values.Where(x => x.Id == id).AsQueryable();

            if (version.IsDraft)
                query = query.Where(x => !x.IsPublished).OrderByDescending(x => x.Version);
            else if(version.IsLatest)
                query = query.OrderByDescending(x => x.Version);
            else if(version.IsPublished)
                query = query.Where(x => x.IsPublished).OrderByDescending(x => x.Version);
            else if(version.Version > 0)
                query = query.Where(x => x.Version == version.Version);
            
            var definition = query.FirstOrDefault();
            return Task.FromResult(definition);
        }
    }
}