using System;
using System.Linq.Expressions;
using AutoMapper;
using Elsa.Persistence.EntityFramework.Core.Stores;
using Elsa.Persistence.Specifications;
using Elsa.Secrets.Models;
using Elsa.Secrets.Persistence.EntityFramework.Core.Services;
using Elsa.Serialization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Elsa.Secrets.Persistence.EntityFramework.Core.Stores
{
    public class EntityFrameworkSecretsStore : EntityFrameworkStore<Secret, SecretsContext>, ISecretsStore
    {
        private readonly IContentSerializer _contentSerializer;

        public EntityFrameworkSecretsStore(ISecretsContextFactory dbContextFactory, IMapper mapper, IContentSerializer contentSerializer, ILogger<EntityFrameworkSecretsStore> logger) : base(dbContextFactory, mapper, logger) => _contentSerializer = contentSerializer;

        protected override Expression<Func<Secret, bool>> MapSpecification(ISpecification<Secret> specification) => AutoMapSpecification(specification);

        protected override void OnSaving(SecretsContext dbContext, Secret entity)
        {
            var data = new
            {
                entity.Properties
            };

            var json = _contentSerializer.Serialize(data);
            dbContext.Entry(entity).Property("Data").CurrentValue = json;
        }

        protected override void OnLoading(SecretsContext dbContext, Secret entity)
        {
            var data = new
            {
                entity.Properties
            };

            var json = (string)dbContext.Entry(entity).Property("Data").CurrentValue;
            data = JsonConvert.DeserializeAnonymousType(json, data, DefaultContentSerializer.CreateDefaultJsonSerializationSettings())!;

            entity.Properties = data.Properties;
        }
    }
}
