using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Persistence.FileSystem
{
    public abstract class FileSystemWorkflowStoreBase : IWorkflowStore
    {
        protected string Directory { get; }
        protected IFileSystemWorkflowProvider Provider { get; }

        protected FileSystemWorkflowStoreBase(string directory, IFileSystemWorkflowProvider provider)
        {
            Directory = directory;
            Provider = provider;
        }
        
        public async Task SaveAsync(Workflow workflow, CancellationToken cancellationToken)
        {
            await Provider.SaveAsync(Directory, workflow, cancellationToken);
        }

        public async Task<Workflow> GetByIdAsync(string id, CancellationToken cancellationToken)
        {
            var workflows = await ListAllAsync(cancellationToken);
            return workflows.FirstOrDefault(x => x.Id == id);
        }

        public Task<IEnumerable<Workflow>> ListAllAsync(CancellationToken cancellationToken)
        {
            return ListAllAsync(null, cancellationToken);
        }
        
        public async Task<IEnumerable<Workflow>> ListAllAsync(string parentId, CancellationToken cancellationToken)
        {
            var workflows = await Provider.ListAsync(Directory, cancellationToken);

            if (parentId != null)
                workflows = workflows.Where(x => x.ParentId == parentId);

            return workflows;
        }
    }
}