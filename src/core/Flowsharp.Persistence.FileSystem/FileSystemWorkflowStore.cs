using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Models;
using Flowsharp.Persistence.Specifications;

namespace Flowsharp.Persistence.FileSystem
{
    public class FileSystemWorkflowStore : IWorkflowStore
    {
        public FileSystemWorkflowStore()
        {
            Workflows = new List<Workflow>();
        }

        public IList<Workflow> Workflows { get; }
        
        public Task<IEnumerable<Workflow>> GetManyAsync(ISpecification<Workflow, IWorkflowSpecificationVisitor> specification, CancellationToken cancellationToken)
        {
            var query = Workflows.AsQueryable().Where(x => specification.IsSatisfiedBy(x));
            var matches = query.Distinct().ToList();
            return Task.FromResult<IEnumerable<Workflow>>(matches);
        }

        public Task<Workflow> GetAsync(ISpecification<Workflow, IWorkflowSpecificationVisitor> specification, CancellationToken cancellationToken)
        {
            var query = Workflows.AsQueryable().Where(x => specification.IsSatisfiedBy(x));
            var match = query.Distinct().FirstOrDefault();
            return Task.FromResult(match);
        }

        public Task AddAsync(Workflow value, CancellationToken cancellationToken)
        {
            Workflows.Add(value);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Workflow value, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}