using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Elsa.DistributedLock;
using Elsa.DistributedLocking;
using Medallion.Threading.SqlServer;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Elsa
{
    // CREDITS:
    // Implementation taken & adapted from Workflow Core: https://github.com/danielgerlag/workflow-core/blob/master/src/providers/WorkflowCore.LockProviders.SqlServer/SqlLockProvider.cs
    public class SqlLockProvider : IDistributedLockProvider
    {
        private readonly ILogger _logger;
        private readonly string _connectionString;
        private readonly ConcurrentDictionary<string, SqlDistributedLockHandle> _locks = new();

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

        public async Task<bool> AcquireLockAsync(string name, Duration? timeout = default, CancellationToken cancellationToken = default)
        {
            var distributedLock = new SqlDistributedLock(name, _connectionString);
            var timeoutTimeSpan = timeout?.ToTimeSpan() ?? TimeSpan.Zero;
            
            _logger.LogDebug("Acquiring a lock on {LockName}", name);

            if (_locks.ContainsKey(name))
                _logger.LogDebug("Waiting for existing lock {LockName} to be released", name);

            await using var handle = await distributedLock.AcquireAsync(timeoutTimeSpan, cancellationToken);
            
            if (handle == null!)
                return false;

            _locks[name] = handle;
            _logger.LogDebug("Lock acquired on {LockName}", name);
            
            return true;
        }

        public async Task ReleaseLockAsync(string name, CancellationToken cancellationToken)
        {
            if (!_locks.ContainsKey(name))
            {
                _logger.LogDebug("Failed to release lock that wasn't captured");
                return;
            }

            var handle = _locks[name];

            if (handle == null)
                return;

            await handle.DisposeAsync();
            _locks.TryRemove(name, out _);
            _logger.LogDebug("Released lock on {LockName}", name);
        }
    }
}