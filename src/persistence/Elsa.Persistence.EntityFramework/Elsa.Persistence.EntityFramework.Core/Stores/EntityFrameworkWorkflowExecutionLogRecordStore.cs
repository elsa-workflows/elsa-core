using System;
using System.Linq.Expressions;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.EntityFramework.Core.Services;
using Elsa.Persistence.Specifications;

namespace Elsa.Persistence.EntityFramework.Core.Stores
{
    public class EntityFrameworkWorkflowExecutionLogRecordStore : ElsaContextEntityFrameworkStore<WorkflowExecutionLogRecord>, IWorkflowExecutionLogStore
    {
        public EntityFrameworkWorkflowExecutionLogRecordStore(Func<IElsaContextFactory> getDbContextFactory, IMapper mapper) : base(getDbContextFactory, mapper)
        {
        }

        protected override Expression<Func<WorkflowExecutionLogRecord, bool>> MapSpecification(ISpecification<WorkflowExecutionLogRecord> specification)
        {
            return AutoMapSpecification(specification);
        }
    }
}