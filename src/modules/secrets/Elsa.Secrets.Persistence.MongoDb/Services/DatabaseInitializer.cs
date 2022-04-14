using Elsa.Secrets.Models;
using Elsa.Services;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Secrets.Persistence.MongoDb.Services
{
    public class DatabaseInitializer : IStartupTask
    {
        private readonly ElsaMongoDbContext _mongoContext;

        public DatabaseInitializer(ElsaMongoDbContext mongoContext)
        {
            _mongoContext = mongoContext;
        }

        public int Order => 0;

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            
        }

    }
}
