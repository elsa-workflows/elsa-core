using Elsa.Workflows;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ActivityExecutionContextRecordExtensions
{
    private const string ActivityExecutionRecordKey = "CapturedActivityExecutionRecord";

    /// <summary>
    /// Captures the activity execution record for the provided <see cref="ActivityExecutionContext"/> and stores it in the context's transient properties.
    /// </summary>
    public static async Task CaptureActivityExecutionRecordAsync(this ActivityExecutionContext context)
    {
        var mapper = context.GetRequiredService<IActivityExecutionMapper>();
        var record = await mapper.MapAsync(context);
        context.TransientProperties[ActivityExecutionRecordKey] = record;
    }

    /// <summary>
    /// Retrieves the captured activity execution record from the transient properties of the provided <see cref="ActivityExecutionContext"/>.
    /// If the record is not found, it maps and returns a new activity execution record using the <see cref="IActivityExecutionMapper"/> service.
    /// </summary>
    public static async Task<ActivityExecutionRecord> GetOrMapCapturedActivityExecutionRecordAsync(this ActivityExecutionContext context)
    {
        // If the record is already captured in the transient properties, return it, as it will contain the serialized state of the activity execution at the time of capture, rather than the current state.
        // This is useful for scenarios where the activity execution state may change after the record is captured, such as referenced workflow variables.
        if (context.TransientProperties.TryGetValue(ActivityExecutionRecordKey, out var capturedRecord))
            return (ActivityExecutionRecord)capturedRecord;

        // If the record is not captured, map a new activity execution record using the mapper.
        var mapper = context.GetRequiredService<IActivityExecutionMapper>();
        var record = await mapper.MapAsync(context);

        return record;
    }
}