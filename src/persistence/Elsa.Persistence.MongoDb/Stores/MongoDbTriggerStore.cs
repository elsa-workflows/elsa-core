using Elsa.Models;
using Elsa.Services;
using MongoDB.Driver;

namespace Elsa.Persistence.MongoDb.Stores;

public class MongoDbTriggerStore : MongoDbStore<Trigger>, ITriggerStore
{
    public MongoDbTriggerStore(IMongoCollection<Trigger> collection, IIdGenerator idGenerator) : base(collection, idGenerator)
    {
    }
}