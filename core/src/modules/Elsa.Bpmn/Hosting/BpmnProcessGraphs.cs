using Bpmn.Semantics;
using Elsa.Bpmn.Activities;

namespace Elsa.Bpmn.Hosting;

/// <summary>
/// Builds the interpreter's graph of a BPMN process, and of every subprocess body it can reach, the way the scope host
/// builds each one when it runs it.
/// </summary>
/// <remarks>
/// Each subprocess body runs as its own scope with its own graph, built when the subprocess starts, so a body that
/// cannot run would be refused mid-run, after earlier work had already happened. A body carries no bound work of its own
/// that the enclosing graph can see, so only the host can reach the bodies nested inside it. Building every reachable
/// body up front refuses a broken one before anything runs, as <c>Bpmn.Semantics</c>' hosting guide asks. Public so
/// <c>Elsa.Bpmn.Interchange</c> refuses at import exactly what the runtime would refuse at its first run.
/// </remarks>
public static class BpmnProcessGraphs
{
    /// <summary>
    /// Builds the graph of <paramref name="process"/> and of every subprocess body it can reach, at any depth, and
    /// discards them.
    /// </summary>
    /// <exception cref="BpmnExecutionException">A process or body is structurally invalid; the message names it.</exception>
    /// <exception cref="BpmnCapabilityException">A process or body needs a capability this runtime does not declare.</exception>
    public static void BuildAll(BpmnProcess process)
    {
        var visited = new HashSet<BpmnProcess>(ReferenceEqualityComparer.Instance) { process };
        var pending = new Stack<BpmnProcess>();
        pending.Push(process);

        while (pending.TryPop(out var scope))
        {
            _ = Build(scope);

            foreach (var bindingRef in scope.WorkBindings.Keys)
            {
                if (scope.FindWorkActivity(bindingRef) is BpmnProcess nested && visited.Add(nested))
                    pending.Push(nested);
            }
        }
    }

    /// <summary>Builds the graph of <paramref name="process"/> alone.</summary>
    internal static BpmnGraph Build(BpmnProcess process)
    {
        var definition = process.Process
                         ?? throw new InvalidOperationException($"BPMN process activity '{process.Id}' has no process definition to execute.");

        // Every binding the definition declares, with the nested definition attached where the bound activity is
        // itself a BPMN scope. The graph validator reads that for an event subprocess body's start trigger.
        var boundWork = BpmnBoundWork.Derive(definition, bindingRef => (process.FindWorkActivity(bindingRef) as BpmnProcess)?.Process);

        return BpmnGraph.Build(definition, boundWork, BpmnRuntimeCapabilities.Declared);
    }
}
