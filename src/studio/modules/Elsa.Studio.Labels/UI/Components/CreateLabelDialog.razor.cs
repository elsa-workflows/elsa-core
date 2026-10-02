using Blazilla;
using Elsa.Studio.Contracts;
using Elsa.Studio.Labels.Client;
using Elsa.Studio.Labels.Models;
using Elsa.Studio.Labels.UI.Validators;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Elsa.Studio.Labels.UI.Components;

/// A dialog that creates a new label.
public partial class CreateLabelDialog
{
    private readonly LabelInputModel _inputModel = new() { Name = string.Empty };
    private EditContext _editContext = null!;
    private LabelInputModelValidator? _validator;
    
    /// The default name of the agent to create.
    [Parameter] public string LabelName { get; set; } = "";
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
    [Inject] private IBackendApiClientProvider ApiClientProvider { get; set; } = null!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        _inputModel.Name = LabelName;
        _inputModel.Description = "";
        _editContext = new(_inputModel);
        var api = await ApiClientProvider.GetApiAsync<ILabelsApi>();
        _validator = new(api);
    }

    private Task OnCancelClicked()
    {
        MudDialog.Cancel();
        return Task.CompletedTask;
    }

    private async Task OnSubmitClicked()
    {
        // Guard against submitting while the validator is still loading: no FluentValidator is attached to
        // _editContext yet, so ValidateAsync() would find no messages and return true, letting an empty label through.
        if (_validator is null)
            return;

        if(!await _editContext.ValidateAsync())
            return;

        await OnValidSubmit();
    }

    private Task OnValidSubmit()
    {
        MudDialog.Close(_inputModel);
        return Task.CompletedTask;
    }
}