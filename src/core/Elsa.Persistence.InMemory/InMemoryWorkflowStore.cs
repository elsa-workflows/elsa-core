using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using NodaTime;

namespace Elsa.Persistence.InMemory
{
    public class InMemoryWorkflowStore : IWorkflowStore
    {
        private readonly IIdGenerator idGenerator;
        private readonly IClock clock;

        public InMemoryWorkflowStore(IIdGenerator idGenerator, IClock clock)
        {
            this.idGenerator = idGenerator;
            this.clock = clock;
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
            value.Metadata.Id = idGenerator.Generate();
            value.CreatedAt = clock.GetCurrentInstant();
            
            Workflows.Add(value);
            return Task.CompletedTask;
        }
        
        public Task UpdateAsync(Workflow value, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public async Task SaveAsync(Workflow value, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(value.Metadata.Id))
                await AddAsync(value, cancellationToken);
            else
                await UpdateAsync(value, cancellationToken);
        }
    }
}