using Elsa.Mediator.Services;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Implementations;

public class WorkflowHost : IWorkflowHost
{
    private readonly IWorkflowRunner _workflowRunner;
    private readonly IEventPublisher _eventPublisher;
    private readonly IIdentityGenerator _identityGenerator;

    public WorkflowHost(Workflow workflow, WorkflowState workflowState, IWorkflowRunner workflowRunner, IEventPublisher eventPublisher, IIdentityGenerator identityGenerator)
    {
        Workflow = workflow;
        WorkflowState = workflowState;
        _workflowRunner = workflowRunner;
        _eventPublisher = eventPublisher;
        _identityGenerator = identityGenerator;
    }
    
    public Workflow Workflow { get; set; }
    public WorkflowState WorkflowState { get; set; }

    public async Task<StartWorkflowHostResult> StartWorkflowAsync(IDictionary<string, object>? input = default, CancellationToken cancellationToken = default)
    {
        var instanceId = _identityGenerator.GenerateId();
        return await StartWorkflowAsync(instanceId, input, cancellationToken);
    }

    public async Task<StartWorkflowHostResult> StartWorkflowAsync(string instanceId, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default)
    {
        await _eventPublisher.PublishAsync(new WorkflowExecuting(Workflow), cancellationToken);
        
        var originalBookmarks = WorkflowState.Bookmarks.ToList();
        var workflowResult = await _workflowRunner.RunAsync(instanceId, Workflow, input, cancellationToken);
        
        WorkflowState = workflowResult.WorkflowState;
        
        var updatedBookmarks = WorkflowState.Bookmarks;
        var diff = Diff.For(originalBookmarks, updatedBookmarks);
        
        await PublishChangedBookmarksAsync(diff, cancellationToken);
        await _eventPublisher.PublishAsync(new WorkflowExecuted(Workflow, WorkflowState), cancellationToken);

        return new StartWorkflowHostResult(diff);
    }

    public async Task<ResumeWorkflowHostResult> ResumeWorkflowAsync(string bookmarkId, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default)
    {
        await _eventPublisher.PublishAsync(new WorkflowExecuting(Workflow), cancellationToken);

        var originalBookmarks = WorkflowState.Bookmarks.ToList();
        var workflowResult = await _workflowRunner.RunAsync(Workflow, WorkflowState, bookmarkId, input, cancellationToken);
        
        WorkflowState = workflowResult.WorkflowState;
        
        var updatedBookmarks = WorkflowState.Bookmarks;
        var diff = Diff.For(originalBookmarks, updatedBookmarks);
        await PublishChangedBookmarksAsync(diff, cancellationToken);
        await _eventPublisher.PublishAsync(new WorkflowExecuted(Workflow, WorkflowState), cancellationToken);

        return new ResumeWorkflowHostResult(diff);
    }
    
    private async Task PublishChangedBookmarksAsync(Diff<Bookmark> diff, CancellationToken cancellationToken)
    {
        var removedBookmarks = diff.Removed;
        var createdBookmarks = diff.Added;
        await _eventPublisher.PublishAsync(new WorkflowBookmarksIndexed(new IndexedWorkflowBookmarks(WorkflowState, createdBookmarks, removedBookmarks)), cancellationToken);
    }
}