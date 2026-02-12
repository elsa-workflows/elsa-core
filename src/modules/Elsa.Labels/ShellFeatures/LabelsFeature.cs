using CShells.Features;
using Elsa.Extensions;
using Elsa.Labels.Contracts;
using Elsa.Labels.Entities;
using Elsa.Labels.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Labels.ShellFeatures;

/// <summary>
/// Enables functionality to tag workflows with labels.
/// </summary>
[ShellFeature(
    DisplayName = "Labels",
    Description = "Enables functionality to tag workflows with labels",
    DependsOn = ["Mediator"])]
[UsedImplicitly]
public class LabelsFeature : IShellFeature
{
    /// <summary>
    /// A delegate that provides an instance of an implementation of <see cref="ILabelStore"/>.
    /// </summary>
    public Func<IServiceProvider, ILabelStore> LabelStore { get; set; } = sp => sp.GetRequiredService<InMemoryLabelStore>();

    /// <summary>
    /// A delegate that provides an instance of an implementation of <see cref="IWorkflowDefinitionLabelStore"/>.
    /// </summary>
    public Func<IServiceProvider, IWorkflowDefinitionLabelStore> WorkflowDefinitionLabelStore { get; set; } = sp => sp.GetRequiredService<InMemoryWorkflowDefinitionLabelStore>();

    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddMemoryStore<Label, InMemoryLabelStore>()
            .AddMemoryStore<WorkflowDefinitionLabel, InMemoryWorkflowDefinitionLabelStore>()
            .AddScoped(LabelStore)
            .AddScoped(WorkflowDefinitionLabelStore);

        services.AddNotificationHandlersFrom(GetType());
    }
}

