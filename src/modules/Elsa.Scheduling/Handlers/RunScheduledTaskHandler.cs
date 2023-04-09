using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;
using Elsa.Scheduling.Commands;
using Elsa.Scheduling.Models;

namespace Elsa.Scheduling.Handlers;

/// <summary>
/// A command handler for <see cref="RunScheduledTask"/>.
/// </summary>
public class RunScheduledTaskHandler : ICommandHandler<RunScheduledTask>
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RunScheduledTaskHandler"/> class.
    /// </summary>
    public RunScheduledTaskHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public async Task<Unit> HandleAsync(RunScheduledTask command, CancellationToken cancellationToken)
    {
        var taskExecutionContext = new TaskExecutionContext(_serviceProvider, cancellationToken);
        await command.Task.ExecuteAsync(taskExecutionContext);
        return Unit.Instance;
    }
}