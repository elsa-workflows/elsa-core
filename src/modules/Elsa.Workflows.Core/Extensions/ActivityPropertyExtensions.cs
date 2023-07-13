using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Contracts;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IActivity"/>.
/// </summary>
public static class ActivityPropertyExtensions
{
    private static readonly string[] CanStartWorkflowPropertyName = {"canStartWorkflow", "CanStartWorkflow", };
    private static readonly string[] RunAsynchronouslyPropertyName = {"runAsynchronously", "RunAsynchronously" };
    private static readonly string[] SourcePropertyName = {"source", "Source"};

    /// <summary>
    /// Gets a flag indicating whether this activity can be used for starting a workflow.
    /// Usually used for triggers, but also used to disambiguate between two or more starting activities and no starting activity was specified.
    /// </summary>
    public static bool GetCanStartWorkflow(this IActivity activity) => activity.CustomProperties.GetValueOrDefault(CanStartWorkflowPropertyName, () => false);

    /// <summary>
    /// Sets a flag indicating whether this activity can be used for starting a workflow.
    /// </summary>
    public static void SetCanStartWorkflow(this IActivity activity, bool value) => activity.CustomProperties[CanStartWorkflowPropertyName[0]] = value;

    /// <summary>
    /// Gets a flag indicating if this activity should execute synchronously or asynchronously.
    /// By default, activities with an <see cref="Workflows.Core.Models.ActivityKind"/> of <see cref="Action"/>, <see cref="Task"/> or <see cref="Trigger"/>
    /// will execute synchronously, while activities of the <see cref="Workflows.Core.Models.ActivityKind.Job"/> kind will execute asynchronously.
    /// </summary>
    public static bool GetRunAsynchronously(this IActivity activity) => activity.CustomProperties.GetValueOrDefault(RunAsynchronouslyPropertyName, () => false);

    /// <summary>
    /// Sets a flag indicating if this activity should execute synchronously or asynchronously.
    /// By default, activities with an <see cref="Workflows.Core.Models.ActivityKind"/> of <see cref="Action"/>, <see cref="Task"/> or <see cref="Trigger"/>
    /// will execute synchronously, while activities of the <see cref="Workflows.Core.Models.ActivityKind.Job"/> kind will execute asynchronously.
    /// </summary>
    public static void SetRunAsynchronously(this IActivity activity, bool value) => activity.CustomProperties[RunAsynchronouslyPropertyName[0]] = value;

    /// <summary>
    /// Gets the source file and line number where this activity was instantiated, if any.
    /// </summary>
    public static string? GetSource(this IActivity activity) => activity.CustomProperties.GetValueOrDefault(SourcePropertyName, () => default(string?));

    /// <summary>
    /// Sets the source file and line number where this activity was instantiated, if any.
    /// </summary>
    public static void SetSource(this IActivity activity, string value) => activity.CustomProperties[SourcePropertyName[0]] = value;

    /// <summary>
    /// Sets the source file and line number where this activity was instantiated, if any.
    /// </summary>
    public static void SetSource(this IActivity activity, string? sourceFile, int? lineNumber)
    {
        if (sourceFile == null || lineNumber != null)
            return;

        var source = $"{Path.GetFileName(sourceFile)}:{lineNumber}";
        activity.SetSource(source);
    }
}