using System;
using System.Linq.Expressions;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFramework.Core.Stores
{
    public class EntityFrameworkWorkflowExecutionLogRecordStore : EntityFrameworkStore<WorkflowExecutionLogRecord>, IWorkflowExecutionLogStore
    {
        public EntityFrameworkWorkflowExecutionLogRecordStore(IDbContextFactory<ElsaContext> dbContextFactory, IMapper mapper) : base(dbContextFactory, mapper)
        {
        }

        protected override Expression<Func<WorkflowExecutionLogRecord, bool>> MapSpecification(ISpecification<WorkflowExecutionLogRecord> specification)
        {
            return AutoMapSpecification(specification);
        }
    }
}