using Elsa.Services;
using Elsa.Persistence.MongoDb.Stores;
using Elsa.Webhooks.Models;
using MongoDB.Driver;

namespace Elsa.Webhooks.Persistence.MongoDb.Stores
{
    public class MongoDbWebhookDefinitionStore : MongoDbStore<WebhookDefinition>, IWebhookDefinitionStore
    {
        public MongoDbWebhookDefinitionStore(IMongoCollection<WebhookDefinition> collection, IIdGenerator idGenerator) : base(collection, idGenerator)
        {
        }
    }
}