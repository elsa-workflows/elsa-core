using System;
using Elsa.Persistence.MongoDb;
using Elsa.Persistence.MongoDb.Options;
using Elsa.Testing.Shared.AutoFixture.Attributes;
using Elsa.Testing.Shared.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Core.IntegrationTests.Autofixture
{
    public class WithMongoDbAttribute : ElsaHostBuilderBuilderCustomizeAttributeBase
    {
        readonly string dbName;

        public override Action<ElsaHostBuilderBuilder> GetBuilderCustomizer()
        {
            return builder => {
                builder.ElsaCallbacks.Add(elsa => {
                    elsa.Services.AddScoped<ElsaMongoDbOptions>(sp =>
                        new ElsaMongoDbOptions()
                        {
                            DatabaseName = dbName,
                            ConnectionString = "mongodb://localhost:27017"
                        }
                    );

                    elsa.UseMongoDbPersistence();
                });
            };
        }

        public WithMongoDbAttribute(string dbName = "IntegrationTests")
        {
            this.dbName = dbName ?? throw new ArgumentNullException(nameof(dbName));
        }
    }
}