using Elsa.Attributes;
using Elsa.Options;
using Elsa.Secrets.Extentions;
using Elsa.Secrets.Persistence.MongoDb.Extentions;
using Elsa.Services.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.Persistence.MongoDb
{
    [Feature("Secrets:MongoDb")]
    public class Startup : StartupBase
    {
        public override void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
            var services = elsa.Services;
            var section = configuration.GetSection($"Elsa:Features:WorkflowSettings");
            var connectionStringName = section.GetValue<string>("ConnectionStringIdentifier");
            var connectionString = section.GetValue<string>("ConnectionString");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                if (string.IsNullOrWhiteSpace(connectionStringName))
                    connectionStringName = "MongoDb";

                connectionString = configuration.GetConnectionString(connectionStringName);
            }

            if (string.IsNullOrWhiteSpace(connectionString))
                connectionString = "mongodb://localhost:27017/Elsa";

            var secretsOptionsBuilder = new SecretsOptionsBuilder(services);
            secretsOptionsBuilder.UseSecretsMongoDbPersistence(options => options.ConnectionString = connectionString);
            services.AddScoped(sp => secretsOptionsBuilder.SectersOptions.SecretsStoreFactory(sp));

            elsa.AddSecrets();
        }
    }
}