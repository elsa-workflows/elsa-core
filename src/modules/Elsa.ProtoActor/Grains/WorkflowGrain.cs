using System.Text.Json;
using Elsa.Common.Models;
using Elsa.ProtoActor.Extensions;
using Elsa.Runtime.Protos;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Services;
using Proto;
using Proto.Cluster;
using Proto.Persistence;
using Bookmark = Elsa.Workflows.Core.Models.Bookmark;
using RunWorkflowResult = Elsa.Runtime.Protos.RunWorkflowResult;

namespace Elsa.ProtoActor.Grains;

/// <summary>
/// Executes a workflow.
/// </summary>
public class WorkflowGrain : WorkflowGrainBase
{
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IWorkflowHostFactory _workflowHostFactory;
    private readonly SerializerOptionsProvider _serializerOptionsProvider;
    private readonly Persistence _persistence;

    private string _definitionId = default!;
    private int _version;
    private IDictionary<string, object>? _input;
    private IWorkflowHost _workflowHost = default!;
    private WorkflowState _workflowState = default!;

    /// <inheritdoc />
    public WorkflowGrain(
        IWorkflowDefinitionService workflowDefinitionService,
        IWorkflowHostFactory workflowHostFactory,
        SerializerOptionsProvider serializerOptionsProvider,
        IProvider provider,
        IContext context) : base(context)
    {
        _workflowDefinitionService = workflowDefinitionService;
        _workflowHostFactory = workflowHostFactory;
        _serializerOptionsProvider = serializerOptionsProvider;
        _persistence = Persistence.WithSnapshotting(provider, WorkflowInstanceId, ApplySnapshot);
    }

    private string WorkflowInstanceId => Context.ClusterIdentity()!.Identity;

    /// <inheritdoc />
    public override async Task OnStarted()
    {
        await _persistence.RecoverStateAsync();

        if (string.IsNullOrWhiteSpace(_definitionId))
            return; // No state yet to recover from.

        var cancellationToken = Context.CancellationToken;

        // Load the workflow definition.
        var workflowDefinition = await _workflowDefinitionService.FindAsync(_definitionId, VersionOptions.SpecificVersion(_version), cancellationToken);

        if (workflowDefinition == null)
            throw new Exception("Workflow definition is no longer available");

        // Materialize the workflow.
        var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, cancellationToken);
        
        // Create an initial workflow state.
        if (_workflowState == null!)
        {
            _workflowState = new WorkflowState
            {
                DefinitionId = workflow.Identity.DefinitionId,
                DefinitionVersion = workflow.Identity.Version,
                //Bookmarks = _bookmarks
            };
        }
        
        // Create a workflow host.
        _workflowHost = await _workflowHostFactory.CreateAsync(workflow, _workflowState, cancellationToken);
    }

    public override async Task<StartWorkflowResponse> Start(StartWorkflowRequest request)
    {
        var definitionId = request.DefinitionId;
        var correlationId = request.CorrelationId == "" ? default : request.CorrelationId;
        var input = request.Input?.Deserialize();
        var versionOptions = VersionOptions.FromString(request.VersionOptions);
        var cancellationToken = Context.CancellationToken;
        var workflowDefinition = await _workflowDefinitionService.FindAsync(definitionId, versionOptions, cancellationToken);

        if (workflowDefinition == null)
            throw new Exception("Specified workflow definition and version does not exist");

        var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, cancellationToken);
        _version = workflow.Version;
        _definitionId = definitionId;
        _input = input;
        
        // Create a workflow host.
        _workflowHost = await _workflowHostFactory.CreateAsync(workflow, cancellationToken);

        var startWorkflowOptions = new StartWorkflowHostOptions(WorkflowInstanceId, correlationId, input);
        await _workflowHost.StartWorkflowAsync(startWorkflowOptions, cancellationToken);

        _workflowState = _workflowHost.WorkflowState;
        
        await SaveSnapshotAsync();

        return new StartWorkflowResponse
        {
            Result = _workflowHost.WorkflowState.Status == WorkflowStatus.Finished ? RunWorkflowResult.Finished : RunWorkflowResult.Suspended,
            Bookmarks = { Map(_workflowHost.WorkflowState.Bookmarks) }
        };
    }

    public override async Task<ResumeWorkflowResponse> Resume(ResumeWorkflowRequest request)
    {
        _input = request.Input?.Deserialize();
        var bookmarkId = request.BookmarkId;
        var cancellationToken = Context.CancellationToken;
        var resumeWorkflowHostOptions = new ResumeWorkflowHostOptions(_input);
        await _workflowHost.ResumeWorkflowAsync(bookmarkId, resumeWorkflowHostOptions, cancellationToken);
        var finished = _workflowHost.WorkflowState.Status == WorkflowStatus.Finished;

        _workflowState = _workflowHost.WorkflowState;
        
        await SaveSnapshotAsync();

        return new ResumeWorkflowResponse
        {
            Result = finished ? RunWorkflowResult.Finished : RunWorkflowResult.Suspended,
            Bookmarks = { Map(_workflowHost.WorkflowState.Bookmarks) }
        };
    }

    public override Task<ExportWorkflowStateResponse> ExportState(ExportWorkflowStateRequest request)
    {
        var options = _serializerOptionsProvider.CreatePersistenceOptions(); 
        var json = JsonSerializer.Serialize(_workflowHost.WorkflowState, options);
        
        var response = new ExportWorkflowStateResponse
        {
            SerializedWorkflowState = new Json
            {
                Text = json
            }
        };

        return Task.FromResult(response);
    }

    public override Task<ImportWorkflowStateResponse> ImportState(ImportWorkflowStateRequest request)
    {
        var options = _serializerOptionsProvider.CreatePersistenceOptions();
        var workflowState = JsonSerializer.Deserialize<WorkflowState>(request.SerializedWorkflowState.Text, options)!;

        _workflowState = workflowState;
        _workflowHost.WorkflowState = workflowState;
        
        return Task.FromResult(new ImportWorkflowStateResponse());
    }

    private void ApplySnapshot(Snapshot snapshot) => (_definitionId, _version, _workflowState, _input) = (WorkflowSnapshot)snapshot.State;
    private async Task SaveSnapshotAsync() => await _persistence.PersistSnapshotAsync(GetState());
    private object GetState() => new WorkflowSnapshot(_definitionId, _version, _workflowState, _input);
    
    private static IEnumerable<BookmarkDto> Map(IEnumerable<Bookmark> bookmarks) =>
        bookmarks.Select(x => new BookmarkDto
        {
            Id = x.Id,
            Name = x.Name,
            ActivityId = x.ActivityId,
            ActivityInstanceId = x.ActivityInstanceId,
            Hash = x.Hash,
            Data = x.Data.EmptyIfNull(),
            CallbackMethodName = x.CallbackMethodName.EmptyIfNull()
        });
}