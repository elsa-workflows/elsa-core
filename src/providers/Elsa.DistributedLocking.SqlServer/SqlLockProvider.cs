using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Elsa.DistributedLock;
using Microsoft.Extensions.Logging;

namespace Elsa
{
    // CREDITS:
    // Implementation taken & adapted from Workflow Core: https://github.com/danielgerlag/workflow-core/blob/master/src/providers/WorkflowCore.LockProviders.SqlServer/SqlLockProvider.cs
    public class SqlLockProvider : IDistributedLockProvider
    {
        private readonly ILogger _logger;
        private const string Prefix = "elsa";
        private readonly string _connectionString;
        private readonly IDictionary<string, SqlConnection> _locks = new Dictionary<string, SqlConnection>();
        private readonly AutoResetEvent _mutex = new(true);

        public SqlLockProvider(string connectionString, ILogger<SqlLockProvider> logger)
        {
            _logger = logger;
            var connectionStringBuilder = new SqlConnectionStringBuilder(connectionString)
            {
                Pooling = true,
                ApplicationName = "Elsa Distributed Lock Provider"
            };

            _connectionString = connectionStringBuilder.ToString();
        }

        public async Task<bool> AcquireLockAsync(string name, CancellationToken cancellationToken)
        {
            if (!_mutex.WaitOne())
                return false;

            try
            {
                var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);
                try
                {
                    var command = connection.CreateCommand();
                    command.CommandText = "sp_getapplock";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Resource", $"{Prefix}:{name}");
                    command.Parameters.AddWithValue("@LockOwner", $"Session");
                    command.Parameters.AddWithValue("@LockMode", $"Exclusive");
                    command.Parameters.AddWithValue("@LockTimeout", 0);

                    var returnParameter = command.Parameters.Add("RetVal", SqlDbType.Int);
                    returnParameter.Direction = ParameterDirection.ReturnValue;

                    await command.ExecuteNonQueryAsync(cancellationToken);
                    var result = Convert.ToInt32(returnParameter.Value);

                    switch (result)
                    {
                        case -1:
                            _logger.LogDebug($"The lock request timed out for {name}");
                            break;

                        case -2:
                            _logger.LogDebug($"The lock request was canceled for {name}");
                            break;

                        case -3:
                            _logger.LogDebug($"The lock request was chosen as a deadlock victim for {name}");
                            break;

                        case -999:
                            _logger.LogError($"Lock provider error for {name}");
                            break;
                    }

                    if (result >= 0)
                    {
                        _locks[name] = connection;
                        return true;
                    }
                    else
                    {
                        connection.Close();
                        return false;
                    }
                }
                catch (Exception)
                {
                    connection.Close();
                    throw;
                }
            }
            finally
            {
                _mutex.Set();
            }
        }

        public async Task ReleaseLockAsync(string name, CancellationToken cancellationToken)
        {
            if (!_mutex.WaitOne())
                return;

            try
            {
                if (!_locks.ContainsKey(name))
                    return;

                var connection = _locks[name];

                if (connection == null)
                    return;

                try
                {
                    var command = connection.CreateCommand();
                    command.CommandText = "sp_releaseapplock";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Resource", $"{Prefix}:{name}");
                    command.Parameters.AddWithValue("@LockOwner", $"Session");

                    var returnParameter = command.Parameters.Add("RetVal", SqlDbType.Int);
                    returnParameter.Direction = ParameterDirection.ReturnValue;

                    await command.ExecuteNonQueryAsync(cancellationToken);
                    var result = Convert.ToInt32(returnParameter.Value);

                    if (result < 0)
                        _logger.LogError($"Unable to release lock for {name}");
                }
                finally
                {
                    connection.Close();
                    _locks.Remove(name);
                }
            }
            finally
            {
                _mutex.Set();
            }
        }
    }
}