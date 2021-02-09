﻿using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.Specifications;
using Elsa.Serialization;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Elsa.Persistence.EntityFramework.Core.Stores
{
    public class EntityFrameworkWorkflowInstanceStore : EntityFrameworkStore<WorkflowInstance>, IWorkflowInstanceStore
    {
        private readonly IContentSerializer _contentSerializer;

        public EntityFrameworkWorkflowInstanceStore(IDbContextFactory<ElsaContext> dbContextFactory, IMapper mapper, IContentSerializer contentSerializer) : base(dbContextFactory, mapper)
        {
            _contentSerializer = contentSerializer;
        }

        public override async Task DeleteAsync(WorkflowInstance entity, CancellationToken cancellationToken = default)
        {
            var workflowInstanceId = entity.Id;
            
            await DoWork(async dbContext =>
            {
                await dbContext.Set<WorkflowExecutionLogRecord>().AsQueryable().Where(x => x.WorkflowInstanceId == workflowInstanceId).DeleteFromQueryAsync(cancellationToken);
                await dbContext.Set<Bookmark>().AsQueryable().Where(x => x.WorkflowInstanceId == workflowInstanceId).DeleteFromQueryAsync(cancellationToken);
                await dbContext.Set<WorkflowInstance>().DeleteByKeyAsync(cancellationToken, workflowInstanceId);
            }, cancellationToken);
        }

        public override async Task<int> DeleteManyAsync(ISpecification<WorkflowInstance> specification, CancellationToken cancellationToken = default)
        {
            var workflowInstances = (await FindManyAsync(specification, cancellationToken: cancellationToken)).ToList();
            var workflowInstanceIds = workflowInstances.Select(x => x.Id).ToList();

            await DoWork(async dbContext =>
            {
                await dbContext.Set<WorkflowExecutionLogRecord>().WhereBulkContains(workflowInstanceIds, x => x.WorkflowInstanceId).DeleteFromQueryAsync(cancellationToken);
                await dbContext.Set<Bookmark>().WhereBulkContains(workflowInstanceIds, x => x.WorkflowInstanceId).DeleteFromQueryAsync(cancellationToken);
                await dbContext.Set<WorkflowInstance>().WhereBulkContains(workflowInstanceIds, x => x.Id).DeleteFromQueryAsync(cancellationToken);
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
                entity.Output,
                entity.Variables,
                entity.ActivityData,
                entity.ActivityOutput,
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
                entity.Output,
                entity.Variables,
                entity.ActivityData,
                entity.ActivityOutput,
                entity.BlockingActivities,
                entity.ScheduledActivities,
                entity.Scopes,
                entity.Fault,
                entity.CurrentActivity
            };
            
            var json = (string)dbContext.Entry(entity).Property("Data").CurrentValue;
            
            if(!string.IsNullOrWhiteSpace(json))
                data = JsonConvert.DeserializeAnonymousType(json, data, DefaultContentSerializer.CreateDefaultJsonSerializationSettings());
            
            entity.Output = data.Output;
            entity.Variables = data.Variables;
            entity.ActivityData = data.ActivityData;
            entity.ActivityOutput = data.ActivityOutput;
            entity.BlockingActivities = data.BlockingActivities;
            entity.ScheduledActivities = data.ScheduledActivities;
            entity.Scopes = data.Scopes;
            entity.Fault = data.Fault;
            entity.CurrentActivity = data.CurrentActivity;
        }
    }
}