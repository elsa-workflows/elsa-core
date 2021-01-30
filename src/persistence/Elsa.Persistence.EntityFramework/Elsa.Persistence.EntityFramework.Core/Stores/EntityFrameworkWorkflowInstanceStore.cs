using System;
using System.Collections.Generic;
using System.Linq.Expressions;
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

        public EntityFrameworkWorkflowInstanceStore(IDbContextFactory<ElsaContext> dbContextFactory, IContentSerializer contentSerializer) : base(dbContextFactory)
        {
            _contentSerializer = contentSerializer;
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
            entity.Scopes = data.Scopes ?? new Stack<string>();
            entity.Fault = data.Fault;
            entity.CurrentActivity = data.CurrentActivity;
        }
    }
}