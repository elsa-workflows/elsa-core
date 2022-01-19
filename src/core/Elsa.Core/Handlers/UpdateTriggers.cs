using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Services;
using MediatR;

namespace Elsa.Handlers;

public class UpdateTriggers : INotificationHandler<WorkflowDefinitionPublished>,
                              INotificationHandler<WorkflowDefinitionDeleted>,
                              INotificationHandler<WorkflowDefinitionRetracted>,
                              INotificationHandler<ManyWorkflowDefinitionsDeleted>
{
    private readonly ITriggerIndexer _triggerIndexer;

    public UpdateTriggers(ITriggerIndexer triggerIndexer)
    {
        _triggerIndexer = triggerIndexer;
    }

    public async Task Handle(WorkflowDefinitionPublished notification, CancellationToken cancellationToken)
    {
        var workflowDefinition = notification.WorkflowDefinition;
        await _triggerIndexer.IndexTriggersAsync(workflowDefinition, cancellationToken);
    }

    public async Task Handle(WorkflowDefinitionDeleted notification, CancellationToken cancellationToken)
    {
        var workflowDefinition = notification.WorkflowDefinition;
        await _triggerIndexer.DeleteTriggersAsync(workflowDefinition.Id, cancellationToken);
    }

    public async Task Handle(WorkflowDefinitionRetracted notification, CancellationToken cancellationToken)
    {
        var workflowDefinition = notification.WorkflowDefinition;
        await _triggerIndexer.DeleteTriggersAsync(workflowDefinition.Id, cancellationToken);
    }

    public async Task Handle(ManyWorkflowDefinitionsDeleted notification, CancellationToken cancellationToken)
    {
        var workflowDefinition = notification.WorkflowDefinitions;
        var ids = workflowDefinition.Select(w => w.Id);
        await _triggerIndexer.DeleteTriggersAsync(ids, cancellationToken);
    }
}