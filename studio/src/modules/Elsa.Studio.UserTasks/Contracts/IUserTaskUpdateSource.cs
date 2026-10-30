using Elsa.Studio.UserTasks.Models;

namespace Elsa.Studio.UserTasks.Contracts;

public interface IUserTaskRealtimeClient : IAsyncDisposable
{
    event Func<UserTaskInvalidation, Task>? Invalidated;
    event Func<bool, Task>? ConnectionChanged;
    bool IsConnected { get; }
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync();
}

public interface IUserTaskPollingService
{
    Task RunAsync(TimeSpan interval, Func<CancellationToken, Task> refreshAsync, CancellationToken cancellationToken = default);
}
