using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.ProtoActor.Extensions;
using Elsa.Runtime.Protos;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Runtime.Services;
using Bookmark = Elsa.Workflows.Core.Models.Bookmark;

namespace Elsa.ProtoActor.Implementations;

public class ProtoActorWorkflowRuntime : IWorkflowRuntime
{
    private readonly GrainClientFactory _grainClientFactory;
    private readonly IIdentityGenerator _identityGenerator;

    public ProtoActorWorkflowRuntime(GrainClientFactory grainClientFactory, IIdentityGenerator identityGenerator)
    {
        _grainClientFactory = grainClientFactory;
        _identityGenerator = identityGenerator;
    }
    
    public async Task StartWorkflowAsync(string definitionId, RunWorkflowOptions options, CancellationToken cancellationToken = default)
    {
        var versionOptions = options.VersionOptions;
        var correlationId = options.CorrelationId;
        var input = options.Input;
        
        var request = new StartWorkflowRequest
        {
            DefinitionId = definitionId,
            VersionOptions = versionOptions.ToString(),
            CorrelationId = correlationId.WithDefault(""),
            Input = input?.Serialize()
        };
        
        var workflowInstanceId = _identityGenerator.GenerateId();
        var client = _grainClientFactory.CreateWorkflowGrainClient(workflowInstanceId);
        var response = await client.Start(request, cancellationToken);
    }

    public async Task ResumeWorkflowAsync(string definitionId, string instanceId, Bookmark bookmark, ResumeWorkflowOptions options, CancellationToken cancellationToken = default)
    {
        // var request = new ResumeWorkflowRequest
        // {
        //
        // };
    }
}