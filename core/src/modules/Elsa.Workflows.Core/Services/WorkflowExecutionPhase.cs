using System.Collections.Concurrent;

namespace Elsa.Workflows;

// Exact references only. The built-in runner opens this internal lane after revalidation;
// no public API, context property, AsyncLocal or serialized token can open it.
internal static class WorkflowExecutionPhase
{
    private static readonly ConcurrentDictionary<WorkflowExecutionContext, byte> Active = new(ReferenceEqualityComparer.Instance);

    public static bool Contains(WorkflowExecutionContext context) => Active.ContainsKey(context);

    public static IDisposable Enter(WorkflowExecutionContext context)
    {
        if (!Active.TryAdd(context, 0))
        {
            throw new InvalidOperationException("An authorized workflow context is already executing.");
        }
        return new Phase(context);
    }

    private sealed class Phase(WorkflowExecutionContext context) : IDisposable
    {
        private int _disposed;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                Active.TryRemove(context, out _);
            }
        }
    }
}
