using Elsa.Extensions;

namespace Elsa.Workflows.Runtime;

public static class ActivityExecutionContextExtensions
{
    private static object LogPersistenceMapKey { get; } = new();

    public static ActivityLogPersistenceModeMap GetLogPersistenceModeMap(this ActivityExecutionContext context)
    {
        return context.TransientProperties.GetValueOrDefault(LogPersistenceMapKey, () => new ActivityLogPersistenceModeMap())!;
    }

    public static void SetLogPersistenceModeMap(this ActivityExecutionContext context, ActivityLogPersistenceModeMap map)
    {
        context.TransientProperties[LogPersistenceMapKey] = map;
    }
}