using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.YesSql.Data;
using Elsa.Services;

namespace Elsa.Persistence.YesSql.Services
{
    public class RunMigrations : IStartupTask
    {
        private readonly IDataMigrationManager _dataMigrationManager;
        public RunMigrations(IDataMigrationManager dataMigrationManager) => _dataMigrationManager = dataMigrationManager;
        public int Order => 0;
        public async Task ExecuteAsync(CancellationToken cancellationToken = default) => await _dataMigrationManager.RunAllAsync();
    }
}