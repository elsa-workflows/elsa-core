using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Elsa.Models;
using Elsa.Serialization;
using Elsa.Services;
using Elsa.Services.Models;
using Storage.Net.Blobs;

namespace Elsa.WorkflowProviders
{
    public class StorageWorkflowProvider : WorkflowProvider
    {
        private readonly IBlobStorage _storage;
        private readonly IWorkflowBlueprintMaterializer _workflowBlueprintMaterializer;
        private readonly IContentSerializer _contentSerializer;

        public StorageWorkflowProvider(IBlobStorage storage, IWorkflowBlueprintMaterializer workflowBlueprintMaterializer, IContentSerializer contentSerializer)
        {
            _storage = storage;
            _workflowBlueprintMaterializer = workflowBlueprintMaterializer;
            _contentSerializer = contentSerializer;
        }

        public override async IAsyncEnumerable<IWorkflowBlueprint> GetWorkflowsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var blobs = await _storage.ListFilesAsync(new ListOptions(), cancellationToken);

            foreach (var blob in blobs)
            {
                var json = await _storage.ReadTextAsync(blob.FullPath, Encoding.UTF8, cancellationToken);
                var model = _contentSerializer.Deserialize<WorkflowDefinition>(json);
                var blueprint = _workflowBlueprintMaterializer.CreateWorkflowBlueprint(model);
                yield return blueprint;
            }
        }
    }
}