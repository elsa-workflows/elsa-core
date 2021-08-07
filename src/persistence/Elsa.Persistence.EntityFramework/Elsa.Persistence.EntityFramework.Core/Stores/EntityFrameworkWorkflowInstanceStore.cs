using System;
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
using Newtonsoft.Json;

namespace Elsa.Persistence.EntityFramework.Core.Stores
{
    public class EntityFrameworkWorkflowInstanceStore : ElsaContextEntityFrameworkStore<WorkflowInstance>, IWorkflowInstanceStore
    {
        private readonly IContentSerializer _contentSerializer;

        public EntityFrameworkWorkflowInstanceStore(IElsaContextFactory dbContextFactory, IMapper mapper, IContentSerializer contentSerializer) : base(dbContextFactory, mapper)
        {
            _contentSerializer = contentSerializer;
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
            var workflowInstances = (await FindManyAsync(specification, cancellationToken: cancellationToken)).ToList();
            var workflowInstanceIds = workflowInstances.Select(x => x.Id).ToArray();

            await DoWork(async dbContext =>
            {
                await dbContext.Set<WorkflowExecutionLogRecord>().AsQueryable().Where(x => workflowInstanceIds.Contains(x.WorkflowInstanceId)).BatchDeleteWithWorkAroundAsync(dbContext, cancellationToken);
                await dbContext.Set<Bookmark>().AsQueryable().Where(x => workflowInstanceIds.Contains(x.WorkflowInstanceId)).BatchDeleteWithWorkAroundAsync(dbContext, cancellationToken);
                await dbContext.Set<WorkflowInstance>().AsQueryable().Where(x => workflowInstanceIds.Contains(x.Id)).BatchDeleteWithWorkAroundAsync(dbContext, cancellationToken);
            }, cancellationToken);

            return workflowInstances.Count;
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
                entity.BlockingActivities,
                entity.ScheduledActivities,
                entity.Scopes,
                entity.Fault,
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
                entity.BlockingActivities,
                entity.ScheduledActivities,
                entity.Scopes,
                entity.Fault,
                entity.CurrentActivity
            };

            var json = (string) dbContext.Entry(entity).Property("Data").CurrentValue;

            if (!string.IsNullOrWhiteSpace(json))
                data = JsonConvert.DeserializeAnonymousType(json, data, DefaultContentSerializer.CreateDefaultJsonSerializationSettings())!;

            entity.Input = data.Input;
            entity.Output = data.Output;
            entity.Variables = data.Variables;
            entity.ActivityData = data.ActivityData;
            entity.BlockingActivities = data.BlockingActivities;
            entity.ScheduledActivities = data.ScheduledActivities;
            entity.Scopes = data.Scopes;
            entity.Fault = data.Fault;
            entity.CurrentActivity = data.CurrentActivity;
        }
    }
}