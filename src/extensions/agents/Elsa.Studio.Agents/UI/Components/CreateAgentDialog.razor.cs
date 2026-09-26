using Elsa.Agents;
using Elsa.Studio.Agents.Client;
using Elsa.Studio.Agents.UI.Validators;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Elsa.Studio.Agents.UI.Components;

/// A dialog that allows the user to create a new agent.
public partial class CreateAgentDialog : IDisposable
{
    private readonly AgentInputModel _agentInputModel = new();
    private EditContext _editContext = null!;
    private AgentSubmitValidator _submitValidator = null!;
    private bool _submitting;
    private AgentInputModelValidator _validator = null!;
    
    /// The default name of the agent to create.
    [Parameter] public string AgentName { get; set; } = "";
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;
    [Inject] private IBackendApiClientProvider ApiClientProvider { get; set; } = null!;
    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = null!;
    [Inject] private IActivityDisplaySettingsRegistry ActivityDisplaySettingsRegistry { get; set; } = null!;
    private ICollection<SkillDescriptorModel> AvailableSkills { get; set; } = [];
    private IReadOnlyCollection<string> SelectedSkills { get; set; } = [];

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        _agentInputModel.Name = AgentName;
        _agentInputModel.PromptTemplate = "You are a helpful assistant.";
        _agentInputModel.Description = "A helpful assistant.";
        _agentInputModel.OutputVariable.Type = "object";
        _agentInputModel.OutputVariable.Description = "The output of the agent.";
        _agentInputModel.ExecutionSettings.ResponseFormat = "json_object";
        _editContext = new(_agentInputModel);
        var agentsApi = await ApiClientProvider.GetApiAsync<IAgentsApi>();
        var skillsApi = await ApiClientProvider.GetApiAsync<ISkillsApi>();
        _validator = new(agentsApi);
        _submitValidator = new(_validator, _agentInputModel, _editContext);
        var skillsResponseList = await skillsApi.ListAsync();
        AvailableSkills = skillsResponseList.Items;
        SelectedSkills = _agentInputModel.Skills.ToList().AsReadOnly();
    }

    private Task OnCancelClicked()
    {
        _submitValidator?.Dispose();
        MudDialog.Cancel();
        return Task.CompletedTask;
    }

    private async Task OnSubmitClicked()
    {
        if (_submitting || _submitValidator == null)
        {
            return;
        }

        _submitting = true;
        try
        {
            if (await _submitValidator.ValidateAndPublishAsync())
            {
                await OnValidSubmit();
            }
        }
        finally
        {
            _submitting = false;
        }
    }

    /// <inheritdoc />
    public void Dispose() => _submitValidator?.Dispose();

    private Task OnValidSubmit()
    {
        _agentInputModel.Skills = SelectedSkills.ToList();
        MudDialog.Close(_agentInputModel);
        ActivityRegistry.MarkStale();
        ActivityDisplaySettingsRegistry.MarkStale();
        return Task.CompletedTask;
    }
}