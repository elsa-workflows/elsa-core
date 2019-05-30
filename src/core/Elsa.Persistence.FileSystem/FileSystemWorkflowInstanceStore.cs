using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Persistence.FileSystem
{
    public class FileSystemWorkflowInstanceStore : IWorkflowInstanceStore
    {
        private const string Directory = "instances";
        private readonly IFileSystemWorkflowProvider provider;

        public FileSystemWorkflowInstanceStore(IFileSystemWorkflowProvider provider)
        {
            this.provider = provider;
        }
        
        public async Task SaveAsync(Workflow workflow, CancellationToken cancellationToken)
        {
            await provider.SaveAsync(Directory, workflow, cancellationToken);
        }
        
        public async Task<IEnumerable<Workflow>> ListAllAsync(CancellationToken cancellationToken)
        {
            return await provider.ListAsync(Directory, cancellationToken);
        }

        public async Task<IEnumerable<Tuple<Workflow, IActivity>>> ListByBlockingActivityAsync(string workflowType, CancellationToken cancellationToken)
        {
            var workflows = await provider.ListAsync(Directory, cancellationToken);
            var query =
                from workflow in workflows
                from activity in workflow.BlockingActivities
                where activity.Name == workflowType
                select Tuple.Create(workflow, activity);

            return query.Distinct();
        }
    }
}