using System;
using System.Linq.Expressions;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.EntityFramework.Core.Models;
using Elsa.Persistence.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFramework.Core.Stores
{
    public class EntityFrameworkWorkflowInstanceStore : EntityFrameworkStore<WorkflowInstance, WorkflowInstanceEntity>, IWorkflowInstanceStore
    {
        public EntityFrameworkWorkflowInstanceStore(ElsaContext dbContext, IMapper mapper) : base(dbContext, mapper)
        {
        }

        protected override DbSet<WorkflowInstanceEntity> DbSet => DbContext.WorkflowInstances;
        protected override Expression<Func<WorkflowInstanceEntity, bool>> MapSpecification(ISpecification<WorkflowInstance> specification)
        {
            return AutoMapSpecification(specification);
        }
    }
}