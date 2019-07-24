using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;

namespace Elsa.Runtime
{
    // Taken & adapted from: https://andrewlock.net/running-async-tasks-on-app-startup-in-asp-net-core-part-2/
    public class TaskExecutingServer : IServer
    {
        private readonly IServer server;
        private readonly IEnumerable<IStartupTask> startupTasks;

        public TaskExecutingServer(IServer server, IEnumerable<IStartupTask> startupTasks)
        {
            this.server = server;
            this.startupTasks = startupTasks;
        }

        public IFeatureCollection Features => server.Features;
        
        public async Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        {
            foreach (var startupTask in startupTasks)
            {
                await startupTask.ExecuteAsync(cancellationToken);
            }

            await server.StartAsync(application, cancellationToken);
        }

        public void Dispose() => server.Dispose();
        public Task StopAsync(CancellationToken cancellationToken) => server.StopAsync(cancellationToken);
    }
}