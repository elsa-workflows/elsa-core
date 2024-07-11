using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Stimuli;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class TaskReporter(IStimulusSender workflowDispatcher) : ITaskReporter
{
    /// <inheritdoc />
    public async Task ReportCompletionAsync(string taskId, object? result = default, CancellationToken cancellationToken = default)
    {
        var bookmarkPayload = new RunTaskStimulus(taskId, default!);

        var input = new Dictionary<string, object>
        {
            [RunTask.InputKey] = result!
        };

        var sender = new StimulusMetadata
        {
            Input = input,
        };
        await workflowDispatcher.SendAsync<RunTask>(bookmarkPayload, sender, cancellationToken: cancellationToken);
    }
}