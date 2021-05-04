using Elsa.Models;
using Elsa.Services;
using MongoDB.Driver;

namespace Elsa.Persistence.MongoDb.Stores
{
    public class MongoDbBookmarkStore : MongoDbStore<Bookmark>, IBookmarkStore
    {
        public MongoDbBookmarkStore(IMongoCollection<Bookmark> collection, IIdGenerator idGenerator) : base(collection, idGenerator)
        {
        }
    }
}