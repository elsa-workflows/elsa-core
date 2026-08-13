namespace Elsa.Bpmn.Hosting;

/// <summary>
/// Runs BPMN scope evaluations one at a time, in arrival order, for one workflow instance.
/// </summary>
/// <remarks>
/// <para>
/// A nested scope can terminalize while its parent is still applying a command list, and a scope raising an
/// escalation delivers it to its parent from inside its own command loop. Either way the parent must not be
/// evaluated on the spot: doing so re-enters the interpreter with a half-applied live-work set and an evaluation
/// already in flight. Posting instead of calling is what keeps that from happening — a post made while the queue is
/// draining is appended and picked up once the evaluation in flight has fully applied.
/// </para>
/// <para>
/// One queue serves every scope in the instance, which is what makes the ordering total rather than per-scope.
/// It lives in the workflow execution context's transient properties: it coordinates a single burst of execution
/// and has nothing to persist.
/// </para>
/// </remarks>
internal sealed class BpmnScopeDispatcher
{
    private readonly Queue<Func<ValueTask>> _queue = new();

    private bool _draining;

    /// <summary>Whether an evaluation is in flight, so a post will be queued rather than run.</summary>
    public bool IsDraining => _draining;

    /// <summary>
    /// Queues an evaluation and, unless one is already in flight, drains the queue to exhaustion.
    /// </summary>
    public async ValueTask PostAsync(Func<ValueTask> evaluation)
    {
        _queue.Enqueue(evaluation);

        if (_draining)
            return;

        _draining = true;

        try
        {
            while (_queue.Count > 0)
                await _queue.Dequeue()();
        }
        catch
        {
            // An evaluation that threw may have applied part of a command list, so everything queued behind it was
            // computed against a state that no longer describes this instance. Dropping the rest is the conservative
            // direction: the failure is on its way to the incident strategy, and resuming half-planned work would
            // bury it under activity nobody asked for.
            _queue.Clear();
            throw;
        }
        finally
        {
            _draining = false;
        }
    }
}
