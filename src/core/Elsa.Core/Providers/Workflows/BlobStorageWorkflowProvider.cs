using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Serialization;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Storage.Net.Blobs;

namespace Elsa.Providers.Workflows
{
    public class BlobStorageWorkflowProvider : WorkflowProvider
    {
        private readonly IBlobStorage _storage;
        private readonly IWorkflowBlueprintMaterializer _workflowBlueprintMaterializer;
        private readonly IContentSerializer _contentSerializer;
        private readonly ILogger _logger;

        public BlobStorageWorkflowProvider(IOptions<BlobStorageWorkflowProviderOptions> options, IWorkflowBlueprintMaterializer workflowBlueprintMaterializer, IContentSerializer contentSerializer, ILogger<BlobStorageWorkflowProvider> logger)
        {
            _storage = options.Value.BlobStorageFactory();
            _workflowBlueprintMaterializer = workflowBlueprintMaterializer;
            _contentSerializer = contentSerializer;
            _logger = logger;
        }

        public override async IAsyncEnumerable<IWorkflowBlueprint> GetWorkflowsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var blobs = await _storage.ListFilesAsync(new ListOptions(), cancellationToken);

            foreach (var blob in blobs)
            {
                var json = await _storage.ReadTextAsync(blob.FullPath, Encoding.UTF8, cancellationToken);
                var model = _contentSerializer.Deserialize<WorkflowDefinition>(json);
                var blueprint = await TryMaterializeBlueprintAsync(model, cancellationToken);
                
                if(blueprint != null)
                    yield return blueprint;
            }
        }
        
        private async Task<IWorkflowBlueprint?> TryMaterializeBlueprintAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken)
        {
            try
            {
                return await _workflowBlueprintMaterializer.CreateWorkflowBlueprintAsync(workflowDefinition, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Failed to materialize workflow definition {WorkflowDefinitionId} with version {WorkflowDefinitionVersion}", workflowDefinition.DefinitionId, workflowDefinition.Version);
            }

            return null;
        }
    }
}