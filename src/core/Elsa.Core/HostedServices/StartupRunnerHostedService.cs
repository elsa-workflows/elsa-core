using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.HostedServices
{
    /// <summary>
    /// A background service that executes the startup runner when the application starts.
    /// </summary>
    public class StartupRunnerHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StartupRunnerHostedService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="StartupRunnerHostedService"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider used to resolve the startup runner.</param>
        /// <param name="logger">The logger for logging startup task execution and errors.</param>
        public StartupRunnerHostedService(IServiceProvider serviceProvider, ILogger<StartupRunnerHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Executes the startup runner to perform application initialization tasks.
        /// </summary>
        /// <param name="stoppingToken">A cancellation token that is triggered when the application is shutting down.</param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await using var scope = _serviceProvider.CreateAsyncScope();
                var startupRunner = scope.ServiceProvider.GetRequiredService<IStartupRunner>();
                await startupRunner.StartupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while executing startup tasks");
            }
        }
    }
}