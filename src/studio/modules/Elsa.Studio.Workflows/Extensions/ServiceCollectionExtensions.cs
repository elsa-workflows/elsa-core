using Elsa.Studio.ActivityPortProviders.Extensions;
using Elsa.Studio.Contracts;
using Elsa.Studio.DomInterop.Extensions;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.UIHints.Extensions;
using Elsa.Studio.Workflows.ActivityPickers.Accordion;
using Elsa.Studio.Workflows.Client;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.Models;
using Elsa.Studio.Workflows.Components.WorkflowInstanceList.Models;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Designer.Extensions;
using Elsa.Studio.Workflows.DiagramDesigners.Bpmn;
using Elsa.Studio.Workflows.DiagramDesigners.Fallback;
using Elsa.Studio.Workflows.DiagramDesigners.Flowcharts;
using Elsa.Studio.Workflows.DiagramDesigners.Sequences;
using Elsa.Studio.Workflows.DiagramDesigners.StateMachines;
using Elsa.Studio.Workflows.Handlers;
using Elsa.Studio.Workflows.Menu;
using Elsa.Studio.Workflows.Services;
using Elsa.Studio.Workflows.Widgets;
using Microsoft.Extensions.DependencyInjection;


namespace Elsa.Studio.Workflows.Extensions;

/// <summary>
/// Contains extension methods for the <see cref="IServiceCollection"/> interface.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the workflows module.
    /// </summary>
    public static IServiceCollection AddWorkflowsModule(this IServiceCollection services)
    {
        services
            .AddScoped<IFeature, Feature>()
            .AddScoped<IMenuProvider, WorkflowsMenu>()
            .AddScoped<IWorkflowInstanceObserverFactory, WorkflowInstanceObserverFactory>()
            .AddScoped<IWorkflowCloningDialogService, WorkflowCloningDialogService>()
            .AddScoped<IWorkflowExportDialogService, WorkflowExportDialogService>()
            .AddScoped<IBpmnImportUiService, BpmnImportUiService>()
            .AddDefaultUIHintHandlers()
            .AddDefaultActivityPortProviders()
            .AddWorkflowsCore()
            .AddWorkflowsDesigner()
            .AddDomInterop()
            .AddClipboardInterop()
            .AddDownloadInterop()
            ;

        services
            .AddDiagramDesignerProvider<FallbackDesignerProvider>()
            .AddDiagramDesignerProvider<StateMachineDiagramDesignerProvider>()
            .AddDiagramDesignerProvider<FlowchartDiagramDesignerProvider>()
            .AddDiagramDesignerProvider<SequenceDiagramDesignerProvider>()
            .AddDiagramDesignerProvider<BpmnDiagramDesignerProvider>();

        services.AddNotificationHandler<RefreshActivityRegistry>();
        services.AddScoped<IWidget, WorkflowDefinitionMetadataWidget>();
        services.AddScoped<IWidget, WorkflowDefinitionSettingsWidget>();
        services.AddScoped<IWidget, WorkflowDefinitionInfoWidget>();
        services.AddScoped<IActivityPickerComponentProvider, AccordionActivityPickerComponentProvider>();
        services.AddScoped<ICreateWorkflowDialogComponentProvider, DefaultCreateWorkflowDialogComponentProvider>();
        
        services.Configure<WorkflowDefinitionOptions>(opts =>
        {
            opts.AutoSaveChangesByDefault = true;
            opts.AutoApplyCodeViewChangesByDefault = true;
        });
        services.Configure<WorkflowInstanceListPollingOptions>(opts =>
        {
            opts.IsEnabledByDefault = true;
            opts.IntervalSeconds = 10;
        });

        return services;
    }

    /// <summary>
    /// Adds the workflows module with remote backend BPMN interchange API support.
    /// </summary>
    public static IServiceCollection AddWorkflowsModule(this IServiceCollection services, BackendApiConfig backendApiConfig)
    {
        return services
            .AddWorkflowsModule()
            .AddRemoteApi<IBpmnInterchangeApi>(backendApiConfig);
    }
}
