using Elsa.Jobs.Models;
using Elsa.Jobs.Notifications;
using Elsa.Jobs.Services;
using Elsa.Mediator.Services;

namespace Elsa.Jobs.Implementations;

public class JobRunner : IJobRunner
{
    private readonly IEventPublisher _eventPublisher;
    private readonly IServiceProvider _serviceProvider;

    public JobRunner(IEventPublisher eventPublisher, IServiceProvider serviceProvider)
    {
        _eventPublisher = eventPublisher;
        _serviceProvider = serviceProvider;
    }

    public async Task RunJobAsync(IJob job, CancellationToken cancellationToken = default)
    {
        var context = new JobExecutionContext(_serviceProvider, cancellationToken);
        await job.ExecuteAsync(context);
        await _eventPublisher.PublishAsync(new JobExecuted(job), cancellationToken);
    }
}