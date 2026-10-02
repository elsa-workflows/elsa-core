using Blazored.FluentValidation;
using Elsa.Secrets;
using Elsa.Studio.Contracts;
using Elsa.Studio.Secrets.Client;
using Elsa.Studio.Secrets.UI.Validators;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Elsa.Studio.Secrets.UI.Components;

/// A dialog that creates a new secret.
public partial class CreateSecretDialog
{
    private readonly SecretInputModel _inputModel = new();
    private EditContext _editContext = null!;
    private FluentValidationValidator _fluentValidationValidator = null!;
    private SecretInputModelValidator _validator = null!;
    
    /// The default name of the agent to create.
    [Parameter] public string SecretName { get; set; } = "";
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
    [Inject] private IBackendApiClientProvider ApiClientProvider { get; set; } = null!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        _inputModel.Name = SecretName;
        _inputModel.Description = "";
        _editContext = new(_inputModel);
        var api = await ApiClientProvider.GetApiAsync<ISecretsApi>();
        _validator = new(api);
    }

    private Task OnCancelClicked()
    {
        MudDialog.Cancel();
        return Task.CompletedTask;
    }

    private async Task OnSubmitClicked()
    {
        if(!await _fluentValidationValidator.ValidateAsync())
            return;

        await OnValidSubmit();
    }

    private Task OnValidSubmit()
    {
        MudDialog.Close(_inputModel);
        return Task.CompletedTask;
    }
}