using System.Net.Http.Json;
using System.Text.Json;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Signals;
using Microsoft.AspNetCore.DataProtection;

namespace NativeCompensation.E1;

// All activity types are internal to this non-packable process experiment.
[Activity("Elsa.E1")]
internal sealed class ProbeScope : Container
{
    public ProbeScope()
    {
        Id = "scope";
        Activities =
        [
            new ParticipantEffect(0, false), new ParticipantEffect(1, false), new Reject { Id = "reject" },
            new ParticipantEffect(1, true), new ParticipantEffect(0, true), new Handoff { Id = "handoff" }
        ];
        OnSignalReceived<FaultSignal>(OnFaultAsync);
    }

    protected override async ValueTask ScheduleChildrenAsync(ActivityExecutionContext context)
    {
        if (context.WorkflowExecutionContext.Properties.ContainsKey(Journal.PropertyKey))
            throw new InvalidOperationException("Scope activation cannot replace an existing obligation.");
        var journal = new Journal();
        if (Environment.GetEnvironmentVariable("E1_HANDOFF") == "Fault")
            journal.Handoff = "Fault";
        journal.Write(context.WorkflowExecutionContext);
        await ScheduleAsync(context, Activities.First());
        await context.GetRequiredService<Barriers>().ReachAsync("scope-activated", context.WorkflowExecutionContext);
    }

    private async ValueTask OnCompletedAsync(ActivityCompletedContext completed)
    {
        var context = completed.TargetContext;
        var workflow = context.WorkflowExecutionContext;
        var journal = Journal.Read(workflow.Properties);
        if (completed.ChildContext.Activity is Handoff)
        {
            journal.OutwardCount++;
            journal.Write(workflow);
            await context.CompleteActivityWithOutcomesAsync("Compensated");
            return;
        }

        var effect = (ParticipantEffect)completed.ChildContext.Activity;
        var entry = journal.Entries.Single(x => x.Slot == effect.Slot);
        if (!effect.Undo)
        {
            if (entry.CompletionOrdinal == null)
                entry.CompletionOrdinal = ++journal.CompletionOrdinal;
            entry.Status = "Applied";
            journal.Write(workflow);
            await ScheduleAsync(context, effect.Slot == 0 ? Activities.ElementAt(1) : Activities.ElementAt(2));
            await context.GetRequiredService<Barriers>().ReachAsync($"forward-ack-{effect.Slot}", workflow);
        }
        else
        {
            entry.Status = "Compensated";
            var next = journal.Entries.Where(x => x.Status == "Applied").OrderByDescending(x => x.CompletionOrdinal).FirstOrDefault();
            if (next == null)
                journal.Phase = "Compensated";
            journal.Write(workflow);
            await ScheduleAsync(context, next == null ? Activities.Last() : Activities.OfType<ParticipantEffect>().Single(x => x.Slot == next.Slot && x.Undo));
            await context.GetRequiredService<Barriers>().ReachAsync(next == null ? "settled-before-handoff" : "between-handlers", workflow);
        }
    }

    private async ValueTask OnFaultAsync(FaultSignal signal, SignalContext signalContext)
    {
        var context = signalContext.ReceiverActivityExecutionContext;
        if (signal.FaultedContext.Activity is Handoff)
            return; // Selected outward fault continues through the native incident path.
        signalContext.StopPropagation();
        var workflow = context.WorkflowExecutionContext;
        var journal = Journal.Read(workflow.Properties);
        var protector = CaptureProtection.For(context);
        if (signal.FaultedContext.Activity is ParticipantEffect)
        {
            journal.ProtectedRecoveryCause = protector.Protect(JsonSerializer.Serialize(new { type = signal.Exception.GetType().FullName, signal.Exception.Message }));
            journal.Phase = "AwaitingIntervention";
            journal.Write(workflow);
            await signal.FaultedContext.CancelActivityAsync();
            context.CreateBookmark("e1-intervention");
            await workflow.CommitAsync();
            return;
        }

        journal.ProtectedOriginalCause = protector.Protect(JsonSerializer.Serialize(new { type = signal.Exception.GetType().FullName, signal.Exception.Message }));
        journal.Phase = "Recovering";
        journal.FaultOwnershipReserved = true;
        journal.FaultedContextId = signal.FaultedContext.Id;
        journal.Write(workflow);
        await signal.FaultedContext.CancelActivityAsync();
        await ScheduleAsync(context, Activities.ElementAt(3));
        await context.GetRequiredService<Barriers>().ReachAsync("fault-owned-before-native-recovery", workflow);
        // Only ExceptionHandlingMiddleware calls RecoverFromFault, after this handler returns.
    }

    private async ValueTask ScheduleAsync(ActivityExecutionContext context, IActivity child)
    {
        if (child is ParticipantEffect { Undo: true } effect)
        {
            var journal = Journal.Read(context.WorkflowExecutionContext.Properties);
            var entry = journal.Entries.Single(x => x.Slot == effect.Slot);
            entry.Status = "Compensating";
            entry.AttemptId = journal.Key(effect.Slot, "undo-attempt-1");
            journal.Write(context.WorkflowExecutionContext);
        }
        await context.ScheduleActivityAsync(child, new ScheduleWorkOptions { CompletionCallback = OnCompletedAsync });
        await context.WorkflowExecutionContext.CommitAsync();
    }
}

[Activity("Elsa.E1")]
internal sealed class ParticipantEffect : Activity
{
    public ParticipantEffect() { }
    internal ParticipantEffect(int slot, bool undo)
    {
        Slot = slot;
        Undo = undo;
        Id = $"{(undo ? "undo" : "reserve")}-{slot}";
    }

    public int Slot { get; set; }
    public bool Undo { get; set; }
    [Input(CanContainSecrets = true)] public Input<string> Target { get; set; } = new("synthetic-resource");

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // This explicit effect-aware activity is the mandatory boundary in the experiment.
        // It is not a general invocation hook and does not qualify arbitrary activities.
        var target = Target.Get(context);
        var workflow = context.WorkflowExecutionContext;
        var barriers = context.GetRequiredService<Barriers>();
        var journal = Journal.Read(workflow.Properties);
        var protector = CaptureProtection.For(context);
        var entry = journal.Entries.SingleOrDefault(x => x.Slot == Slot);
        if (entry == null)
        {
            entry = new Entry
            {
                Slot = Slot, OccurrenceId = journal.Key(Slot, "occurrence"),
                OperationKey = journal.Key(Slot, "forward"), UndoKey = journal.Key(Slot, "undo"),
                ProtectedRequest = protector.Protect(JsonSerializer.Serialize(new { target, Slot })),
                AttemptId = journal.Key(Slot, "forward-attempt-1")
            };
            journal.Entries.Add(entry);
        }
        if (Undo)
            entry.UndoContextId = context.Id;
        else
            entry.ForwardContextId = context.Id;
        journal.Write(workflow);
        await barriers.ReachAsync($"before-intent-{Id}", workflow);
        await workflow.CommitAsync();
        await barriers.ReachAsync($"after-intent-{Id}", workflow);
        var frozenRequest = protector.Unprotect(entry.ProtectedRequest);
        var reservationId = Undo
            ? JsonSerializer.Deserialize<JsonElement>(protector.Unprotect(entry.ProtectedResult ?? throw new InvalidOperationException("Missing required captured result."))).GetProperty("reservationId").GetString()
            : null;
        using var response = await context.GetRequiredService<HttpClient>().PostAsJsonAsync(Undo ? "undo" : "reserve", new
        {
            operationKey = entry.OperationKey, undoKey = entry.UndoKey, reservationId, request = JsonSerializer.Deserialize<JsonElement>(frozenRequest)
        });
        response.EnsureSuccessStatusCode();
        var receipt = await response.Content.ReadAsStringAsync();
        await barriers.ReachAsync($"participant-committed-{Id}", workflow);
        if (Undo)
            entry.ProtectedUndoReceipt = protector.Protect(receipt);
        else
            entry.ProtectedResult = protector.Protect(receipt);
        journal.Write(workflow);
        await context.CompleteActivityAsync();
    }
}

[Activity("Elsa.E1")]
internal sealed class Reject : Activity
{
    protected override ValueTask ExecuteAsync(ActivityExecutionContext context) => throw new InvalidOperationException("Synthetic definite rejection; this activity has no external effect.");
}

[Activity("Elsa.E1")]
internal sealed class Handoff : Activity
{
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var journal = Journal.Read(context.WorkflowExecutionContext.Properties);
        if (journal.Phase != "Compensated" || journal.Entries.Any(x => x.Status != "Compensated"))
            throw new InvalidOperationException("Unresolved recovery cannot take an outward handoff.");
        if (journal.Handoff == "Fault")
            throw new RetainedForwardFailureException(journal.ProtectedOriginalCause!);
        await context.CompleteActivityAsync();
    }
}

// Safe wrapper retains an opaque protected cause reference; it never reconstructs a CLR exception type.
internal sealed class RetainedForwardFailureException(string protectedCause) : Exception("Retained synthetic forward failure after successful compensation.")
{
    internal string ProtectedCause { get; } = protectedCause;
}

internal static class CaptureProtection
{
    internal static IDataProtector For(ActivityExecutionContext context) => context.GetRequiredService<IDataProtectionProvider>()
        .CreateProtector("synthetic-tenant", Program.InstanceId, "e1-inline-capture-v1");
}
