using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Persistence.EntityFramework.Core.Services;
using Elsa.Persistence.Specifications;
using Elsa.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Elsa.Persistence.EntityFramework.Core.Stores
{
    public class EntityFrameworkWorkflowInstanceStore : ElsaContextEntityFrameworkStore<WorkflowInstance>, IWorkflowInstanceStore
    {
        private readonly IContentSerializer _contentSerializer;
        private readonly ILogger<EntityFrameworkWorkflowInstanceStore> _logger;

        public EntityFrameworkWorkflowInstanceStore(
            IElsaContextFactory dbContextFactory,
            IMapper mapper,
            IContentSerializer contentSerializer,
            ILogger<EntityFrameworkWorkflowInstanceStore> logger) : base(dbContextFactory, mapper, logger)
        {
            _contentSerializer = contentSerializer;
            _logger = logger;
        }

        public override async Task DeleteAsync(WorkflowInstance entity, CancellationToken cancellationToken = default)
        {
            var workflowInstanceId = entity.Id;

            await DoWork(async dbContext =>
            {
                await dbContext.Set<WorkflowExecutionLogRecord>().AsQueryable().Where(x => x.WorkflowInstanceId == workflowInstanceId).BatchDeleteWithWorkAroundAsync(dbContext, cancellationToken);
                await dbContext.Set<Bookmark>().AsQueryable().Where(x => x.WorkflowInstanceId == workflowInstanceId).BatchDeleteWithWorkAroundAsync(dbContext, cancellationToken);
                await dbContext.Set<WorkflowInstance>().AsQueryable().Where(x => x.Id == workflowInstanceId).BatchDeleteWithWorkAroundAsync(dbContext, cancellationToken);
            }, cancellationToken);
        }

        public override async Task<int> DeleteManyAsync(ISpecification<WorkflowInstance> specification, CancellationToken cancellationToken = default)
        {
            var workflowInstanceIds = (await FindManyAsync<string>(specification, (wf) => wf.Id, cancellationToken: cancellationToken)).ToList();
            await DeleteManyByIdsAsync(workflowInstanceIds, cancellationToken);
            return workflowInstanceIds.Count;
        }

        public async Task DeleteManyByIdsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
        {
            var idList = ids.ToList();

            if (!idList.Any())
                return;

            await DoWork(async dbContext =>
            {
                await dbContext.Set<WorkflowExecutionLogRecord>().AsQueryable().Where(x => idList.Contains(x.WorkflowInstanceId)).BatchDeleteWithWorkAroundAsync(dbContext, cancellationToken);
                await dbContext.Set<Bookmark>().AsQueryable().Where(x => idList.Contains(x.WorkflowInstanceId)).BatchDeleteWithWorkAroundAsync(dbContext, cancellationToken);
                await dbContext.Set<WorkflowInstance>().AsQueryable().Where(x => idList.Contains(x.Id)).BatchDeleteWithWorkAroundAsync(dbContext, cancellationToken);
            }, cancellationToken);
        }

        protected override Expression<Func<WorkflowInstance, bool>> MapSpecification(ISpecification<WorkflowInstance> specification)
        {
            return AutoMapSpecification(specification);
        }

        protected override void OnSaving(ElsaContext dbContext, WorkflowInstance entity)
        {
            var data = new
            {
                entity.Input,
                entity.Output,
                entity.Variables,
                entity.ActivityData,
                entity.Metadata,
                entity.BlockingActivities,
                entity.ScheduledActivities,
                entity.Scopes,
                entity.Fault,
                entity.Faults,
                entity.CurrentActivity
            };

            var json = _contentSerializer.Serialize(data);

            dbContext.Entry(entity).Property("Data").CurrentValue = json;
        }

        protected override void OnLoading(ElsaContext dbContext, WorkflowInstance entity)
        {
            var data = new
            {
                entity.Input,
                entity.Output,
                entity.Variables,
                entity.ActivityData,
                entity.Metadata,
                entity.BlockingActivities,
                entity.ScheduledActivities,
                entity.Scopes,
                entity.Fault,
                entity.Faults,
                entity.CurrentActivity
            };

            var json = (string)dbContext.Entry(entity).Property("Data").CurrentValue;

            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    data = JsonConvert.DeserializeAnonymousType(json, data, DefaultContentSerializer.CreateDefaultJsonSerializationSettings())!;
                }
                catch (JsonSerializationException e)
                {
                    _logger.LogWarning(e, "Failed to deserialize workflow instance JSON data");
                }
            }

            entity.Input = data.Input;
            entity.Output = data.Output;
            entity.Variables = data.Variables;
            entity.ActivityData = data.ActivityData;
            entity.Metadata = data.Metadata;
            entity.BlockingActivities = data.BlockingActivities;
            entity.ScheduledActivities = data.ScheduledActivities;
            entity.Scopes = data.Scopes;
            entity.Fault = data.Fault;
            entity.Faults = data.Faults;
            entity.CurrentActivity = data.CurrentActivity;
        }
    }
}