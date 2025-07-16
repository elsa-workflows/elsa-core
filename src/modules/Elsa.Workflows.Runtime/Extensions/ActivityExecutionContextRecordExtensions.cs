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
    
    public static ActivityExecutionRecord? GetCapturedActivityExecutionRecord(this ActivityExecutionContext context)
    {
        return context.TransientProperties.TryGetValue(ActivityExecutionRecordKey, out var record) ? (ActivityExecutionRecord?)record : null;
    }
}