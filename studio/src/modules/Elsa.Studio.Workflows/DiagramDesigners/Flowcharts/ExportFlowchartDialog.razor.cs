using Blazilla;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Validators;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Elsa.Studio.Workflows.DiagramDesigners.Flowcharts;

/// <summary>
/// A dialog that collects the options to export the flowchart as an image with.
/// </summary>
public partial class ExportFlowchartDialog
{
    private readonly ExportGraphOptions _model = new();
    private EditContext _editContext = null!;
    private ExportGraphOptionsValidator _validator = null!;

    /// <summary>
    /// The file name to propose, without an extension.
    /// </summary>
    [Parameter] public string FileName { get; set; } = string.Empty;

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        // Deliberately not OnParametersSet: the dialog's parameters never change once it is shown, and rebuilding the
        // edit context on a re-render would discard both the user's edits and any validation messages already shown.
        _model.FileName = FileName;
        _editContext = new(_model);
        _validator = new(Localizer);
    }

    private Task OnCancelClicked()
    {
        MudDialog.Cancel();
        return Task.CompletedTask;
    }

    private async Task OnSubmitClicked()
    {
        // The Ok button lives outside the form, so it has to run the validation itself.
        if (!await _editContext.ValidateAsync())
            return;

        await OnValidSubmit();
    }

    private Task OnValidSubmit()
    {
        MudDialog.Close(_model);
        return Task.CompletedTask;
    }
}
