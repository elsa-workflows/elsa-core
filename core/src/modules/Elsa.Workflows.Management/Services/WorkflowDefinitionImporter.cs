using Elsa.Common.Models;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Exceptions;
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
        [Obsolete("Use the constructor that accepts an IWorkflowDefinitionStore. This overload always generates a version id for a new workflow.")]
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
            string? generatedId = null;

            // Create a new workflow in case no existing definition was found.
            if (isNew)
            {
                draft = await _workflowDefinitionPublisher.NewAsync(cancellationToken: cancellationToken);
                generatedId = draft.Id;

                if (!string.IsNullOrWhiteSpace(definitionId))
                    draft.DefinitionId = definitionId;
                
                if (_workflowDefinitionStore != null)
                    draft.Id = await _workflowDefinitionStore.ResolveVersionIdAsync(model.Id, draft.Id, cancellationToken);
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

            var publish = request.Publish ?? model.IsPublished;
            if (isNew)
                return await CreateInitialAsync(draft, generatedId!, publish, cancellationToken);

            if (publish)
            {
                var result = await _workflowDefinitionPublisher.PublishAsync(draft, cancellationToken);
                return new ImportWorkflowResult(result.Succeeded, draft, result.ValidationErrors);
            }

            try
            {
                await _workflowDefinitionPublisher.SaveDraftAsync(draft, cancellationToken);
                return new(true, draft, []);
            }
            catch (WorkflowDefinitionEditConflictException)
            {
                return new(false, draft, [new("The workflow definition changed or is protected.")]);
            }
        }

        private async Task<ImportWorkflowResult> CreateInitialAsync(WorkflowDefinition draft, string generatedId, bool publish, CancellationToken cancellationToken)
        {
            try
            {
                PublishWorkflowDefinitionResult result;
                try
                {
                    result = await _workflowDefinitionPublisher.CreateInitialAsync(draft, publish, cancellationToken);
                }
                catch (WorkflowDefinitionAlreadyExistsException) when (draft.Id != generatedId)
                {
                    // Another tenant or version may have claimed the exported ID after we looked it up.
                    // Retry once with the original server-generated version identity.
                    draft.Id = generatedId;
                    result = await _workflowDefinitionPublisher.CreateInitialAsync(draft, publish, cancellationToken);
                }

                return new(result.Succeeded, draft, result.ValidationErrors);
            }
            catch (WorkflowDefinitionAlreadyExistsException)
            {
                return new(false, draft, [new("A workflow definition with the same identity already exists.")]);
            }
        }
    }
}
