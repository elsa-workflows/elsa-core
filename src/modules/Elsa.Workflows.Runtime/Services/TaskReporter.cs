using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Stimuli;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class TaskReporter(IStimulusSender stimulusSender) : ITaskReporter
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
        await stimulusSender.SendAsync<RunTask>(bookmarkPayload, sender, cancellationToken: cancellationToken);
    }
}