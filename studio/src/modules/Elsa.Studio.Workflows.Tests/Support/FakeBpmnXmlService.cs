using Elsa.Studio.Bpmn.Designers;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.DiagramDesigners.Bpmn;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;

namespace Elsa.Studio.Workflows.Tests.Support;

/// <summary>
/// An in-memory <see cref="IBpmnXmlService"/>: each GET and PUT answers with the next result a test queued, the last one
/// repeating, and every PUT is recorded as it was sent.
/// </summary>
internal sealed class FakeBpmnXmlService : IBpmnXmlService
{
    private readonly List<Result<BpmnXmlRevision, BpmnDocumentFailure>> _getResults = [];
    private readonly List<Result<BpmnXmlSaveResult, BpmnDocumentFailure>> _putResults = [];

    public List<(string DefinitionId, string Xml, string IfMatch)> Puts { get; } = [];
    public int GetCount { get; private set; }

    /// <summary>When set, a PUT awaits it before answering, so a test can act while one is in flight.</summary>
    public TaskCompletionSource? PutGate { get; set; }

    /// <summary>When set, a PUT throws it once it would answer, as a timeout would.</summary>
    public Exception? PutException { get; set; }

    public FakeBpmnXmlService ReturnsXml(string xml, string eTag) => Add(_getResults, new(new BpmnXmlRevision(xml, eTag)));
    public FakeBpmnXmlService RefusesGet(BpmnDocumentFailureReason reason) => Add(_getResults, new(new BpmnDocumentFailure(reason, "refused")));
    public FakeBpmnXmlService AcceptsPut(string? newETag, params BpmnForeignContentModel[] dropped) => Add(_putResults, new(new BpmnXmlSaveResult(new BpmnImportResultModel(), newETag, dropped)));
    public FakeBpmnXmlService RefusesPut(BpmnDocumentFailureReason reason) => Add(_putResults, new(new BpmnDocumentFailure(reason, "refused")));

    /// <summary>When set, a GET awaits it before answering, so a test can act while one is in flight.</summary>
    public TaskCompletionSource? GetGate { get; set; }

    public async Task<Result<BpmnXmlRevision, BpmnDocumentFailure>> GetXmlAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        var index = GetCount++;

        if (GetGate != null)
            await GetGate.Task;

        return Next(_getResults, index);
    }

    public async Task<Result<BpmnXmlSaveResult, BpmnDocumentFailure>> PutXmlAsync(string definitionId, string xml, string eTag, CancellationToken cancellationToken = default)
    {
        Puts.Add((definitionId, xml, eTag));

        if (PutGate != null)
            await PutGate.Task;

        if (PutException != null)
            throw PutException;

        return Next(_putResults, Puts.Count - 1);
    }

    public Task<Result<BpmnXmlSaveResult, BpmnDocumentFailure>> CreateAsync(string name, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    private FakeBpmnXmlService Add<T>(List<T> results, T result)
    {
        results.Add(result);
        return this;
    }

    private static T Next<T>(List<T> results, int index) =>
        results.Count == 0 ? throw new InvalidOperationException("No result was queued for this call.") : results[Math.Min(index, results.Count - 1)];
}

/// <summary>
/// A canvas whose designer exports <see cref="Xml"/>, or throws <see cref="SaveException"/>, counts undo and redo, and
/// keeps each element's binding in <see cref="Bindings"/>.
/// </summary>
internal sealed class FakeModelerCanvas : IBpmnModelerCanvas
{
    /// <summary>Each element's binding, as the designer's document holds it.</summary>
    public Dictionary<string, BpmnDesignerBinding> Bindings { get; } = new(StringComparer.Ordinal);

    /// <summary>Every binding the designer was asked to set, in order.</summary>
    public List<(string ElementId, BpmnDesignerBinding Binding)> SetBindings { get; } = [];

    /// <summary>When set, <c>setBinding</c> refuses with this reason instead of applying the binding.</summary>
    public string? SetBindingRefusal { get; set; }

    /// <summary>Raised after a binding is applied, as the designer raises <c>change</c>.</summary>
    public Action<string>? BindingApplied { get; set; }

    public string Xml { get; set; } = "<edited/>";
    public Exception? SaveException { get; set; }

    /// <summary>Runs while the designer exports, as an edit made during the export would.</summary>
    public Action? DuringSave { get; set; }

    public int Saves { get; private set; }
    public int Undos { get; private set; }
    public int Redos { get; private set; }

    public Task<string> SaveAsync()
    {
        Saves++;
        DuringSave?.Invoke();
        return SaveException != null ? Task.FromException<string>(SaveException) : Task.FromResult(Xml);
    }

    public Task UndoAsync()
    {
        Undos++;
        return Task.CompletedTask;
    }

    public Task RedoAsync()
    {
        Redos++;
        return Task.CompletedTask;
    }

    /// <summary>When set, the next <c>getBinding</c> reads the binding at once but returns it only once this completes.</summary>
    public TaskCompletionSource? NextGetBindingGate { get; set; }

    /// <summary>When set, the next <c>setBinding</c> applies the binding only once this completes.</summary>
    public TaskCompletionSource? NextSetBindingGate { get; set; }

    public async Task<BpmnDesignerBinding?> GetBindingAsync(string elementId)
    {
        var gate = NextGetBindingGate;
        NextGetBindingGate = null;
        var binding = Bindings.TryGetValue(elementId, out var found) ? found : null;

        if (gate != null)
            await gate.Task;

        return binding;
    }

    public async Task<BpmnDesignerSetBindingResult> SetBindingAsync(string elementId, BpmnDesignerBinding binding)
    {
        var gate = NextSetBindingGate;
        NextSetBindingGate = null;
        SetBindings.Add((elementId, binding));

        if (gate != null)
            await gate.Task;

        if (SetBindingRefusal != null)
            return new(false, SetBindingRefusal);

        Bindings[elementId] = binding;
        BindingApplied?.Invoke(elementId);
        return new(true);
    }
}
