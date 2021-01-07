using System;
using System.Linq.Expressions;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.EntityFramework.Core.Models;
using Elsa.Persistence.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFramework.Core.Stores
{
    public class EntityFrameworkWorkflowDefinitionStore : EntityFrameworkStore<WorkflowDefinition, WorkflowDefinitionEntity>, IWorkflowDefinitionStore
    {
        public EntityFrameworkWorkflowDefinitionStore(ElsaContext dbContext, IMapper mapper) : base(dbContext, mapper)
        {
        }

        protected override DbSet<WorkflowDefinitionEntity> DbSet => DbContext.WorkflowDefinitions;
        protected override Expression<Func<WorkflowDefinitionEntity, bool>> MapSpecification(ISpecification<WorkflowDefinition> specification) => AutoMapSpecification(specification);
    }
}