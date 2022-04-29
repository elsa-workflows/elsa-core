using Elsa.Activities;
using Elsa.Management.Notifications;
using Elsa.Management.Services;
using Elsa.Mediator.Services;
using Elsa.Models;
using Elsa.Persistence.Commands;
using Elsa.Persistence.Models;
using Elsa.Persistence.Requests;
using Elsa.Services;

namespace Elsa.Management.Implementations
{
    public class WorkflowPublisher : IWorkflowPublisher
    {
        private readonly IMediator _mediator;
        private readonly IIdentityGenerator _identityGenerator;
        private readonly ISystemClock _systemClock;

        public WorkflowPublisher(IMediator mediator, IIdentityGenerator identityGenerator, ISystemClock systemClock)
        {
            _mediator = mediator;
            _identityGenerator = identityGenerator;
            _systemClock = systemClock;
        }

        public Workflow New()
        {
            var id = _identityGenerator.GenerateId();
            var definitionId = _identityGenerator.GenerateId();
            const int version = 1;

            return new Workflow(
                new WorkflowIdentity(definitionId, version, id),
                WorkflowPublication.LatestDraft,
                new WorkflowMetadata(CreatedAt: _systemClock.UtcNow),
                new Sequence(),
                new List<Variable>(),
                new Dictionary<string, object>(),
                new Dictionary<string, object>());
        }

        public async Task<Workflow?> PublishAsync(string definitionId, CancellationToken cancellationToken = default)
        {
            var workflow = await _mediator.RequestAsync(new FindWorkflowByDefinitionId(definitionId, VersionOptions.Latest), cancellationToken);

            if (workflow == null)
                return null;

            return await PublishAsync(workflow, cancellationToken);
        }

        public async Task<Workflow> PublishAsync(Workflow workflow, CancellationToken cancellationToken = default)
        {
            var identity = workflow.Identity;
            var definitionId = identity.DefinitionId;

            // Reset current latest and published definitions.
            var publishedAndOrLatestWorkflows = await _mediator.RequestAsync(new FindLatestAndPublishedWorkflows(definitionId), cancellationToken);

            foreach (var publishedAndOrLatestWorkflow in publishedAndOrLatestWorkflows)
            {
                publishedAndOrLatestWorkflow.Publication = WorkflowPublication.Draft;
                await _mediator.ExecuteAsync(new SaveWorkflowDefinition(publishedAndOrLatestWorkflow), cancellationToken);
            }

            workflow = workflow.Publication.IsPublished ? workflow.IncrementVersion() : workflow.WithPublished();

            workflow.WithLatest();
            workflow = Initialize(workflow);

            await _mediator.PublishAsync(new WorkflowPublishing(workflow), cancellationToken);
            await _mediator.ExecuteAsync(new SaveWorkflowDefinition(workflow), cancellationToken);
            await _mediator.PublishAsync(new WorkflowPublished(workflow), cancellationToken);
            return workflow;
        }

        public async Task<Workflow?> RetractAsync(string definitionId, CancellationToken cancellationToken = default)
        {
            var workflow = await _mediator.RequestAsync(new FindWorkflowByDefinitionId(definitionId, VersionOptions.Published), cancellationToken);

            if (workflow == null)
                return null;

            return await RetractAsync(workflow, cancellationToken);
        }

        public async Task<Workflow> RetractAsync(Workflow workflow, CancellationToken cancellationToken = default)
        {
            if (!workflow.Publication.IsPublished)
                throw new InvalidOperationException("Cannot unpublish an unpublished workflow definition.");

            workflow.WithPublished(false);
            workflow = Initialize(workflow);

            await _mediator.PublishAsync(new WorkflowRetracting(workflow), cancellationToken);
            await _mediator.ExecuteAsync(new SaveWorkflowDefinition(workflow), cancellationToken);
            await _mediator.PublishAsync(new WorkflowRetracted(workflow), cancellationToken);
            return workflow;
        }

        public async Task<Workflow?> GetDraftAsync(string definitionId, CancellationToken cancellationToken = default)
        {
            var workflow = await _mediator.RequestAsync(new FindWorkflowByDefinitionId(definitionId, VersionOptions.Latest), cancellationToken);

            if (workflow == null)
                return null;

            if (!workflow.Publication.IsPublished)
                return workflow;

            var draft = workflow.Clone();

            draft.Identity = new WorkflowIdentity(
                workflow.Identity.DefinitionId,
                workflow.Identity.Version + 1,
                _identityGenerator.GenerateId());
            
            draft.Publication = WorkflowPublication.LatestDraft;

            return draft;
        }

        public async Task<Workflow> SaveDraftAsync(Workflow workflow, CancellationToken cancellationToken = default)
        {
            var draft = workflow;
            var definitionId = workflow.Identity.DefinitionId;
            var latestVersion = await _mediator.RequestAsync(new FindWorkflowByDefinitionId(definitionId, VersionOptions.Latest), cancellationToken);

            if (latestVersion?.Publication is { IsPublished: true, IsLatest: true })
            {
                latestVersion = latestVersion.WithLatest(false);
                await _mediator.ExecuteAsync(new SaveWorkflowDefinition(latestVersion), cancellationToken);
            }

            draft.Publication = WorkflowPublication.LatestDraft;
            draft = Initialize(draft);

            await _mediator.ExecuteAsync(new SaveWorkflowDefinition(draft), cancellationToken);
            return draft;
        }

        public async Task DeleteAsync(string definitionId, CancellationToken cancellationToken = default)
        {
            await _mediator.ExecuteAsync(new DeleteWorkflowInstances(definitionId), cancellationToken);
            await _mediator.ExecuteAsync(new DeleteWorkflowDefinition(definitionId), cancellationToken);
        }

        public Task DeleteAsync(Workflow workflow, CancellationToken cancellationToken = default) => DeleteAsync(workflow.Identity.DefinitionId, cancellationToken);

        private Workflow Initialize(Workflow workflow)
        {
            if (workflow.Identity.Id == null!)
                workflow = workflow.WithId(_identityGenerator.GenerateId());

            if (workflow.Identity.DefinitionId == null!)
                workflow = workflow.WithDefinitionId(_identityGenerator.GenerateId());

            if (workflow.Identity.Version == 0)
                workflow = workflow.WithVersion(1);

            return workflow;
        }
    }
}