using Elsa.Jobs.Contracts;
using Elsa.Jobs.Models;
using Elsa.Jobs.Notifications;
using Elsa.Mediator.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Jobs.Services;

public class JobRunner : IJobRunner
{
    private readonly IEventPublisher _eventPublisher;
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public JobRunner(IEventPublisher eventPublisher, IServiceProvider serviceProvider, IServiceScopeFactory serviceScopeFactory)
    {
        _eventPublisher = eventPublisher;
        _serviceProvider = serviceProvider;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task RunJobAsync(IJob job, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = new JobExecutionContext(scope.ServiceProvider, cancellationToken);
        await job.ExecuteAsync(context);
        await _eventPublisher.PublishAsync(new JobExecuted(job), cancellationToken);
    }
}