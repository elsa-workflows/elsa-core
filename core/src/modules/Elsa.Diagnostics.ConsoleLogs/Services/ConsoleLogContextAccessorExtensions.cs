namespace Elsa.Diagnostics.ConsoleLogs.Services;

internal static class ConsoleLogContextAccessorExtensions
{
    public static IDisposable PushWorkflowExecution(this IConsoleLogContextAccessor accessor, WorkflowExecutionContext context)
    {
        var identity = context.Workflow.Identity;
        return new CompositeScope(
            accessor.PushMetadata(ConsoleLogMetadataKeys.WorkflowInstanceId, context.Id),
            accessor.PushMetadata(ConsoleLogMetadataKeys.WorkflowDefinitionId, identity.DefinitionId),
            accessor.PushMetadata(ConsoleLogMetadataKeys.WorkflowDefinitionVersionId, identity.Id));
    }

    public static IDisposable PushActivityExecution(this IConsoleLogContextAccessor accessor, ActivityExecutionContext context)
    {
        return new CompositeScope(
            accessor.PushWorkflowExecution(context.WorkflowExecutionContext),
            PushMetadataIfNotEmpty(accessor, ConsoleLogMetadataKeys.ActivityInstanceId, context.Id),
            PushMetadataIfNotEmpty(accessor, ConsoleLogMetadataKeys.ActivityId, context.Activity.Id),
            PushMetadataIfNotEmpty(accessor, ConsoleLogMetadataKeys.ActivityNodeId, context.NodeId));
    }

    private static IDisposable PushMetadataIfNotEmpty(IConsoleLogContextAccessor accessor, string key, string? value) =>
        string.IsNullOrWhiteSpace(value) ? EmptyScope.Instance : accessor.PushMetadata(key, value);

    private sealed class EmptyScope : IDisposable
    {
        public static readonly EmptyScope Instance = new();

        private EmptyScope()
        {
        }

        public void Dispose()
        {
        }
    }

    private sealed class CompositeScope(params IDisposable[] scopes) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            for (var i = scopes.Length - 1; i >= 0; i--)
                scopes[i].Dispose();

            _disposed = true;
        }
    }
}
