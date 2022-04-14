using Elsa.Persistence.MongoDb.Stores;
using Elsa.Secrets.Models;
using Elsa.Services;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Secrets.Persistence.MongoDb.Stores
{
    public class MongoDbSecretsStore : MongoDbStore<Secret>, ISecretsStore
    {
        public MongoDbSecretsStore(IMongoCollection<Secret> collection, IIdGenerator idGenerator) : base(collection, idGenerator)
        {
        }
    }
}
