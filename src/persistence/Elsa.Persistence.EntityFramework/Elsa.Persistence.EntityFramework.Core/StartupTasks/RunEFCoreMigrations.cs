using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EntityFramework.Core.StartupTasks
{
    /// <summary>
    /// Executes EF Core migrations.
    /// </summary>
    public class RunEFCoreMigrations : IStartupTask
    {
        private readonly ElsaContext _elsaContext;
        public RunEFCoreMigrations(ElsaContext elsaContext) => _elsaContext = elsaContext;
        public async Task ExecuteAsync(CancellationToken cancellationToken = default) => await _elsaContext.Database.MigrateAsync(cancellationToken);
    }
}