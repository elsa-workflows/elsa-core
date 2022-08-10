using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.HostedServices;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Runtime
{
    public class BackgroundServiceRunner : IStartupTask
    {
        public int Order => 9999;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<StartupRunner> _logger;
        private readonly ICollection<Type> _hostedServiceTypes;

        public BackgroundServiceRunner(IEnumerable<IElsaHostedService> hostedServices, IServiceScopeFactory scopeFactory, ILogger<StartupRunner> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _hostedServiceTypes = hostedServices.Select(x => x.GetType()).ToList();
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            foreach (var hostedServiceType in _hostedServiceTypes)
            {
                using var scope = _scopeFactory.CreateScope();
                var hostedService = (IElsaHostedService)scope.ServiceProvider.GetRequiredService(hostedServiceType);
                _logger.LogInformation("Running hosted service {HostedServiceName}", hostedServiceType.Name);
                await hostedService.StartAsync(cancellationToken);
            }
        }
    }
}
