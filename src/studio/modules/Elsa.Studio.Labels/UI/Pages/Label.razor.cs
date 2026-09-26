using Elsa.Studio.Components;
using Elsa.Studio.Contracts;
using Elsa.Studio.Labels.Client;
using Elsa.Studio.Labels.Models;
using Elsa.Studio.Labels.UI.Validators;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Labels.UI.Pages;

public partial class Label : StudioComponentBase
{
    /// The ID of the label to edit.
    [Parameter] public string LabelId { get; set; } = default!;

    [Inject] private IBackendApiClientProvider ApiClientProvider { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private MudForm _form = default!;
    private LabelInputModelValidator _validator = default!;
    private LabelInputModel _model = new(){ Name = string.Empty};

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var apiClient = await ApiClientProvider.GetApiAsync<ILabelsApi>();
        _validator = new LabelInputModelValidator(apiClient);
    }

    /// <summary>
    /// Called when the component's parameters are set. This method retrieves the label details
    /// from the API and initializes the model with the label's data.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected override async Task OnParametersSetAsync()
    {
        var apiClient = await ApiClientProvider.GetApiAsync<ILabelsApi>();
        var label = await apiClient.GetAsync(LabelId);
        _model = new LabelInputModel()
        {
            Name = label.Name ?? label.NormalizedName,
            Description = label.Description ?? string.Empty,
            Color = label.Color ?? string.Empty,
        };
    }

    private async Task OnSaveClicked()
    {
        await _form.Validate();

        if (!_form.IsValid)
            return;

        var apiClient = await ApiClientProvider.GetApiAsync<ILabelsApi>();
        await apiClient.UpdateAsync(LabelId, _model);
        Snackbar.Add("Label successfully updated.", Severity.Success);
        StateHasChanged();
        NavigationManager.NavigateTo("labels");
    }
}