using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.Common.Models;
using Elsa.ProtoActor.Extensions;
using Elsa.Runtime.Protos;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Persistence.Entities;
using Elsa.Workflows.Persistence.Services;
using Elsa.Workflows.Runtime.Services;
using Proto;
using Bookmark = Elsa.Workflows.Core.Models.Bookmark;

namespace Elsa.ProtoActor.Grains;

/// <summary>
/// Executes a workflow instance.
/// </summary>
public class WorkflowGrain : WorkflowGrainBase
{
    private readonly IWorkflowInstanceStore _workflowInstanceStore;
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IWorkflowRunner _workflowRunner;
    private readonly IWorkflowInstanceFactory _workflowInstanceFactory;
    private readonly SerializerOptionsProvider _serializerOptionsProvider;
    private Workflow? _workflow;
    private WorkflowState? _workflowState;
    private IReadOnlyCollection<Bookmark>? _bookmarks;

    public WorkflowGrain(
        IWorkflowInstanceStore workflowInstanceStore,
        IWorkflowDefinitionStore workflowDefinitionStore,
        IWorkflowDefinitionService workflowDefinitionService,
        IWorkflowRunner workflowRunner,
        IWorkflowInstanceFactory workflowInstanceFactory,
        SerializerOptionsProvider serializerOptionsProvider,
        IContext context) : base(context)
    {
        _workflowInstanceStore = workflowInstanceStore;
        _workflowDefinitionStore = workflowDefinitionStore;
        _workflowDefinitionService = workflowDefinitionService;
        _workflowRunner = workflowRunner;
        _workflowInstanceFactory = workflowInstanceFactory;
        _serializerOptionsProvider = serializerOptionsProvider;
    }


    public override async Task<StartWorkflowResponse> Start(StartWorkflowRequest request)
    {
        var definitionId = request.DefinitionId;
        var versionOptions = VersionOptions.FromString(request.VersionOptions);
        var correlationId = request.CorrelationId == "" ? default : request.CorrelationId;
        var input = request.Input?.Deserialize();
        var cancellationToken = Context.CancellationToken;

        var workflowDefinition = await _workflowDefinitionService.FindAsync(definitionId, versionOptions, cancellationToken);

        if (workflowDefinition == null)
            return new StartWorkflowResponse
            {
                NotFound = true
            };
        
        _workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, cancellationToken);
        var workflowResult = await _workflowRunner.RunAsync(_workflow, input, cancellationToken);
        var finished = workflowResult.WorkflowState.Status == WorkflowStatus.Finished;

        _workflowState = workflowResult.WorkflowState;
        _bookmarks = workflowResult.Bookmarks;
        
        return new StartWorkflowResponse
        {
            Finished = finished
        };
    }
}