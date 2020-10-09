using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Runtime
{
    public class StartupRunner : IStartupRunner
    {
        private readonly IServiceProvider _serviceProvider;

        public StartupRunner(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
        }
        
        public async Task StartupAsync(CancellationToken cancellationToken = default)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var startupTasks = scope.ServiceProvider.GetServices<IStartupTask>();
                
                foreach (var startupTask in startupTasks)
                {
                    await startupTask.ExecuteAsync(cancellationToken);
                }
            }
        }
    }
}