using Elsa.Common.Models;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Management.Services
{
    /// <inheritdoc />
    public class WorkflowDefinitionImporter : IWorkflowDefinitionImporter
    {
        private readonly IActivitySerializer _serializer;
        private readonly IWorkflowDefinitionPublisher _workflowDefinitionPublisher;
        private readonly VariableDefinitionMapper _variableDefinitionMapper;
        private readonly IWorkflowDefinitionStore? _workflowDefinitionStore;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowDefinitionImporter"/> class.
        /// </summary>
        public WorkflowDefinitionImporter(
            IActivitySerializer serializer,
            IWorkflowDefinitionPublisher workflowDefinitionPublisher,
            VariableDefinitionMapper variableDefinitionMapper,
            IWorkflowDefinitionStore workflowDefinitionStore)
        {
            _serializer = serializer;
            _workflowDefinitionPublisher = workflowDefinitionPublisher;
            _variableDefinitionMapper = variableDefinitionMapper;
            _workflowDefinitionStore = workflowDefinitionStore;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowDefinitionImporter"/> class.
        /// </summary>
        [Obsolete("Use the constructor that accepts an IWorkflowDefinitionStore. This overload keeps the imported version id even when another tenant already uses it.")]
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
        public async Task<ImportWorkflowResult> ImportAsync(SaveWorkflowDefinitionRequest request, CancellationToken cancellationToken = default)
        {
            var model = request.Model;
            var definitionId = model.DefinitionId;

            // Get a workflow draft version.
            var draft = !string.IsNullOrWhiteSpace(definitionId)
                ? await _workflowDefinitionPublisher.GetDraftAsync(definitionId, VersionOptions.Latest, cancellationToken)
                : null;

            var isNew = draft == null;

            // Create a new workflow in case no existing definition was found.
            if (isNew)
            {
                draft = await _workflowDefinitionPublisher.NewAsync(cancellationToken: cancellationToken);

                if (!string.IsNullOrWhiteSpace(definitionId))
                    draft.DefinitionId = definitionId;
                
                // Version ids are primary keys shared by all tenants. An export imported into a second tenant keeps its
                // definition id but needs a new version id.
                if (!string.IsNullOrWhiteSpace(model.Id) && !await IsVersionIdTakenAsync(model.Id, cancellationToken))
                    draft.Id = model.Id;
            }

            // Update the draft with the received model.
            var root = model.Root ?? new Sequence();
            var stringData = _serializer.Serialize(root);
            var variables = _variableDefinitionMapper.Map(model.Variables).ToList();

            draft!.StringData = stringData;
            draft.OriginalSource = null; // The imported model replaces any previous file snapshot.
            draft.MaterializerName = JsonWorkflowMaterializer.MaterializerName;
            draft.Name = model.Name?.Trim();
            draft.Description = model.Description?.Trim();
            draft.CustomProperties = model.CustomProperties ?? new Dictionary<string, object>();
            draft.Variables = variables;
            draft.Inputs = model.Inputs ?? new List<InputDefinition>();
            draft.Outputs = model.Outputs ?? new List<OutputDefinition>();
            draft.Outcomes = model.Outcomes ?? new List<string>();
            draft.IsReadonly = model.IsReadonly;
            draft.Options = model.Options ?? new WorkflowOptions();
            draft.ToolVersion = model.ToolVersion;

            if (request.Publish ?? model.IsPublished)
            {
                var result = await _workflowDefinitionPublisher.PublishAsync(draft, cancellationToken);
                return new ImportWorkflowResult(result.Succeeded, draft, result.ValidationErrors);
            }

            await _workflowDefinitionPublisher.SaveDraftAsync(draft, cancellationToken);
            return new ImportWorkflowResult(true, draft, new List<WorkflowValidationError>());
        }

        private async Task<bool> IsVersionIdTakenAsync(string versionId, CancellationToken cancellationToken)
        {
            if (_workflowDefinitionStore == null)
                return false;

            return await _workflowDefinitionStore.AnyAsync(new WorkflowDefinitionFilter { Id = versionId, TenantAgnostic = true }, cancellationToken);
        }
    }
}