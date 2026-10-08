using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.Handlers.Notifications;

/// <summary>
/// Validates output-converter bindings before a workflow definition is accepted.
/// </summary>
public class ValidateOutputConverters(
    IWorkflowGraphBuilder workflowGraphBuilder,
    IActivityRegistryLookupService activityRegistryLookupService,
    IOutputConverterRegistry outputConverterRegistry,
    IOutputBindingDestinationResolver destinationResolver,
    IOutputConverterSettingsValidator settingsValidator,
    IServiceProvider serviceProvider) : INotificationHandler<WorkflowDefinitionValidating>
{
    /// <inheritdoc />
    public async Task HandleAsync(WorkflowDefinitionValidating notification, CancellationToken cancellationToken)
    {
        var graph = await workflowGraphBuilder.BuildAsync(notification.Workflow, cancellationToken);

        foreach (var node in graph.Nodes)
        {
            var activity = node.Activity;
            var activityDescriptor = await activityRegistryLookupService.FindAsync(activity.Type, activity.Version);

            if (activityDescriptor == null)
                continue;

            foreach (var outputDescriptor in activityDescriptor.Outputs)
            {
                if (outputDescriptor.ValueGetter(activity) is not Output { Converter: { } configuration } output)
                    continue;

                ValidateBinding(
                    graph,
                    node,
                    output,
                    outputDescriptor,
                    configuration,
                    notification.ValidationErrors);
            }
        }
    }

    private void ValidateBinding(
        WorkflowGraph graph,
        ActivityNode node,
        Output output,
        OutputDescriptor outputDescriptor,
        OutputConverterConfiguration configuration,
        ICollection<WorkflowValidationError> validationErrors)
    {
        var activity = node.Activity;

        void AddError(string message) =>
            validationErrors.Add(new($"Output '{outputDescriptor.Name}' converter: {message}", activity.Id));

        if (string.IsNullOrWhiteSpace(configuration.Id))
        {
            AddError("a converter ID is required.");
            return;
        }

        var destination = destinationResolver.Resolve(graph, node, output);

        if (destination == null)
        {
            AddError("a converter can only be configured when the output has a variable or workflow-output destination.");
            return;
        }

        var registration = outputConverterRegistry.FindRegistration(configuration.Id);

        if (registration == null)
        {
            AddError($"converter '{configuration.Id}' is not registered.");
            return;
        }

        var converterDescriptor = registration.Descriptor;

        if (!converterDescriptor.SourceType.IsAssignableFrom(outputDescriptor.Type))
            AddError($"converter '{configuration.Id}' cannot accept declared source type '{outputDescriptor.Type.FullName}'.");

        if (!CanAssign(converterDescriptor.ResultType, destination.Type))
            AddError($"converter '{configuration.Id}' result type '{converterDescriptor.ResultType.FullName}' cannot be assigned to destination type '{destination.Type.FullName}'.");

        IOutputConverter converter;

        try
        {
            converter = serviceProvider.GetRequiredKeyedService<IOutputConverter>(registration.ServiceKey);
        }
        catch
        {
            AddError($"converter '{configuration.Id}' could not be resolved.");
            return;
        }

        foreach (var settingsError in settingsValidator.Validate(converterDescriptor, converter, configuration.Settings))
            AddError($"converter '{configuration.Id}' settings are invalid: {settingsError}");
    }

    private static bool CanAssign(Type sourceType, Type destinationType)
    {
        if (destinationType.IsAssignableFrom(sourceType))
            return true;

        return Nullable.GetUnderlyingType(destinationType)?.IsAssignableFrom(sourceType) == true;
    }
}
