using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Serialization.Models;

namespace Elsa.Persistence.FileSystem
{
    public abstract class FileSystemWorkflowStoreBase : IWorkflowDefinitionStore
    {
        protected string Directory { get; }

        protected FileSystemWorkflowStoreBase(string directory)
        {
            Directory = directory;
        }
        
        public Task SaveAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<WorkflowDefinition> GetByIdAsync(string id, CancellationToken cancellationToken)
        {
            var workflows = await ListAllAsync(cancellationToken);
            return workflows.FirstOrDefault(x => x.Id == id);
        }
        
        public Task<IEnumerable<WorkflowDefinition>> ListAllAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Tuple<WorkflowDefinition, ActivityDefinition>>> ListByStartActivityAsync(string activityType, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}