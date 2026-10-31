using CShells;
using CShells.AspNetCore.Configuration;
using CShells.AspNetCore.Extensions;
using CShells.DependencyInjection;
using Elsa.ReactStudio.Host;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Elsa.ReactStudio;

/// <summary>Hosting entrypoints for the explicit React Studio Preview feature.</summary>
public static class ReactStudioHostingExtensions
{
    private static readonly Dictionary<string, (Type Feature, bool Core)> Modules = new(StringComparer.Ordinal)
    {
        ["shell"] = (typeof(Elsa.ReactStudio.Shell.ShellFeature), true),
        ["dashboard"] = (typeof(Elsa.ReactStudio.Dashboard.DashboardFeature), true),
        ["workflow-definitions"] = (typeof(Elsa.ReactStudio.WorkflowDefinitions.WorkflowDefinitionsFeature), true),
        ["designer"] = (typeof(Elsa.ReactStudio.Designer.DesignerFeature), true),
        ["activity-editors"] = (typeof(Elsa.ReactStudio.ActivityEditors.ActivityEditorsFeature), true),
        ["workflow-instances"] = (typeof(Elsa.ReactStudio.WorkflowInstances.WorkflowInstancesFeature), true),
        ["alterations"] = (typeof(Elsa.ReactStudio.Alterations.AlterationsFeature), false),
        ["labels"] = (typeof(Elsa.ReactStudio.Labels.LabelsFeature), false),
        ["security"] = (typeof(Elsa.ReactStudio.Security.SecurityFeature), false),
        ["secrets"] = (typeof(Elsa.ReactStudio.Secrets.SecretsFeature), false),
        ["user-tasks"] = (typeof(Elsa.ReactStudio.UserTasks.UserTasksFeature), false),
        ["environments"] = (typeof(Elsa.ReactStudio.Environments.EnvironmentsFeature), false),
        ["settings"] = (typeof(Elsa.ReactStudio.Settings.SettingsFeature), false),
        ["appearance"] = (typeof(Elsa.ReactStudio.Appearance.AppearanceFeature), false),
        ["diagnostics-console-logs"] = (typeof(Elsa.ReactStudio.Diagnostics.ConsoleLogs.ConsoleLogsFeature), false),
        ["diagnostics-structured-logs"] = (typeof(Elsa.ReactStudio.Diagnostics.StructuredLogs.StructuredLogsFeature), false),
    };

    /// <summary>Adds the Preview shell and the selected modules; call only when the deployment opts in.</summary>
    public static WebApplicationBuilder AddReactStudioPreview(this WebApplicationBuilder builder, params Type[] additionalFeatures)
    {
        ArgumentNullException.ThrowIfNull(additionalFeatures);
        builder.AddShells(shells => shells
            .WithAssemblies(Modules.Values.Select(module => module.Feature.Assembly).Concat(additionalFeatures.Select(feature => feature.Assembly))
                .Append(typeof(ReactStudioPreviewFeature).Assembly).Distinct().ToArray())
            .ConfigureResolverPipeline(pipeline => pipeline.Use(new ReactStudioShellResolver(builder.Configuration)))
            .AddShell("ReactStudio", shell =>
            {
                // Compose against the final host configuration, including configuration
                // providers added after this hosting extension was registered.
                var selected = builder.Configuration.GetSection("ReactStudio:Modules").Get<string[]>();
                if (selected != null && selected.Any(id => !Modules.ContainsKey(id)))
                {
                    throw new ArgumentException("ReactStudio:Modules contains an unknown module ID.");
                }
                var enabled = Modules.Where(module => module.Value.Core || selected == null || selected.Contains(module.Key, StringComparer.Ordinal))
                    .Select(module => module.Value.Feature);
                shell.WithFeatures(enabled.Concat(additionalFeatures).Prepend(typeof(ReactStudioPreviewFeature)).Distinct().ToArray());
            }));
        return builder;
    }

    /// <summary>Maps the enabled Preview feature endpoints after routing is configured.</summary>
    public static WebApplication MapReactStudioPreview(this WebApplication app)
    {
        app.MapShells();
        return app;
    }
}
