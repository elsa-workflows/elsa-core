using Elsa.Persistence.MongoDb.Options;
using Elsa.Secrets.Persistence.MongoDb.Services;
using Elsa.Secrets.Persistence.MongoDb.Stores;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.Persistence.MongoDb.Extensions
{
    public static class SecretsServiceCollectionExtensions
    {
        public static SecretsOptionsBuilder UseSecretsMongoDbPersistence(this SecretsOptionsBuilder secretsOptions, Action<ElsaMongoDbOptions> configureOptions) => UseSecretsMongoDbPersistence<ElsaMongoDbContext>(secretsOptions, configureOptions);

        public static SecretsOptionsBuilder UseSecretsMongoDbPersistence<TDbContext>(this SecretsOptionsBuilder secretsOptions, Action<ElsaMongoDbOptions> configureOptions) where TDbContext : ElsaMongoDbContext
        {
            AddCore<TDbContext>(secretsOptions);
            secretsOptions.Services.Configure(configureOptions);
            return secretsOptions;
        }

        public static SecretsOptionsBuilder UseSecretsMongoDbPersistence(this SecretsOptionsBuilder secretsOptions, IConfiguration configuration) => UseSecretsMongoDbPersistence<ElsaMongoDbContext>(secretsOptions, configuration);

        public static SecretsOptionsBuilder UseSecretsMongoDbPersistence<TDbContext>(this SecretsOptionsBuilder secretsOptions, IConfiguration configuration) where TDbContext : ElsaMongoDbContext
        {
            AddCore<TDbContext>(secretsOptions);
            secretsOptions.Services.Configure<ElsaMongoDbOptions>(configuration);
            return secretsOptions;
        }

        private static void AddCore<TDbContext>(SecretsOptionsBuilder secretsOptions) where TDbContext : ElsaMongoDbContext
        {
            secretsOptions.Services
                .AddSingleton<MongoDbSecretsStore>()
                .AddSingleton<TDbContext>()
                .AddSingleton<ElsaMongoDbContext, TDbContext>()
                .AddSingleton(sp => sp.GetRequiredService<TDbContext>().Secrets);

            secretsOptions.UseSecretsStore(sp => sp.GetRequiredService<MongoDbSecretsStore>());

            DatabaseRegister.RegisterMapsAndSerializers();
        }
    }
}
