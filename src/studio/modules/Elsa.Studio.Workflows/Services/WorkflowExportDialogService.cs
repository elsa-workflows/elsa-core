using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionList;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using MudBlazor;

namespace Elsa.Studio.Workflows.Services;

/// <inheritdoc />
public class WorkflowExportDialogService(IDialogService dialogService, ILocalizer localizer, IFiles files) : IWorkflowExportDialogService
{
    /// <inheritdoc />
    public async Task<bool?> ShowExportOptionsDialogAsync()
    {
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            Position = DialogPosition.Center,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        var dialogInstance = await dialogService.ShowAsync<ExportWorkflowDialog>(localizer["Export"], options);
        var result = await dialogInstance.Result;

        if (result?.Canceled == true)
            return null;

        return result?.Data is true;
    }

    /// <inheritdoc />
    public async Task<bool> ExportAndDownloadAsync(Func<bool, Task<FileDownload>> exportFactory)
    {
        var includeConsumingWorkflows = await ShowExportOptionsDialogAsync();

        if (includeConsumingWorkflows == null)
            return false;

        var download = await exportFactory(includeConsumingWorkflows.Value);

        if (download.Content.CanSeek)
            download.Content.Seek(0, SeekOrigin.Begin);

        await files.DownloadFileFromStreamAsync(download.FileName, download.Content);
        return true;
    }
}

