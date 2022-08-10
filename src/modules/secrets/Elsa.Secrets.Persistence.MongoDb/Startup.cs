using Elsa.Attributes;
using Elsa.Extensions;
using Elsa.Options;
using Elsa.Secrets.Extensions;
using Elsa.Secrets.Persistence.MongoDb.Extensions;
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
            var secretsOptionsBuilder = new SecretsOptionsBuilder(elsa.Services, elsa.ContainerBuilder);
            secretsOptionsBuilder.UseSecretsMongoDbPersistence();
            elsa.ContainerBuilder.AddScoped(sp => secretsOptionsBuilder.SecretsOptions.SecretsStoreFactory(sp));

            elsa.AddSecrets();
        }
    }
}