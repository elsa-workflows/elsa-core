using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Messages;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Persistence.InMem;
using Rebus.Routing.TypeBased;
using Rebus.Transport.InMem;

namespace Elsa.StartupTasks
{
    public class StartServiceBusTask : IStartupTask
    {
        public Task ExecuteAsync(CancellationToken cancellationToken = default)
        {   
            return Task.CompletedTask;
        }
    }
}