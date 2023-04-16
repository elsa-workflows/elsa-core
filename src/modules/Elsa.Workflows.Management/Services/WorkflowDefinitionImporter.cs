using Elsa.Common.Models;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Management.Services
{
    /// <inheritdoc />
    public class WorkflowDefinitionImporter : IWorkflowDefinitionImporter
    {
        private readonly IActivitySerializer _serializer;
        private readonly IWorkflowDefinitionPublisher _workflowDefinitionPublisher;
        private readonly VariableDefinitionMapper _variableDefinitionMapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowDefinitionImporter"/> class.
        /// </summary>
        public WorkflowDefinitionImporter(
            IActivitySerializer serializer,
            IWorkflowDefinitionPublisher workflowDefinitionPublisher,
            VariableDefinitionMapper variableDefinitionMapper)
        {
            _serializer = serializer;
            _workflowDefinitionPublisher = workflowDefinitionPublisher;
            _variableDefinitionMapper = variableDefinitionMapper;
        }

        /// <inheritdoc />
        public async Task<WorkflowDefinition> ImportAsync(SaveWorkflowDefinitionRequest request, CancellationToken cancellationToken = default)
        {
            var definitionId = request.DefinitionId;

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
            var root = request.Root!;
            var stringData = _serializer.Serialize(root);
            var variables = _variableDefinitionMapper.Map(request.Variables).ToList();

            draft!.StringData = stringData;
            draft.MaterializerName = JsonWorkflowMaterializer.MaterializerName;
            draft.Name = request.Name?.Trim();
            draft.Description = request.Description?.Trim();
            draft.CustomProperties = request.CustomProperties ?? new Dictionary<string, object>();
            draft.Variables = variables;
            draft.Inputs = request.Inputs ?? new List<InputDefinition>();
            draft.Outputs = request.Outputs ?? new List<OutputDefinition>();
            draft.Outcomes = request.Outcomes ?? new List<string>();
            draft.Options = request.Options;
            draft.UsableAsActivity = request.UsableAsActivity;
            draft = request.Publish ? await _workflowDefinitionPublisher.PublishAsync(draft, cancellationToken) : await _workflowDefinitionPublisher.SaveDraftAsync(draft, cancellationToken);

            return draft;
        }
    }
}