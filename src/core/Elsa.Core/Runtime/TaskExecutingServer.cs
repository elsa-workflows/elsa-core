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
        private readonly IStartupRunner startupRunner;

        public TaskExecutingServer(IServer server, IStartupRunner startupRunner)
        {
            this.server = server;
            this.startupRunner = startupRunner;
        }

        public IFeatureCollection Features => server.Features;
        
        public async Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        {
            await startupRunner.StartupAsync(cancellationToken);
            await server.StartAsync(application, cancellationToken);
        }

        public void Dispose() => server.Dispose();
        public Task StopAsync(CancellationToken cancellationToken) => server.StopAsync(cancellationToken);
    }
}