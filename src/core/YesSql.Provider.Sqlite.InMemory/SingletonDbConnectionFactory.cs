using System;
using System.Data.Common;

namespace YesSql.Provider.Sqlite.InMemory
{
    public class SingletonDbConnectionFactory<TDbConnection> : IConnectionFactory
        where TDbConnection : DbConnection, new()
    {
        private readonly string _connectionString;
        private DbConnection _connection;

        public Type DbConnectionType => typeof(TDbConnection);

        public SingletonDbConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public DbConnection CreateConnection()
        {
            if (_connection == null)
            {
                _connection = new TDbConnection
                {
                    ConnectionString = _connectionString
                };
            }
            
            return _connection;
        }
    }
}