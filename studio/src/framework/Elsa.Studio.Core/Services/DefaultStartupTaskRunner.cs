using Elsa.Studio.Contracts;

namespace Elsa.Studio.Services;

/// <inheritdoc />
public class DefaultStartupTaskRunner : IStartupTaskRunner
{
    private readonly IEnumerable<IStartupTask> _startupTasks;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="startupTasks"></param>
    public DefaultStartupTaskRunner(IEnumerable<IStartupTask> startupTasks)
    {
        _startupTasks = startupTasks;
    }

    /// <inheritdoc />
    public async Task RunStartupTasksAsync(CancellationToken cancellationToken = default)
    {
        foreach (var task in _startupTasks)
            await task.ExecuteAsync(cancellationToken);
    }
}