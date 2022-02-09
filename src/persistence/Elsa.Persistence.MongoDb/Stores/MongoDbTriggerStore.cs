using System;
using Elsa.Models;
using Elsa.Services;
using MongoDB.Driver;

namespace Elsa.Persistence.MongoDb.Stores;

public class MongoDbTriggerStore : MongoDbStore<Trigger>, ITriggerStore
{
    public MongoDbTriggerStore(Func<IMongoCollection<Trigger>> collectionFactory, IIdGenerator idGenerator) : base(collectionFactory, idGenerator)
    {
    }
}