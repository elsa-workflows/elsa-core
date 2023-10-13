using Elsa.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class DefaultActivityExecutionMapper : IActivityExecutionMapper
{
    /// <inheritdoc />
    public ActivityExecutionRecord Map(ActivityExecutionContext source)
    {
        // Get any outcomes that were added to the activity execution context.
        var outcomes = source.JournalData.TryGetValue("Outcomes", out var resultValue) ? resultValue as string[] : default;
        var payload = new Dictionary<string, object>();

        if (outcomes != null)
            payload.Add("Outcomes", outcomes);

        // Get any outputs that were added to the activity execution context.
        var activity = source.Activity;
        var expressionExecutionContext = source.ExpressionExecutionContext;
        var activityDescriptor = source.ActivityDescriptor;
        var outputDescriptors = activityDescriptor.Outputs;

        var outputs = outputDescriptors.ToDictionary(x => x.Name, x =>
        {
            if (x.IsSerializable == false)
                return "(not serializable)";

            var cachedValue = activity.GetOutput(expressionExecutionContext, x.Name);

            if (cachedValue != default)
                return cachedValue;

            if (x.ValueGetter(activity) is Output output && source.TryGet(output.MemoryBlockReference(), out var outputValue))
                return outputValue;

            return default;
        });

        return new ActivityExecutionRecord
        {
            Id = source.Id,
            ActivityId = source.Activity.Id,
            WorkflowInstanceId = source.WorkflowExecutionContext.Id,
            ActivityType = source.Activity.Type,
            ActivityName = source.Activity.Name,
            ActivityState = source.ActivityState,
            Outputs = outputs,
            Payload = payload,
            Exception = ExceptionState.FromException(source.Exception),
            ActivityTypeVersion = source.Activity.Version,
            StartedAt = source.StartedAt,
            HasBookmarks = source.Bookmarks.Any(),
            Status = GetAggregateStatus(source),
            CompletedAt = source.CompletedAt
        };
    }

    private ActivityStatus GetAggregateStatus(ActivityExecutionContext context)
    {
        // If any child activity is faulted, the aggregate status is faulted.
        var descendantContexts = context.GetDescendants().ToList();

        if (descendantContexts.Any(x => x.Status == ActivityStatus.Faulted))
            return ActivityStatus.Faulted;

        return context.Status;
    }
}