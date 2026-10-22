using Elsa.Studio.Models;
using Elsa.Studio.Workflows.DiagramDesigners.Bpmn;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;

namespace Elsa.Studio.Workflows.Tests.Support;

/// <summary>
/// An in-memory <see cref="IBpmnXmlService"/>: each GET and PUT answers with the next result a test queued, the last one
/// repeating, and every PUT and create is recorded as it was sent.
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

    /// <summary>The name of every workflow a test created, in order.</summary>
    public List<string> Creates { get; } = [];

    /// <summary>What a create answers with; unset, a create is not expected and throws.</summary>
    public Result<BpmnXmlSaveResult, BpmnDocumentFailure>? CreateResult { get; set; }

    /// <summary>When set, a create awaits it before answering, so a test can act while one is in flight.</summary>
    public TaskCompletionSource? CreateGate { get; set; }

    /// <summary>When set, a create throws it, as a dropped connection would.</summary>
    public Exception? CreateException { get; set; }

    public async Task<Result<BpmnXmlSaveResult, BpmnDocumentFailure>> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        Creates.Add(name);

        if (CreateGate != null)
        {
            await CreateGate.Task;
        }

        if (CreateException != null)
        {
            throw CreateException;
        }

        return CreateResult ?? throw new NotSupportedException("No create result was set.");
    }

    private FakeBpmnXmlService Add<T>(List<T> results, T result)
    {
        results.Add(result);
        return this;
    }

    private static T Next<T>(List<T> results, int index) =>
        results.Count == 0 ? throw new InvalidOperationException("No result was queued for this call.") : results[Math.Min(index, results.Count - 1)];
}

/// <summary>A canvas whose designer exports <see cref="Xml"/>, or throws <see cref="SaveException"/>, and counts undo and redo.</summary>
internal sealed class FakeModelerCanvas : IBpmnModelerCanvas
{
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
}
