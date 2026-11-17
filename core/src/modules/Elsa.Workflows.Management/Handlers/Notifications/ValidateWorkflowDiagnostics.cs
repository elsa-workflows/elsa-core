using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Notifications;

namespace Elsa.Workflows.Management.Handlers.Notifications;

internal sealed class ValidateWorkflowDiagnostics(IWorkflowGraphBuilder graphBuilder, IEnumerable<IWorkflowDiagnosticProvider> providers)
    : INotificationHandler<WorkflowDefinitionValidating>
{
    public async Task HandleAsync(WorkflowDefinitionValidating notification, CancellationToken cancellationToken)
    {
        var diagnostics = providers.ToArray();
        if (diagnostics.Length == 0)
            return;

        var graph = await graphBuilder.BuildAsync(notification.Workflow, cancellationToken);
        foreach (var diagnostic in diagnostics.SelectMany(provider => provider.GetDiagnostics(graph)))
            notification.ValidationErrors.Add(diagnostic);
    }
}
