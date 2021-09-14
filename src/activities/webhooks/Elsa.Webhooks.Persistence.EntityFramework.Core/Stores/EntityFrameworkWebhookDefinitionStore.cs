using System;
using System.Linq.Expressions;
using Elsa.Persistence.EntityFramework.Core.Stores;
using Elsa.Persistence.Specifications;
using Elsa.Serialization;
using Elsa.Webhooks.Persistence.EntityFramework.Core.Services;
using Elsa.Webhooks.Models;
using AutoMapper;

namespace Elsa.Webhooks.Persistence.EntityFramework.Core.Stores
{
    public class EntityFrameworkWebhookDefinitionStore : EntityFrameworkStore<WebhookDefinition, WebhookContext>, IWebhookDefinitionStore
    {
        private readonly IContentSerializer _contentSerializer;

        public EntityFrameworkWebhookDefinitionStore(IWebhookContextFactory dbContextFactory, IMapper mapper, IContentSerializer contentSerializer) : base(dbContextFactory, mapper)
        {
            _contentSerializer = contentSerializer;
        }

        protected override Expression<Func<WebhookDefinition, bool>> MapSpecification(ISpecification<WebhookDefinition> specification) => AutoMapSpecification(specification);
    }
}