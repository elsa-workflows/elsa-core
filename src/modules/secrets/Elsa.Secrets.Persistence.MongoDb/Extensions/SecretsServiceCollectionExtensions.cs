using Autofac;
using Autofac.Multitenant;
using Elsa.Extensions;
using Elsa.Multitenancy;
using Elsa.Multitenancy.Extensions;
using Elsa.Persistence.MongoDb.Options;
using Elsa.Secrets.Persistence.MongoDb.Services;
using Elsa.Secrets.Persistence.MongoDb.Stores;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.Persistence.MongoDb.Extensions
{
    public static class SecretsServiceCollectionExtensions
    {
        public static SecretsOptionsBuilder UseSecretsMongoDbPersistence(this SecretsOptionsBuilder secretsOptions) => UseSecretsMongoDbPersistence<ElsaMongoDbContext>(secretsOptions);

        public static SecretsOptionsBuilder UseSecretsMongoDbPersistence<TDbContext>(this SecretsOptionsBuilder secretsOptions) where TDbContext : ElsaMongoDbContext
        {
            AddCore<TDbContext>(secretsOptions);

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
            secretsOptions.ContainerBuilder
              .Register(cc =>
              {
                  var tenant = cc.Resolve<ITenant>();
                  return new ElsaMongoDbOptions() { ConnectionString = tenant!.GetDatabaseConnectionString()! };
              }).IfNotRegistered(typeof(ElsaMongoDbOptions)).InstancePerTenant();

            secretsOptions.ContainerBuilder
                .AddMultiton<MongoDbSecretsStore>()
                .AddMultiton<TDbContext>()
                .AddMultiton<ElsaMongoDbContext, TDbContext>()
                .AddMultiton(sp => sp.GetRequiredService<TDbContext>().Secrets);

            secretsOptions.UseSecretsStore(sp => sp.GetRequiredService<MongoDbSecretsStore>());

            DatabaseRegister.RegisterMapsAndSerializers();
        }
    }
}
