using Elsa.Services;
using Elsa.Persistence.MongoDb.Stores;
using Elsa.Webhooks.Models;
using MongoDB.Driver;
using System;

namespace Elsa.Webhooks.Persistence.MongoDb.Stores
{
    public class MongoDbWebhookDefinitionStore : MongoDbStore<WebhookDefinition>, IWebhookDefinitionStore
    {
        public MongoDbWebhookDefinitionStore(Func<IMongoCollection<WebhookDefinition>> collectionFactory, IIdGenerator idGenerator) : base(collectionFactory, idGenerator)
        {
        }
    }
}