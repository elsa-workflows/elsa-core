using Elsa.Mediator.Middleware.Command;
using Elsa.Mediator.Middleware.Command.Contracts;
using Elsa.Tenants.Mediator.Middleware;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;

namespace Elsa.Tenants.Mediator.Tasks;

[UsedImplicitly]
public class SetupMediatorPipelines(ICommandPipeline commandPipeline) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        commandPipeline.Setup(pipeline => pipeline.UseMiddleware<TenantPropagatingMiddleware>(0));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}