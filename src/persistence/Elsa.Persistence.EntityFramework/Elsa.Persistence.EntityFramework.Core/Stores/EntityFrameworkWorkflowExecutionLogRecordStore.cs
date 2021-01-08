using System;
using System.Linq.Expressions;
using Elsa.Models;
using Elsa.Persistence.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFramework.Core.Stores
{
    public class EntityFrameworkWorkflowExecutionLogRecordStore : EntityFrameworkStore<WorkflowExecutionLogRecord>, IWorkflowExecutionLogStore
    {
        public EntityFrameworkWorkflowExecutionLogRecordStore(ElsaContext dbContext) : base(dbContext)
        {
        }

        protected override DbSet<WorkflowExecutionLogRecord> DbSet => DbContext.WorkflowExecutionLogRecords;
        protected override Expression<Func<WorkflowExecutionLogRecord, bool>> MapSpecification(ISpecification<WorkflowExecutionLogRecord> specification)
        {
            return AutoMapSpecification(specification);
        }
    }
}