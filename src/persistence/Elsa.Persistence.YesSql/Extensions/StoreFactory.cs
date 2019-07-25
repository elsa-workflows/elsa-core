using System;
using System.Data;
using System.Linq;
using System.Reflection;
using Elsa.Persistence.YesSql.Options;
using Elsa.Persistence.YesSql.Services;
using Elsa.Persistence.YesSql.StartupTasks;
using Elsa.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using YesSql;
using YesSql.Indexes;
using YesSql.Provider.MySql;
using YesSql.Provider.PostgreSql;
using YesSql.Provider.Sqlite;
using YesSql.Provider.SqlServer;

namespace Elsa.Persistence.YesSql.Extensions
{
    public static class StoreFactory
    {
        internal static IStore CreateStore(IServiceProvider services)
        {
            var options = services.GetRequiredService<IOptions<YesSqlOptions>>().Value;
            var connectionString = options.ConnectionString;
            IConfiguration configuration = new Configuration();

            switch (options.Provider)
            {
                case DatabaseProvider.SqlConnection:
                    configuration
                        .UseSqlServer(connectionString, IsolationLevel.ReadUncommitted)
                        .UseBlockIdGenerator();
                    break;
                case DatabaseProvider.SqLite:
                    configuration
                        .UseSqLite(connectionString, IsolationLevel.ReadUncommitted)
                        .UseDefaultIdGenerator();
                    break;
                case DatabaseProvider.MySql:
                    configuration
                        .UseMySql(connectionString, IsolationLevel.ReadUncommitted)
                        .UseDefaultIdGenerator();
                    break;
                case DatabaseProvider.Postgres:
                    configuration
                        .UsePostgreSql(connectionString, IsolationLevel.ReadUncommitted)
                        .UseDefaultIdGenerator();
                    break;
            }

            if (!string.IsNullOrWhiteSpace(options.TablePrefix))
            {
                configuration = configuration.SetTablePrefix($"{options.TablePrefix}_");
            }

            var store = global::YesSql.StoreFactory.CreateAsync(configuration).GetAwaiter().GetResult();
            var indexes = services.GetServices<IIndexProvider>();

            store.RegisterIndexes(indexes);
            return store;
        }
    }
}