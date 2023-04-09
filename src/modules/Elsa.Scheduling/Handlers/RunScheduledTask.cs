using Elsa.Mediator.Contracts;
using Elsa.Scheduling.Contracts;

namespace Elsa.Scheduling.Handlers;

/// <summary>
/// A command to run a scheduled task.
/// </summary>
public record RunScheduledTask(ITask Task) : ICommand;