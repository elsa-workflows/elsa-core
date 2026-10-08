using Elsa.Studio.Workflows.Domain.Models;

namespace Elsa.Studio.Workflows.Contracts;

/// <summary>
/// Provides functionality for showing the export options dialog when exporting workflow definitions.
/// </summary>
public interface IWorkflowExportDialogService
{
    /// <summary>
    /// Shows the export options dialog and returns whether consuming workflows should be included.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> to include consuming workflows, <see langword="false"/> to exclude them,
    /// or <see langword="null"/> if the user cancelled the dialog.
    /// </returns>
    Task<bool?> ShowExportOptionsDialogAsync();

    /// <summary>
    /// Shows the export options dialog and, if not cancelled, invokes <paramref name="exportFactory"/> with the
    /// selected option and downloads the resulting file.
    /// </summary>
    /// <param name="exportFactory">
    /// A delegate that receives the <c>includeConsumingWorkflows</c> flag and returns a <see cref="FileDownload"/>.
    /// </param>
    /// <returns><see langword="true"/> if the export was performed; <see langword="false"/> if the user cancelled.</returns>
    Task<bool> ExportAndDownloadAsync(Func<bool, Task<FileDownload>> exportFactory);
}

