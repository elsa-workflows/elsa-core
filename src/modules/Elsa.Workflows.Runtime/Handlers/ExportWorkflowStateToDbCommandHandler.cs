// using Elsa.Common.Models;
// using Elsa.Extensions;
// using Elsa.Mediator.Contracts;
// using Elsa.Mediator.Models;
// using Elsa.Workflows.Core.Activities;
// using Elsa.Workflows.Management.Contracts;
// using Elsa.Workflows.Management.Entities;
// using Elsa.Workflows.Management.Filters;
// using Elsa.Workflows.Management.Notifications;
// using Elsa.Workflows.Runtime.Commands;
//
// namespace Elsa.Workflows.Runtime.Handlers;
//
// /// <summary>
// /// A default implementation that asynchronously stores the workflow state in a database using <see cref="IWorkflowInstanceStore"/>.
// /// </summary>
// internal class ExportWorkflowStateToDbCommandHandler : ICommandHandler<ExportWorkflowStateToDbCommand>
// {
//     private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
//     private readonly IWorkflowInstanceStore _workflowInstanceStore;
//     private readonly INotificationSender _notificationSender;
//
//     /// <summary>
//     /// Constructor.
//     /// </summary>
//     public ExportWorkflowStateToDbCommandHandler(
//         IWorkflowDefinitionStore workflowDefinitionStore,
//         IWorkflowInstanceStore workflowInstanceStore,
//         INotificationSender notificationSender
//     )
//     {
//         _workflowDefinitionStore = workflowDefinitionStore;
//         _workflowInstanceStore = workflowInstanceStore;
//         _notificationSender = notificationSender;
//     }
//
//     /// <inheritdoc />
//     public async Task<Unit> HandleAsync(ExportWorkflowStateToDbCommand command, CancellationToken cancellationToken)
//     {
//         var workflowState = command.WorkflowState;
//         //var instanceFilter = new WorkflowInstanceFilter { Id = workflowState.Id };
//         //var workflowInstance = await _workflowInstanceStore.FindAsync(instanceFilter, cancellationToken);
//
//         var workflowInstance = new WorkflowInstance
//         {
//             Id = workflowState.Id,
//             CreatedAt = workflowState.CreatedAt,
//             DefinitionId = workflowState.DefinitionId,
//             DefinitionVersionId = workflowState.DefinitionVersionId,
//             Version = workflowState.DefinitionVersion,
//             Status = workflowState.Status,
//             SubStatus = workflowState.SubStatus,
//             CorrelationId = workflowState.CorrelationId,
//             UpdatedAt = workflowState.UpdatedAt,
//             FinishedAt = workflowState.FinishedAt,
//             WorkflowState = workflowState
//         };
//
//         if (workflowState.Properties.TryGetValue<string>(SetName.WorkflowInstanceNameKey, out var name))
//             workflowInstance.Name = name;
//
//         await _workflowInstanceStore.SaveAsync(workflowInstance, cancellationToken);
//         await _notificationSender.SendAsync(new WorkflowInstanceSaved(workflowInstance), cancellationToken);
//         return Unit.Instance;
//     }
// }