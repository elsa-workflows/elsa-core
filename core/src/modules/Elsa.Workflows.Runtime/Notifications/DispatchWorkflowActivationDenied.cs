using Elsa.Mediator.Contracts;

namespace Elsa.Workflows.Runtime.Notifications;

/// <summary>
/// Signals that a queued child-workflow dispatch was accepted but activation was denied.
/// </summary>
internal sealed record DispatchWorkflowActivationDenied(
    string ParentWorkflowInstanceId,
    string WorkflowInstanceId,
    string ActivityTypeName,
    string StimulusHash) : INotification;

/// <summary>
/// Carries an internal bookmark-resume route through queued definition dispatch without persisting it on a child instance.
/// </summary>
internal static class DispatchWorkflowActivationDeniedRoute
{
    private const string ActivityTypeNameKey = "__elsa.internal.dispatch-activation-denied.activity-type-name";
    private const string StimulusHashKey = "__elsa.internal.dispatch-activation-denied.stimulus-hash";

    public static void Add(IDictionary<string, object> properties, string activityTypeName, string stimulusHash)
    {
        properties[ActivityTypeNameKey] = activityTypeName;
        properties[StimulusHashKey] = stimulusHash;
    }

    public static IDictionary<string, object>? SanitizeAndRead(IDictionary<string, object>? properties, out (string ActivityTypeName, string StimulusHash)? route)
    {
        route = null;

        if (properties == null)
            return null;

        if (!properties.ContainsKey(ActivityTypeNameKey) && !properties.ContainsKey(StimulusHashKey))
            return properties;

        // Do not mutate the caller's property bag: it may be shared with dispatch notification handlers.
        var sanitizedProperties = new Dictionary<string, object>(properties);
        var hasActivityTypeName = sanitizedProperties.Remove(ActivityTypeNameKey, out var activityTypeNameValue);
        var hasStimulusHash = sanitizedProperties.Remove(StimulusHashKey, out var stimulusHashValue);

        if (hasActivityTypeName && hasStimulusHash && activityTypeNameValue is string activityTypeName && !string.IsNullOrWhiteSpace(activityTypeName) && stimulusHashValue is string stimulusHash && !string.IsNullOrWhiteSpace(stimulusHash))
            route = (activityTypeName, stimulusHash);

        return sanitizedProperties;
    }
}
