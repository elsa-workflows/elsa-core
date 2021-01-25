using System;
using System.Linq.Expressions;
using Elsa.Models;
using Elsa.Persistence.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFramework.Core.Stores
{
    public class EntityFrameworkWorkflowTriggerStore : EntityFrameworkStore<WorkflowTrigger>, IWorkflowTriggerStore
    {
        public EntityFrameworkWorkflowTriggerStore(ElsaContext dbContext) : base(dbContext)
        {
        }

        protected override DbSet<WorkflowTrigger> DbSet => DbContext.WorkflowTriggers;
        
        protected override Expression<Func<WorkflowTrigger, bool>> MapSpecification(ISpecification<WorkflowTrigger> specification)
        {
            return AutoMapSpecification(specification);
        }
    }
}