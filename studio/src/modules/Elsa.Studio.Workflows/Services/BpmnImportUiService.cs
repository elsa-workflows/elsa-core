using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionList;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Elsa.Studio.Workflows.Services;

/// <inheritdoc cref="IBpmnImportUiService" />
public class BpmnImportUiService(
    IDialogService dialogService,
    ILocalizer localizer,
    IBpmnInterchangeService bpmnInterchangeService,
    IWorkflowDefinitionService workflowDefinitionService) : IBpmnImportUiService
{
    /// <summary>The maximum file size this flow reads into memory, matching <see cref="ImportOptions"/>'s own default.</summary>
    private const int MaxAllowedSize = 1024 * 1024 * 10; // 10 MB

    /// <inheritdoc />
    public bool IsBpmnFile(IBrowserFile file) => IsBpmnFileName(file.Name);

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="fileName"/> has a <c>.bpmn</c> extension, case-insensitively.
    /// </summary>
    public static bool IsBpmnFileName(string fileName) => fileName.EndsWith(".bpmn", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public async Task<WorkflowImportResult?> ImportFileAsync(IBrowserFile file, string? definitionId, CancellationToken cancellationToken = default)
    {
        // The document is read once and re-used for both calls: Analyze and Import read the same file through the
        // same reader in elsa-core (see BpmnInterchangeDocumentService's remarks), so re-uploading whatever is still
        // on disk for the second call risks it having changed between the two round trips.
        await using var browserStream = file.OpenReadStream(MaxAllowedSize, cancellationToken);
        using var content = new MemoryStream();
        await browserStream.CopyToAsync(content, cancellationToken);

        content.Seek(0, SeekOrigin.Begin);
        var analysisResult = await bpmnInterchangeService.AnalyzeAsync(content, file.Name, cancellationToken);

        if (!analysisResult.IsSuccess)
            return Failed(file.Name, analysisResult.Failure!);

        var processId = await ShowFindingsDialogAsync(analysisResult.Success!);

        if (processId.Cancelled)
            return null;

        content.Seek(0, SeekOrigin.Begin);
        var importResult = await bpmnInterchangeService.ImportAsync(content, file.Name, definitionId, name: null, processId.ProcessId, cancellationToken);

        if (!importResult.IsSuccess)
        {
            var failure = importResult.Failure!;

            // Recognized by the envelope's machine-readable code rather than by wording (or even just 422 status),
            // so a server rewording of the message never breaks this. A body with no code (an older server) or an
            // unrecognized one falls back to the ordinary failure path, showing the server's own message.
            if (failure.Code == BpmnErrorCodes.ImportCapabilityUnsupported)
            {
                var refusal = BpmnCapabilityRefusal.FromData(failure.Data);

                if (refusal != null)
                {
                    var message = string.Join(" ", failure.Errors.Select(error => error.ErrorMessage));
                    await ShowRefusalDialogAsync(refusal);
                    return new()
                    {
                        FileName = file.Name,
                        Failure = new(message, WorkflowImportFailureType.CapabilityRefusal)
                    };
                }
            }

            return Failed(file.Name, failure);
        }

        var workflowDefinition = await workflowDefinitionService.FindByDefinitionIdAsync(importResult.Success!.DefinitionId, VersionOptions.Latest, cancellationToken);

        if (workflowDefinition == null)
        {
            return new()
            {
                FileName = file.Name,
                Failure = new(localizer["The document was imported, but the resulting workflow definition could not be loaded."], WorkflowImportFailureType.Exception)
            };
        }

        return new()
        {
            FileName = file.Name,
            WorkflowDefinition = workflowDefinition
        };
    }

    /// <inheritdoc />
    public async Task<BpmnImportBatch> ImportBpmnFilesAsync(IReadOnlyList<IBrowserFile> files, string? definitionId, CancellationToken cancellationToken = default)
    {
        var bpmnFiles = files.Where(IsBpmnFile).ToList();
        var otherFiles = files.Where(file => !IsBpmnFile(file)).ToList();

        var results = new List<WorkflowImportResult>();

        // When updating an already-open workflow, a batch may contain at most one BPMN file: importing more than
        // one would import each into the same definition, silently replacing the earlier ones.
        if (definitionId != null && bpmnFiles.Count > 1)
        {
            var message = (string)localizer["Select a single BPMN file to update the open workflow; {0} were selected", bpmnFiles.Count];

            foreach (var bpmnFile in bpmnFiles)
            {
                results.Add(new()
                {
                    FileName = bpmnFile.Name,
                    Failure = new(message, WorkflowImportFailureType.Exception)
                });
            }

            return new(results, otherFiles);
        }

        foreach (var bpmnFile in bpmnFiles)
        {
            var result = await ImportFileAsync(bpmnFile, definitionId, cancellationToken);
            if (result != null)
                results.Add(result);
        }

        return new(results, otherFiles);
    }

    private async Task ShowRefusalDialogAsync(BpmnCapabilityRefusal refusal)
    {
        var parameters = new DialogParameters<BpmnImportRefusalDialog> { { x => x.Refusal, refusal } };
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            Position = DialogPosition.Center,
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            CloseButton = true
        };

        var dialog = await dialogService.ShowAsync<BpmnImportRefusalDialog>(localizer["Import refused"], parameters, options);
        await dialog.Result;
    }

    private async Task<(bool Cancelled, string? ProcessId)> ShowFindingsDialogAsync(BpmnImportAnalysisModel analysis)
    {
        var parameters = new DialogParameters<BpmnImportFindingsDialog> { { x => x.Analysis, analysis } };
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            Position = DialogPosition.Center,
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            CloseButton = true
        };

        var dialog = await dialogService.ShowAsync<BpmnImportFindingsDialog>(localizer["Import BPMN"], parameters, options);
        var result = await dialog.Result;

        if (result is null || result.Canceled)
            return (true, null);

        return (false, result.Data as string);
    }

    private static WorkflowImportResult Failed(string fileName, ValidationErrors errors) => new()
    {
        FileName = fileName,
        Failure = new(string.Join(" ", errors.Errors.Select(error => error.ErrorMessage)), WorkflowImportFailureType.Exception)
    };
}
