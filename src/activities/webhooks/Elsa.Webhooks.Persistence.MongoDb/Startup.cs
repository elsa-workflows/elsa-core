using Elsa.Attributes;
using Elsa.Services.Startup;
using Elsa.Webhooks.Persistence.MongoDb.Extensions;
using Microsoft.Extensions.Configuration;

namespace Elsa.Webhooks.Persistence.MongoDb
{
    [Feature("Webhooks:Persistence:MongoDb")]
    public class Startup : StartupBase
    {
        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            var section = configuration.GetSection($"Elsa:Persistence:MongoDb");
            var connectionStringName = section.GetValue<string>("ConnectionStringName");
            var connectionString = section.GetValue<string>("ConnectionString");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                if (string.IsNullOrWhiteSpace(connectionStringName))
                    connectionStringName = "MongoDb";

                connectionString = configuration.GetConnectionString(connectionStringName);
            }

            if (string.IsNullOrWhiteSpace(connectionString))
                connectionString = "mongodb://localhost:27017/Elsa";

            elsa.UseWebhookMongoDbPersistence(options => options.ConnectionString = connectionString);
        }
    }
}
