namespace Elsa.Workflows.Signals;

/// <summary>
/// Sent up the ancestor chain when an activity faulted, giving an enclosing container the chance to handle the failure
/// itself instead of letting it fall through to the workflow-global <see cref="IIncidentStrategy"/>.
/// </summary>
/// <remarks>
/// <para>
/// A container opts in exactly the way it opts into cancellation, by registering a handler for this signal:
/// </para>
/// <code>
/// public MyContainer()
/// {
///     OnSignalReceived&lt;FaultSignal&gt;(OnChildFaultedAsync);
/// }
///
/// private async ValueTask OnChildFaultedAsync(FaultSignal signal, SignalContext context)
/// {
///     if (!IsMine(signal.FaultedContext))
///         return;                        // Not my child: let it keep bubbling.
///
///     context.StopPropagation();         // I am handling this.
///     await signal.FaultedContext.CancelActivityAsync();
///     await ScheduleFallbackAsync(context.ReceiverActivityExecutionContext);
/// }
/// </code>
/// <para>
/// Bubbling is the default. A handler that does not recognize <see cref="FaultedContext"/> simply returns without
/// stopping propagation, and the signal continues to the next ancestor, so nested containers compose without any of
/// them knowing about the others. If no ancestor stops propagation, the incident strategy runs exactly as it does
/// when this signal does not exist at all.
/// </para>
/// <para>
/// <b>The contract.</b> Responsibilities are split, and the split is deliberate:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///     <b>The middleware</b> calls <c>Fault()</c>, sends this signal, and — only when propagation was stopped — calls
///     <c>RecoverFromFault()</c> exactly once. Nothing else.
///     </description>
///   </item>
///   <item>
///     <description>
///     <b>The handler</b> decides the fault is its own, calls <see cref="SignalContext.StopPropagation"/>, and then
///     terminalizes the faulted activity: cancel it, complete it, or reschedule it. There is no single right answer
///     for the middleware to pick here — an interrupting error boundary wants the activity cancelled, a retry handler
///     wants it rescheduled, a fallback handler may want it completed with a substitute result — so a handler that
///     claims the failure also owns deciding what becomes of the failed work.
///     </description>
///   </item>
///   <item>
///     <description>
///     <b>The handler must not</b> call <c>RecoverFromFault()</c>.
///     </description>
///   </item>
/// </list>
/// <para>
/// That last rule is not stylistic. <c>RecoverFromFault()</c> is asymmetric: it <i>sets</i> the faulting context's
/// <see cref="ActivityExecutionContext.AggregateFaultCount"/> to zero, which is idempotent, but <i>decrements</i> the
/// count on every ancestor, which is not. A second call is therefore harmless for the faulting context and harmful for
/// every ancestor, driving their counts to <c>-1</c>. Those counts are persisted and surface through
/// <c>ActivityExecutionRecord.AggregateFaultCount</c> and <c>ActivityExecutionStats</c>, so the failure mode is
/// silently wrong fault numbers in the UI rather than an exception anyone would notice.
/// </para>
/// <para>
/// <b>On the faulted activity's status.</b> <c>RecoverFromFault()</c> transitions the activity out of
/// <see cref="ActivityStatus.Faulted"/> and back to <see cref="ActivityStatus.Running"/>, so a handler that stops
/// propagation and terminalizes nothing leaves the activity <see cref="ActivityStatus.Running"/> having already thrown.
/// That is the handler's bug. A completing container happens to sweep such an activity via
/// <see cref="ActivityExecutionContext.CompleteActivityAsync"/>, which cancels its non-completed children, so the
/// mistake degrades rather than hangs — but that is a backstop, not the mechanism, and a container that suspends
/// instead of completing will persist the activity as <see cref="ActivityStatus.Running"/>.
/// </para>
/// </remarks>
/// <param name="Exception">The exception that caused the fault.</param>
/// <param name="FaultedContext">The <see cref="ActivityExecutionContext"/> of the activity that faulted.</param>
public record FaultSignal(Exception Exception, ActivityExecutionContext FaultedContext);
