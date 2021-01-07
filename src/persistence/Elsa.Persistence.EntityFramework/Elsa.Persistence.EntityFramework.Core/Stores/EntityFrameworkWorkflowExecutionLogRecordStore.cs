using System;
using System.Linq.Expressions;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.EntityFramework.Core.Models;
using Elsa.Persistence.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFramework.Core.Stores
{
    public class EntityFrameworkWorkflowExecutionLogRecordStore : EntityFrameworkStore<WorkflowExecutionLogRecord, WorkflowExecutionLogRecordEntity>, IWorkflowExecutionLogStore
    {
        public EntityFrameworkWorkflowExecutionLogRecordStore(ElsaContext dbContext, IMapper mapper) : base(dbContext, mapper)
        {
        }

        protected override DbSet<WorkflowExecutionLogRecordEntity> DbSet => DbContext.WorkflowExecutionLogRecords;
        protected override Expression<Func<WorkflowExecutionLogRecordEntity, bool>> MapSpecification(ISpecification<WorkflowExecutionLogRecord> specification)
        {
            return AutoMapSpecification(specification);
        }
    }
}