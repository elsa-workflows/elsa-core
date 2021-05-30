using System;
using System.Linq.Expressions;
using AutoMapper;
using Elsa.Persistence.EntityFramework.Core.Services;
using Elsa.Persistence.EntityFramework.Core.Stores;
using Elsa.Persistence.Specifications;
using Elsa.Serialization;
using Elsa.Webhooks.Abstractions.Models;
using Elsa.Webhooks.Abstractions.Persistence;
//using Elsa.Webhooks.Persistence.EntityFramework.Core.Services;

namespace Elsa.Webhooks.Persistence.EntityFramework.Core.Stores
{
    public class EntityFrameworkWebhookDefinitionStore : EntityFrameworkStore<WebhookDefinition>, IWebhookDefinitionStore
    {
        private readonly IContentSerializer _contentSerializer;

        public EntityFrameworkWebhookDefinitionStore(IElsaContextFactory dbContextFactory, IMapper mapper, IContentSerializer contentSerializer) : base(dbContextFactory, mapper)
        {
            _contentSerializer = contentSerializer;
        }

        protected override Expression<Func<WebhookDefinition, bool>> MapSpecification(ISpecification<WebhookDefinition> specification) => AutoMapSpecification(specification);

        //protected override void OnSaving(WebhookContext dbContext, WebhookDefinition entity)
        //{
        //    //var data = new
        //    //{
        //    //    entity.Activities,
        //    //    entity.Connections,
        //    //    entity.Variables,
        //    //    entity.ContextOptions,
        //    //    entity.CustomAttributes
        //    //};

        //    //var json = _contentSerializer.Serialize(data);
        //    //dbContext.Entry(entity).Property("Data").CurrentValue = json;
        //}

        //protected override void OnLoading(WebhookContext dbContext, WebhookDefinition entity)
        //{
        //    //var data = new
        //    //{
        //    //    entity.Activities,
        //    //    entity.Connections,
        //    //    entity.Variables,
        //    //    entity.ContextOptions,
        //    //    entity.CustomAttributes
        //    //};

        //    //var json = (string)dbContext.Entry(entity).Property("Data").CurrentValue;
        //    //data = JsonConvert.DeserializeAnonymousType(json, data, DefaultContentSerializer.CreateDefaultJsonSerializationSettings());

        //    //entity.Activities = data.Activities;
        //    //entity.Connections = data.Connections;
        //    //entity.Variables = data.Variables;
        //    //entity.ContextOptions = data.ContextOptions;
        //    //entity.CustomAttributes = data.CustomAttributes;
        //}
    }
}