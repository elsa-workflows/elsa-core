using Elsa.Workflows;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ActivityExecutionContextRecordExtensions
{
    private const string ActivityExecutionRecordKey = "CapturedActivityExecutionRecord";
    
    public static async Task CaptureActivityExecutionRecordAsync(this ActivityExecutionContext context)
    {
        var mapper = context.GetRequiredService<IActivityExecutionMapper>();
        var record = await mapper.MapAsync(context);
        context.TransientProperties[ActivityExecutionRecordKey] = record;
    }
    
    public static async Task<ActivityExecutionRecord> GetOrMapCapturedActivityExecutionRecordAsync(this ActivityExecutionContext context)
    {
        var mapper = context.GetRequiredService<IActivityExecutionMapper>();
        var record = await mapper.MapAsync(context);

        if (context.TransientProperties.TryGetValue(ActivityExecutionRecordKey, out var capturedRecord))
        {
            var serializedSnapshot = ((ActivityExecutionRecord)capturedRecord).SerializedSnapshot!;
            
            // Take the existing serialized snapshot.
            record.SerializedSnapshot = serializedSnapshot;
            
            // Update the serialized snapshot with the current record's properties.
            // This will reflect the latest state of the activity execution context without losing the existing serialized snapshot representing e.g., variable values at the time of the record capture.
            serializedSnapshot.HasBookmarks = record.HasBookmarks;
            serializedSnapshot.Status = record.Status;
            serializedSnapshot.AggregateFaultCount = record.AggregateFaultCount;
            serializedSnapshot.CompletedAt = record.CompletedAt;
        }

        return record;
    }
}