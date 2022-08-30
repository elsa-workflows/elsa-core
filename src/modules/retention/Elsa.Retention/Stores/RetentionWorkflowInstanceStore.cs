using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Persistence.EntityFramework.Core.Stores;
using Elsa.Persistence.Specifications;
using Microsoft.Extensions.Logging;
using Open.Linq.AsyncExtensions;
using System.Linq.Expressions;
using System;
using Elsa.Persistence.EntityFramework.Core.Services;
using AutoMapper;
using Elsa.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Retention.Stores
{
    public class RetentionWorkflowInstanceStore : EntityFrameworkWorkflowInstanceStore, IRetentionWorkflowInstanceStore
    {
        public RetentionWorkflowInstanceStore(IElsaContextFactory dbContextFactory,
            IMapper mapper,
            IContentSerializer contentSerializer,
            ILogger<EntityFrameworkWorkflowInstanceStore> logger) : base(dbContextFactory, mapper, contentSerializer, logger)
        {
        }

        public async Task<IEnumerable<Tdto>> FindManyWithDTOAsync<Tdto>(ISpecification<WorkflowInstance> specification, IOrderBy<WorkflowInstance>? orderBy = default, IPaging? paging = default, Expression<Func<WorkflowInstance, Tdto>> selectDtoFunc = default, CancellationToken cancellationToken = default) where Tdto : class
        {
            var filter = MapSpecification(specification);

            return await DoQuery(async dbContext =>
            {
                var dbSet = dbContext.Set<WorkflowInstance>();
                var queryable = dbSet.Where(filter);

                if (orderBy != null)
                {
                    var orderByExpression = orderBy.OrderByExpression;
                    queryable = orderBy.SortDirection == SortDirection.Ascending ? queryable.OrderBy(orderByExpression) : queryable.OrderByDescending(orderByExpression);
                }

                if (paging != null)
                    queryable = queryable.Skip(paging.Skip).Take(paging.Take);

                var queryableDto = queryable.Select(selectDtoFunc).AsQueryable();
                return await queryableDto.ToListAsync();

            }, cancellationToken);
        }



    }

}