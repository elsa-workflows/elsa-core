using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// Reports the status of a given task, which will result in <see cref="RunTask"/> activities being resumed.
/// </summary>
public interface ITaskReporter
{
    /// <summary>
    /// Triggers the <see cref="RunTask"/> activity.
    /// </summary>
    Task ReportCompletionAsync(string taskId, object? result = default, CancellationToken cancellationToken = default);
}