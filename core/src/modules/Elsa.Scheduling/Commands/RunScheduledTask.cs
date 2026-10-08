using Elsa.Mediator.Contracts;

namespace Elsa.Scheduling.Commands;

/// <summary>
/// A command to run a scheduled task.
/// </summary>
public record RunScheduledTask(ITask Task) : ICommand;