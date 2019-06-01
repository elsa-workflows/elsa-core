using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;

namespace Elsa.Persistence.FileSystem
{
    public class FileSystemWorkflowInstanceStoreBase : FileSystemWorkflowStoreBase, IWorkflowInstanceStore
    {
        public FileSystemWorkflowInstanceStoreBase(IFileSystemWorkflowProvider provider) : base("instances", provider)
        {
        }
        
        public async Task<IEnumerable<Tuple<Workflow, IActivity>>> ListByBlockingActivityAsync(string workflowType, CancellationToken cancellationToken)
        {
            var workflows = await ListAllAsync(cancellationToken);
            var query =
                from workflow in workflows
                from activity in workflow.BlockingActivities
                where activity.Name == workflowType
                select Tuple.Create(workflow, activity);

            return query.Distinct();
        }
        
        public Task<IEnumerable<Workflow>> ListByStatusAsync(WorkflowStatus status, CancellationToken cancellationToken)
        {
            return ListByStatusAsync(null, status, cancellationToken);
        }

        public async Task<IEnumerable<Workflow>> ListByStatusAsync(string parentId, WorkflowStatus status, CancellationToken cancellationToken)
        {
            var workflows = await ListAllAsync(parentId, cancellationToken);
            return workflows.Where(x => x.Status == status);
        }
    }
}