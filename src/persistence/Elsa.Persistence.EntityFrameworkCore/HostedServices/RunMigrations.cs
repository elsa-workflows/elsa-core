using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.EntityFrameworkCore.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Persistence.EntityFrameworkCore.HostedServices
{
    /// <summary>
    /// Executes EF Core migrations.
    /// </summary>
    public class RunMigrations : IHostedService
    {
        private readonly IServiceProvider serviceProvider;
        public RunMigrations(IServiceProvider serviceProvider) => this.serviceProvider = serviceProvider;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = serviceProvider.CreateScope();
            var elsaContext = scope.ServiceProvider.GetRequiredService<ElsaContext>();
            await elsaContext.Database.MigrateAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}