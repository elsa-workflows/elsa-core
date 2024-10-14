using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Stimuli;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class TaskReporter(IBookmarkQueue bookmarkQueue, IStimulusHasher stimulusHasher) : ITaskReporter
{
    private static readonly string ActivityTypeName = ActivityTypeNameHelper.GenerateTypeName<RunTask>();
    
    /// <inheritdoc />
    public async Task ReportCompletionAsync(string taskId, object? result = default, CancellationToken cancellationToken = default)
    {
        var stimulus = new RunTaskStimulus(taskId, default!);

        var input = new Dictionary<string, object>
        {
            [RunTask.InputKey] = result!
        };

        var bookmarkQueueItem = new NewBookmarkQueueItem
        {
            ActivityTypeName = ActivityTypeName,
            StimulusHash = stimulusHasher.Hash(ActivityTypeName, stimulus),
            Options = new ResumeBookmarkOptions
            {
                Input = input
            }
        };
        
        await bookmarkQueue.EnqueueAsync(bookmarkQueueItem, cancellationToken);
    }
}