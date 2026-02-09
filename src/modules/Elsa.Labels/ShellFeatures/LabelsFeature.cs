using CShells.FastEndpoints.Features;
using CShells.Features;
using Elsa.Extensions;
using Elsa.Labels.Contracts;
using Elsa.Labels.Entities;
using Elsa.Labels.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Labels.ShellFeatures;

/// <summary>
/// Enables functionality to tag workflows with labels.
/// </summary>
[ShellFeature(
    DisplayName = "Workflow Labels",
    Description = "Enables labeling and tagging of workflows for better organization",
    DependsOn = ["Mediator"])]
public class LabelsFeature : IFastEndpointsShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddMemoryStore<Label, InMemoryLabelStore>()
            .AddMemoryStore<WorkflowDefinitionLabel, InMemoryWorkflowDefinitionLabelStore>()
            .AddScoped<ILabelStore, InMemoryLabelStore>()
            .AddScoped<IWorkflowDefinitionLabelStore, InMemoryWorkflowDefinitionLabelStore>()
            ;

        services.AddNotificationHandlersFrom(typeof(LabelsFeature));
    }
}
