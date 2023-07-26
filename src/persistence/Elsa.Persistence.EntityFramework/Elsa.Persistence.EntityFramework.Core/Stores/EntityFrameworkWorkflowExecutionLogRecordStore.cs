using System;
using System.Linq.Expressions;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.EntityFramework.Core.Services;
using Elsa.Persistence.Specifications;
using Microsoft.Extensions.Logging;

namespace Elsa.Persistence.EntityFramework.Core.Stores
{
    public class EntityFrameworkWorkflowExecutionLogRecordStore : ElsaContextEntityFrameworkStore<WorkflowExecutionLogRecord>, IWorkflowExecutionLogStore
    {
        public EntityFrameworkWorkflowExecutionLogRecordStore(IElsaContextFactory dbContextFactory, IMapper mapper, ILogger<EntityFrameworkWorkflowExecutionLogRecordStore> logger) : base(dbContextFactory, mapper, logger)
        {
        }

        protected override Expression<Func<WorkflowExecutionLogRecord, bool>> MapSpecification(ISpecification<WorkflowExecutionLogRecord> specification)
        {
            return AutoMapSpecification(specification);
        }
    }
}