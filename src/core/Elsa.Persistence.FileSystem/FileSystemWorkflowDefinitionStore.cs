using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Models;

namespace Elsa.Persistence.FileSystem
{
    public class FileSystemWorkflowDefinitionStore : IWorkflowDefinitionStore
    {
        private const string Directory = "definitions";
        private readonly IFileSystemWorkflowProvider provider;

        public FileSystemWorkflowDefinitionStore(IFileSystemWorkflowProvider provider)
        {
            this.provider = provider;
        }
        
        public async Task<IEnumerable<Workflow>> ListAllAsync(CancellationToken cancellationToken)
        {
            return await provider.ListAsync(Directory, cancellationToken);
        }

        public async Task SaveAsync(Workflow workflow, CancellationToken cancellationToken)
        {
            await provider.SaveAsync(Directory, workflow, cancellationToken);
        }

        public async Task<IEnumerable<Tuple<Workflow, IActivity>>> ListByStartActivityAsync(string activityType, CancellationToken cancellationToken)
        {
            var workflows = await ListAllAsync(cancellationToken);
            var query = 
                from workflow in workflows
                from activity in workflow.GetStartActivities()
                where activity.Name == activityType
                select Tuple.Create(workflow, activity);

            return query.Distinct();
        }

        public async Task<Workflow> GetByIdAsync(string id, CancellationToken cancellationToken)
        {
            var workflows = await ListAllAsync(cancellationToken);
            return workflows.FirstOrDefault(x => x.Id == id);
        }
    }
}