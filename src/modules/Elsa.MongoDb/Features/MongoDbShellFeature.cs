using Elsa.MongoDb.Options;
using Elsa.Framework.Shells;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using static Elsa.MongoDb.Helpers.MongoDbFeatureHelper;

namespace Elsa.MongoDb.Features;

[Feature("MongoDb")]
public class MongoDbShellFeature(IConfiguration configuration) : ShellFeature
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.Configure<MongoDbOptions>(configuration.GetSection("MongoDb"));
        services.AddScoped(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MongoDbOptions>>().Value;
            var connectionStringName = string.IsNullOrEmpty(options.ConnectionStringOrName) ? "MongoDb" : options.ConnectionStringOrName;
            var connectionString = configuration.GetConnectionString(connectionStringName) ?? connectionStringName;
            return CreateDatabase(sp, connectionString);
        });

        RegisterSerializers();
        RegisterClassMaps();
    }
}