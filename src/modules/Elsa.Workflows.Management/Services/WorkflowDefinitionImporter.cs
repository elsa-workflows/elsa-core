using Elsa.Common.Models;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Management.Models;
using System.Text.Json;

namespace Elsa.Workflows.Management.Services
{
    public class WorkflowDefinitionImporter : IWorkflowDefinitionImporter
    {
        private readonly SerializerOptionsProvider _serializerOptionsProvider;
        private readonly IWorkflowDefinitionPublisher _workflowDefinitionPublisher;
        private readonly VariableDefinitionMapper _variableDefinitionMapper;

        public WorkflowDefinitionImporter(
            SerializerOptionsProvider serializerOptionsProvider,
            IWorkflowDefinitionPublisher workflowDefinitionPublisher,
            VariableDefinitionMapper variableDefinitionMapper)
        {
            _serializerOptionsProvider = serializerOptionsProvider;
            _workflowDefinitionPublisher = workflowDefinitionPublisher;
            _variableDefinitionMapper = variableDefinitionMapper;
        }

        public async Task<WorkflowDefinition> ImportAsync(WorkflowDefinitionImportModel model, CancellationToken cancellationToken = default)
        {
            var definitionId = model.DefinitionId;

            // Get a workflow draft version.
            var draft = !string.IsNullOrWhiteSpace(definitionId)
                ? await _workflowDefinitionPublisher.GetDraftAsync(definitionId, VersionOptions.Latest, cancellationToken)
                : default;

            var isNew = draft == null;

            // Create a new workflow in case no existing definition was found.
            if (isNew)
            {
                draft = _workflowDefinitionPublisher.New();

                if (!string.IsNullOrWhiteSpace(definitionId))
                    draft.DefinitionId = definitionId;
            }

            // Update the draft with the received model.
            var root = model.Root;
            var serializerOptions = _serializerOptionsProvider.CreateApiOptions();
            var stringData = JsonSerializer.Serialize(root, serializerOptions);
            var variables = _variableDefinitionMapper.Map(model.Variables).ToList();

            draft!.StringData = stringData;
            draft.MaterializerName = JsonWorkflowMaterializer.MaterializerName;
            draft.Name = model.Name?.Trim();
            draft.Description = model.Description?.Trim();
            draft.CustomProperties = model.CustomProperties ?? new Dictionary<string, object>();
            draft.Variables = variables;
            draft.Inputs = model.Inputs ?? new List<InputDefinition>();
            draft.Outputs = model.Outputs ?? new List<OutputDefinition>();
            draft.Outcomes = model.Outcomes ?? new List<string>();
            draft.Options = model.Options;
            draft.UsableAsActivity = model.UsableAsActivity;
            draft = model.Publish ? await _workflowDefinitionPublisher.PublishAsync(draft, cancellationToken) : await _workflowDefinitionPublisher.SaveDraftAsync(draft, cancellationToken);

            return draft;
        }
    }
}
