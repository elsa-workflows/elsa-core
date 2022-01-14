using System;
using Elsa.Models;
using Elsa.Services;
using MongoDB.Driver;

namespace Elsa.Persistence.MongoDb.Stores
{
    public class MongoDbBookmarkStore : MongoDbStore<Bookmark>, IBookmarkStore
    {
        public MongoDbBookmarkStore(Func<IMongoCollection<Bookmark>> collectionFactory, IIdGenerator idGenerator) : base(collectionFactory, idGenerator)
        {
        }
    }
}