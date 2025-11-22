using Elsa.Workflows;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IActivity"/>.
/// </summary>
public static class ActivityPropertyExtensions
{
    private static readonly string[] CanStartWorkflowPropertyName = ["canStartWorkflow", "CanStartWorkflow"];
    private static readonly string[] RunAsynchronouslyPropertyName = ["runAsynchronously", "RunAsynchronously"];
    private static readonly string[] SourcePropertyName = ["source", "Source"];
    private static readonly string[] CommitStrategyName = ["commitStrategyName", "CommitStrategyName"];

    extension(IActivity activity)
    {
        /// <summary>
        /// Gets a flag indicating whether this activity can be used for starting a workflow.
        /// Usually used for triggers, but also used to disambiguate between two or more starting activities and no starting activity was specified.
        /// </summary>
        public bool GetCanStartWorkflow() => activity.CustomProperties.GetValueOrDefault(CanStartWorkflowPropertyName, () => false);

        /// <summary>
        /// Sets a flag indicating whether this activity can be used for starting a workflow.
        /// </summary>
        public void SetCanStartWorkflow(bool value) => activity.CustomProperties[CanStartWorkflowPropertyName[0]] = value;

        /// <summary>
        /// Gets a flag indicating if this activity should execute synchronously or asynchronously.
        /// By default, activities with an <see cref="ActivityKind"/> of <see cref="Action"/>, <see cref="Task"/> or <see cref="Trigger"/>
        /// will execute synchronously, while activities of the <see cref="ActivityKind.Job"/> kind will execute asynchronously.
        /// </summary>
        public bool? GetRunAsynchronously()
        {
            return activity.CustomProperties.GetValueOrDefault<bool?>(RunAsynchronouslyPropertyName, defaultValueFactory: () => null);
        }

        /// <summary>
        /// Sets a flag indicating if this activity should execute synchronously or asynchronously.
        /// By default, activities with an <see cref="ActivityKind"/> of <see cref="Action"/>, <see cref="Task"/> or <see cref="Trigger"/>
        /// will execute synchronously, while activities of the <see cref="ActivityKind.Job"/> kind will execute asynchronously.
        /// </summary>
        public void SetRunAsynchronously(bool? value) => activity.CustomProperties[RunAsynchronouslyPropertyName[0]] = value ?? false;

        /// <summary>
        /// Gets the source file and line number where this activity was instantiated, if any.
        /// </summary>
        public string? GetSource() => activity.CustomProperties.GetValueOrDefault(SourcePropertyName, () => default(string?));

        /// <summary>
        /// Sets the source file and line number where this activity was instantiated, if any.
        /// </summary>
        public void SetSource(string value) => activity.CustomProperties[SourcePropertyName[0]] = value;

        /// <summary>
        /// Gets the commit state behavior for the specified activity.
        /// </summary>
        public string? GetCommitStrategy() => activity.CustomProperties.GetValueOrDefault<string?>(CommitStrategyName, () => null);
    }

    /// <summary>
    /// Sets the commit state behavior for the specified activity.
    /// </summary>
    public static TActivity SetCommitStrategy<TActivity>(this TActivity activity, string? name) where TActivity : IActivity
    {
        if (string.IsNullOrWhiteSpace(name))
            activity.CustomProperties.Remove(CommitStrategyName[0]);
        else
            activity.CustomProperties[CommitStrategyName[0]] = name;
        return activity;
    }

    extension(IActivity activity)
    {
        /// <summary>
        /// Sets the source file and line number where this activity was instantiated, if any.
        /// </summary>
        public void SetSource(string? sourceFile, int? lineNumber)
        {
            if (sourceFile == null || lineNumber == null)
                return;

            var source = $"{Path.GetFileName(sourceFile)}:{lineNumber}";
            activity.SetSource(source);
        }

        /// <summary>
        /// Gets the display text for the specified activity.
        /// </summary>
        public string? GetDisplayText() => activity.Metadata.TryGetValue("displayText", out var value) ? value.ToString() : null;

        /// <summary>
        /// Sets the display text for the specified activity.
        /// </summary>
        public void SetDisplayText(string value) => activity.Metadata["displayText"] = value;

        /// <summary>
        /// Gets the description for the specified activity.
        /// </summary>
        public string? GetDescription() => activity.Metadata.TryGetValue("description", out var value) ? value.ToString() : null;

        /// <summary>
        /// Sets the description for the specified activity.
        /// </summary>
        public void SetDescription(string value) => activity.Metadata["description"] = value;
    }
}