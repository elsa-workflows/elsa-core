using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;

namespace Elsa.Data.Services
{
    public class DataMigrationsRunner : IStartupTask
    {
        private readonly IDataMigrationManager _dataMigrationManager;

        public DataMigrationsRunner(IDataMigrationManager dataMigrationManager)
        {
            _dataMigrationManager = dataMigrationManager;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default) =>
            await _dataMigrationManager.RunAllAsync();
    }
}