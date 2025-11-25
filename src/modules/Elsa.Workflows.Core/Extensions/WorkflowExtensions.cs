using Elsa.Workflows.Activities;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extension methods for <see cref="Workflow"/>.
/// </summary>  
public static class WorkflowExtensions
{
    extension(Workflow workflow)
    {
        /// <summary>
        /// Returns a boolean indicating whether the workflow was created with modern tooling.
        /// </summary>
        public bool CreatedWithModernTooling() => workflow.WorkflowMetadata.ToolVersion?.Major >= 3;

        /// <summary>
        /// Executes the specified action depending on whether the workflow was created with modern tooling or not.
        /// </summary>
        public void WhenCreatedWithModernTooling(Action modernToolingAction, Action legacyToolingAction)
        {
            if (workflow.CreatedWithModernTooling())
                modernToolingAction();
            else
                legacyToolingAction();
        }
    }
}