using System.Xml;
using Bpmn.Interchange;
using Bpmn.Model;
using Elsa.Bpmn.Activities;
using Elsa.Bpmn.Interchange.Exceptions;
using Elsa.Scheduling.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Runtime.Activities;

namespace Elsa.Bpmn.Interchange.Binding;

/// <summary>
/// Turns the reader's <see cref="BpmnWorkBinding"/> declarations into the Elsa activities a
/// <see cref="BpmnProcess"/> scope runs. This is the seam where BPMN vocabulary becomes Elsa work.
/// </summary>
/// <remarks>
/// <para>
/// Six of the seven binding kinds bind to an activity this repository already has. Only
/// <see cref="BpmnWorkBinding.UnboundTask"/> is an authoring decision, which is right: BPMN genuinely does not say
/// what a <c>serviceTask</c> does. Its answer is read from the <c>elsa:activityBinding</c> extension element inside
/// the document — see <see cref="BpmnActivityBindingFormat"/>, including what exporting one discloses.
/// </para>
/// <para>
/// <b>Every binding for a scope is bound, whatever its slot.</b> A <see cref="BpmnBindingSlot.ScopeListener"/>
/// binding — the listener an event subprocess arms while its enclosing scope runs — is an entry in the same
/// <see cref="BpmnProcess.WorkBindings"/> map as any other, under its own binding ref. It needs no special case here
/// because the interpreter arms it at scope start on its own; singling it out would be a second code path with
/// nothing different to do.
/// </para>
/// <para>
/// <b>Distinct activity instances per scope.</b> A binding ref is unique within a scope, not across scopes, so two
/// scopes can legitimately declare the same one. Every binding gets its own freshly constructed activity, nothing is
/// cached or reused between scopes, and each activity is given a scope-qualified id. That is not a tidiness point:
/// <c>ActivityVisitor</c> collects activities into a set and skips one it has already seen, so a single instance
/// appearing under two scopes becomes one node in Elsa's identity graph and the second scope's child is missing from
/// it — a graph that builds, publishes, and then never runs half the process.
/// </para>
/// <para>
/// This binder lives in <c>Elsa.Bpmn.Interchange</c> rather than <c>Elsa.Bpmn</c> because <see cref="BpmnWorkBinding"/>
/// is a <c>Bpmn.Interchange</c> type: binding it in <c>Elsa.Bpmn</c> would pull the interchange library into the
/// execution module's dependency closure, which is exactly the package split D12 draws. The direction it needs is
/// available — <c>Elsa.Bpmn.Interchange</c> already references <c>Elsa.Bpmn</c>, so <see cref="BpmnProcess"/> and the
/// four activity targets are all in reach.
/// </para>
/// <para>
/// Nothing here decides root position: <see cref="BpmnProcess.IsRootScope"/> is left off on every scope this produces,
/// including the outermost one, so a bound process is safe to nest by default and whoever composes it into a workflow
/// says explicitly that it is an entry point.
/// </para>
/// </remarks>
public sealed class BpmnWorkBinder(BpmnActivityBindingFormat format)
{
    /// <summary>
    /// Binds one process definition, and every process nested inside it, into a <see cref="BpmnProcess"/> scope.
    /// </summary>
    /// <param name="definition">The process to bind.</param>
    /// <param name="bindings">Every binding the read produced, across all processes in the document.</param>
    /// <exception cref="BpmnBindingException">A binding cannot be turned into an activity.</exception>
    public BpmnProcess Bind(BpmnProcessDefinition definition, IReadOnlyCollection<BpmnWorkBinding> bindings)
    {
        var scope = BindScope(definition, bindings);
        scope.Id = definition.ProcessId;
        return scope;
    }

    private BpmnProcess BindScope(BpmnProcessDefinition definition, IReadOnlyCollection<BpmnWorkBinding> bindings)
    {
        var scope = new BpmnProcess
        {
            Process = definition
        };

        // A document-declared variable is invisible to the interpreter's IBpmnVariableReader port until it resolves
        // through Elsa's own ExpressionExecutionContext.GetVariable, which walks Container.Variables — not
        // BpmnProcessDefinition.Variables. Declaring one here for each is what makes a collection-mode multi-instance
        // over a document-declared collection readable instead of Absent. The declared default, when there is one,
        // travels as the JsonElement it already is: the reader serializes whatever the memory block holds, so nothing
        // here needs to interpret BpmnVariableDeclaration.TypeHint.
        foreach (var declaration in definition.Variables)
        {
            scope.Variables.Add(new Variable(declaration.Name, declaration.DefaultValue is { } defaultValue ? (object)defaultValue : null));
        }

        // Element ids whose elsa:activityBinding was actually used. A declaration nothing consumed is refused below.
        var consumed = new HashSet<string>(StringComparer.Ordinal);

        foreach (var binding in bindings.Where(binding => string.Equals(binding.ProcessId, definition.ProcessId, StringComparison.Ordinal)))
        {
            var activity = CreateActivity(definition, binding, bindings, consumed);

            // Scope-qualified and deterministic. The binding ref alone is unique only within its scope, and an id that
            // repeats across scopes gives two logical positions one identity in bookmarks and persisted state.
            activity.Id = $"{binding.ProcessId}:{binding.BindingRef}";

            scope.Activities.Add(activity);
            scope.WorkBindings[binding.BindingRef] = activity.Id;
        }

        RefuseUnusedDeclarations(definition, consumed);

        return scope;
    }

    private IActivity CreateActivity(BpmnProcessDefinition definition, BpmnWorkBinding binding, IReadOnlyCollection<BpmnWorkBinding> bindings, ISet<string> consumed) =>
        binding switch
        {
            BpmnWorkBinding.TimerWait timer => new Delay(IsoDurationOf(timer)),
            BpmnWorkBinding.MessageWait message => new Event(message.MessageName),
            BpmnWorkBinding.SignalWait signal => new Event(signal.SignalName),
            BpmnWorkBinding.MessagePublish publish => new PublishEvent
            {
                EventName = new(publish.MessageName)
            },
            BpmnWorkBinding.CallProcess call => new DispatchWorkflow
            {
                WorkflowDefinitionId = new(CalledElementOf(call)),
                WaitForCompletion = new(call.WaitForCompletion)
            },
            BpmnWorkBinding.NestedProcess nested => BindScope(nested.Definition, bindings),
            BpmnWorkBinding.UnboundTask unbound => ReadDeclaredActivity(definition, unbound, consumed),
            // The binding hierarchy is closed, so this is reachable only from a library version that added a kind this
            // binder has never heard of. Skipping it would produce a scope whose interpreter starts work nothing maps.
            _ => throw new BpmnBindingException($"The BPMN work binding kind '{binding.GetType().Name}' declared by element '{binding.ElementId}' is not supported by this binder.")
        };

    private IActivity ReadDeclaredActivity(BpmnProcessDefinition definition, BpmnWorkBinding.UnboundTask unbound, ISet<string> consumed)
    {
        var element = definition.Elements.FirstOrDefault(element => string.Equals(element.ElementId, unbound.ElementId, StringComparison.Ordinal));

        if (BpmnActivityBindingFormat.Find(element?.Extensions) is not { } declaration)
        {
            throw new BpmnBindingException(
                $"BPMN element '{unbound.ElementId}' of process '{unbound.ProcessId}' is a '{unbound.TaskType}': the document says what it is for, not how to perform it, and nothing binds it to an Elsa activity. "
                + $"Declare one with an <{BpmnActivityBindingFormat.NamespacePrefix}:{BpmnActivityBindingFormat.BindingElementName}> element inside the element's <extensionElements>.");
        }

        consumed.Add(unbound.ElementId);

        return format.Read(declaration);
    }

    /// <summary>
    /// Refuses an <c>elsa:activityBinding</c> on an element that has no unbound task to bind.
    /// </summary>
    /// <remarks>
    /// Six of the seven kinds bind on their own and never consult a declaration, so one written on a timer, a
    /// subprocess or a gateway configures nothing. Ignoring it is the quiet answer: the author sees their expression in
    /// the file, the process runs, and the activity they configured never executes. Refusing says so.
    /// </remarks>
    private static void RefuseUnusedDeclarations(BpmnProcessDefinition definition, ISet<string> consumed)
    {
        foreach (var element in definition.Elements.Where(element => BpmnActivityBindingFormat.Find(element.Extensions) is not null && !consumed.Contains(element.ElementId)))
        {
            throw new BpmnBindingException(
                $"BPMN element '{element.ElementId}' ({element.ElementType}) of process '{definition.ProcessId}' carries an <{BpmnActivityBindingFormat.NamespacePrefix}:{BpmnActivityBindingFormat.BindingElementName}> element, but its work is not an unbound task, so nothing would ever run it. "
                + "Only a task the document describes without implementing takes an authored activity binding.");
        }
    }

    private static TimeSpan IsoDurationOf(BpmnWorkBinding.TimerWait timer)
    {
        try
        {
            return XmlConvert.ToTimeSpan(timer.IsoDuration);
        }
        catch (Exception exception) when (exception is FormatException or OverflowException or ArgumentNullException)
        {
            throw new BpmnBindingException($"BPMN element '{timer.ElementId}' declares the timer duration '{timer.IsoDuration}', which is not an ISO-8601 duration Elsa can wait for.");
        }
    }

    private static string CalledElementOf(BpmnWorkBinding.CallProcess call) =>
        !string.IsNullOrWhiteSpace(call.CalledElement)
            ? call.CalledElement
            : throw new BpmnBindingException($"BPMN element '{call.ElementId}' is a call activity that names no calledElement, so there is no workflow definition to dispatch.");
}
