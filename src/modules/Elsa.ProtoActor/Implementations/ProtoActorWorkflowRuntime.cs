using System.Threading;
using System.Threading.Tasks;
using Elsa.ProtoActor.Extensions;
using Elsa.Runtime.Protos;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.ProtoActor.Implementations;

public class ProtoActorWorkflowRuntime : IWorkflowRuntime
{
    private readonly GrainClientFactory _grainClientFactory;
    private readonly IIdentityGenerator _identityGenerator;
    private readonly IHasher _hasher;

    public ProtoActorWorkflowRuntime(GrainClientFactory grainClientFactory, IIdentityGenerator identityGenerator, IHasher hasher)
    {
        _grainClientFactory = grainClientFactory;
        _identityGenerator = identityGenerator;
        _hasher = hasher;
    }

    public async Task<StartWorkflowResult> StartWorkflowAsync(string definitionId, StartWorkflowOptions options, CancellationToken cancellationToken = default)
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

        return new StartWorkflowResult(workflowInstanceId);
    }

    public async Task<ResumeWorkflowResult> ResumeWorkflowAsync(string instanceId, string bookmarkId, ResumeWorkflowOptions options, CancellationToken cancellationToken = default)
    {
        var request = new ResumeWorkflowRequest
        {
            InstanceId = instanceId,
            BookmarkId = bookmarkId,
            Input = options.Input?.Serialize()
        };

        var client = _grainClientFactory.CreateWorkflowGrainClient(instanceId);
        var response = await client.Resume(request, cancellationToken);

        return new ResumeWorkflowResult();
    }

    public async Task<TriggerWorkflowsResult> TriggerWorkflowsAsync(object bookmarkPayload, TriggerWorkflowsOptions options, CancellationToken cancellationToken = default)
    {
        var hash = _hasher.Hash(bookmarkPayload);
        var client = _grainClientFactory.CreateBookmarkGrainClient(hash);
        var bookmarksResponse = await client.Resolve(new ResolveBookmarkRequest(), cancellationToken);
        var bookmarks = bookmarksResponse!.Bookmarks;

        foreach (var bookmark in bookmarks)
        {
            var workflowInstanceId = bookmark.WorkflowInstanceId;
            var resumeResult = await ResumeWorkflowAsync(workflowInstanceId, bookmark.BookmarkId, new ResumeWorkflowOptions(options.Input), cancellationToken);    
        }
        
        return new TriggerWorkflowsResult();
    }
}