﻿using System;
using System.Linq.Expressions;
using Elsa.Models;
using Elsa.Persistence.Specifications;
using Elsa.Serialization;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Elsa.Persistence.EntityFramework.Core.Stores
{
    public class EntityFrameworkWorkflowDefinitionStore : EntityFrameworkStore<WorkflowDefinition>, IWorkflowDefinitionStore
    {
        private readonly IContentSerializer _contentSerializer;

        public EntityFrameworkWorkflowDefinitionStore(IDbContextFactory<ElsaContext> dbContextFactory, IContentSerializer contentSerializer) : base(dbContextFactory)
        {
            _contentSerializer = contentSerializer;
        }
        
        protected override Expression<Func<WorkflowDefinition, bool>> MapSpecification(ISpecification<WorkflowDefinition> specification) => AutoMapSpecification(specification);

        protected override void OnSaving(ElsaContext dbContext, WorkflowDefinition entity)
        {
            var data = new
            {
                entity.Activities,
                entity.Connections,
                entity.Variables,
                entity.ContextOptions,
                entity.CustomAttributes
            };
            
            var json = _contentSerializer.Serialize(data);
            dbContext.Entry(entity).Property("Data").CurrentValue = json;
        }

        protected override void OnLoading(ElsaContext dbContext, WorkflowDefinition entity)
        {
            var data = new
            {
                entity.Activities,
                entity.Connections,
                entity.Variables,
                entity.ContextOptions,
                entity.CustomAttributes
            };
            
            var json = (string)dbContext.Entry(entity).Property("Data").CurrentValue;
            data = JsonConvert.DeserializeAnonymousType(json, data, DefaultContentSerializer.CreateDefaultJsonSerializationSettings());

            entity.Activities = data.Activities;
            entity.Connections = data.Connections;
            entity.Variables = data.Variables;
            entity.ContextOptions = data.ContextOptions;
            entity.CustomAttributes = data.CustomAttributes;
        }
    }
}