using System;
using Elsa.Persistence.MongoDb;
using Elsa.Testing.Shared.AutoFixture.Attributes;
using Elsa.Testing.Shared.Helpers;

namespace Elsa.Core.IntegrationTests.Autofixture
{
    public class WithMongoDbAttribute : ElsaHostBuilderBuilderCustomizeAttributeBase
    {
        readonly string dbName;

        public override Action<ElsaHostBuilderBuilder> GetBuilderCustomizer()
        {
            var connectionString = Environment.GetEnvironmentVariable("TEST_MONGODB") ?? "mongodb://localhost:27017";
            return builder => {
                builder.ElsaCallbacks.Add(elsa => {
                    elsa.UseMongoDbPersistence(opts => {
                        opts.ConnectionString = connectionString;
                        opts.DatabaseName = dbName;
                    });
                });
            };
        }

        public WithMongoDbAttribute(string dbName = "IntegrationTests")
        {
            this.dbName = dbName ?? throw new ArgumentNullException(nameof(dbName));
        }
    }
}