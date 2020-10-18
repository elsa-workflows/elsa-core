using System;
using System.Data;
using YesSql;
using YesSql.Provider.Sqlite;

namespace Elsa.YesSql.Provider.Sqlite.InMemory
{
    public static class InMemorySqliteDbProviderOptionsExtensions
    {
        public static IConfiguration RegisterInMemorySqlite(this IConfiguration configuration)
        {
            SqlDialectFactory.SqlDialects["inmemorysqliteconnection"] = new InMemorySqliteDialect();
            CommandInterpreterFactory.CommandInterpreters["inmemorysqliteconnection"] = d => new SqliteCommandInterpreter(d);

            return configuration;
        }

        public static IConfiguration UseInMemorySqlite(
            this IConfiguration configuration,
            IsolationLevel isolationLevel = IsolationLevel.Serializable)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            RegisterInMemorySqlite(configuration);
            configuration.ConnectionFactory = new SingletonDbConnectionFactory<InMemorySqliteConnection>("Filename=:memory:");
            configuration.IsolationLevel = isolationLevel;

            return configuration;
        }
    }
}