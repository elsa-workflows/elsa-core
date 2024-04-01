using System.Diagnostics.CodeAnalysis;
using Elsa.Extensions;
using Elsa.Workflows.Activities.Flowchart.Models;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Signals;

namespace Elsa.Workflows;

public partial class ActivityExecutionContext
{
    /// <summary>
    /// Complete the current activity. This should only be called by activities that explicitly suppress automatic-completion.
    /// </summary>
    [RequiresUnreferencedCode("The activity may be serialized and executed in a different context.")]
    public async ValueTask CompleteActivityAsync(object? result = default)
    {
        var outcomes = result as Outcomes;

        // If the activity is executing in the background, simply capture the result and return.
        if (this.GetIsBackgroundExecution())
        {
            if (outcomes != null)
                this.SetBackgroundOutcomes(outcomes.Names);
            else
                this.SetBackgroundCompletion();
            return;
        }

        // If the activity is not running, do nothing.
        if (Status != ActivityStatus.Running)
            return;

        // Cancel any non-completed child activities.
        var childContexts = WorkflowExecutionContext.ActivityExecutionContexts.Where(x => x.ParentActivityExecutionContext == this && x.CanCancelActivity()).ToList();

        foreach (var childContext in childContexts)
            await childContext.CancelActivityAsync();

        // Mark the activity as complete.
        TransitionTo(ActivityStatus.Completed);

        // Record the outcomes, if any.
        if (outcomes != null)
            JournalData["Outcomes"] = outcomes.Names;

        // Record the output, if any.
        var activity = Activity;
        var expressionExecutionContext = ExpressionExecutionContext;
        var activityDescriptor = ActivityDescriptor;
        var outputDescriptors = activityDescriptor.Outputs;
        var outputs = outputDescriptors.ToDictionary(x => x.Name, x => activity.GetOutput(expressionExecutionContext, x.Name)!);
        var serializer = GetRequiredService<ISafeSerializer>();

        foreach (var outputDescriptor in outputDescriptors)
        {
            if (outputDescriptor.IsSerializable == false)
                continue;

            var outputName = outputDescriptor.Name;
            var outputValue = outputs[outputName];

            if (outputValue == null!)
                continue;

            var serializedOutputValue = await serializer.SerializeAsync(outputValue, CancellationToken);
            JournalData[outputName] = serializedOutputValue;
        }

        // Add an execution log entry.
        AddExecutionLogEntry("Completed", payload: JournalData);

        // Send a signal.
        await this.SendSignalAsync(new ActivityCompleted(result));

        // Clear bookmarks.
        ClearBookmarks();
        WorkflowExecutionContext.Bookmarks.RemoveWhere(x => x.ActivityInstanceId == Id);

        // Remove completion callbacks.
        ClearCompletionCallbacks();

        // Remove all associated variables, unless this is the root context - in which case we want to keep the variables since we're not deleting that one.
        if (ParentActivityExecutionContext != null)
        {
            var variablePersistenceManager = GetRequiredService<IVariablePersistenceManager>();
            await variablePersistenceManager.DeleteVariablesAsync(this);
        }

        // Update the completed at timestamp.
        CompletedAt = WorkflowExecutionContext.SystemClock.UtcNow;
    }

    /// <summary>
    /// Complete the current activity with the specified outcomes.
    /// </summary>
    [RequiresUnreferencedCode("The activity may be serialized and executed in a different context.")]
    public ValueTask CompleteActivityWithOutcomesAsync(params string[] outcomes)
    {
        return CompleteActivityAsync(new Outcomes(outcomes));
    }

    /// <summary>
    /// Complete the current composite activity with the specified outcome.
    /// </summary>
    public async ValueTask CompleteCompositeAsync(params string[] outcomes)
    {
        await this.SendSignalAsync(new CompleteCompositeSignal(new Outcomes(outcomes)));
    }
}