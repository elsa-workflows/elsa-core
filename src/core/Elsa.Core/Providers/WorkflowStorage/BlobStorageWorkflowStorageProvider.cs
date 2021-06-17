using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Serialization;
using Newtonsoft.Json;
using Storage.Net.Blobs;

namespace Elsa.Providers.WorkflowStorage
{
    public class BlobStorageWorkflowStorageProvider : WorkflowStorageProvider
    {
        public const string ProviderName = "BlobStorage";
        
        private readonly IBlobStorage _blobStorage;
        private readonly JsonSerializerSettings _serializerSettings;

        public BlobStorageWorkflowStorageProvider(IBlobStorage blobStorage)
        {
            _blobStorage = blobStorage;
            _serializerSettings = DefaultContentSerializer.CreateDefaultJsonSerializationSettings();
            _serializerSettings.TypeNameHandling = TypeNameHandling.All;
        }
        
        public override string DisplayName => "Blob Storage";

        public override async ValueTask SaveAsync(WorkflowStorageContext context, string key, object? value, CancellationToken cancellationToken = default)
        {
            if (value == null)
                return;
            
            var path = GetFullPath(context, key);
            
            if(value is Stream stream)
            {
                await _blobStorage.WriteAsync(path, stream, cancellationToken: cancellationToken);
                var blob = await _blobStorage.GetBlobAsync(path, cancellationToken);
                blob.Metadata["ContentType"] = "Binary";
            }
            else if (value is byte[] bytes)
            {
                await _blobStorage.WriteAsync(path, bytes, cancellationToken: cancellationToken);
                var blob = await _blobStorage.GetBlobAsync(path, cancellationToken);
                blob.Metadata["ContentType"] = "Binary";
            }
            else
            {
                var json = JsonConvert.SerializeObject(value, _serializerSettings);
                var jsonBytes = Encoding.UTF8.GetBytes(json);
                await _blobStorage.WriteAsync(path, jsonBytes, cancellationToken: cancellationToken);
                var blob = await _blobStorage.GetBlobAsync(path, cancellationToken);
                blob.Metadata["ContentType"] = "Json";
            }
        }

        public override async ValueTask<object?> LoadAsync(WorkflowStorageContext context, string key, CancellationToken cancellationToken = default)
        {
            var path = GetFullPath(context, key);
            
            if (!await _blobStorage.ExistsAsync(path, cancellationToken))
                return null;
            
            var blob = await _blobStorage.GetBlobAsync(path, cancellationToken);
            var contentType = blob.Metadata.GetItem("ContentType") ?? "Json";

            if (contentType == "Json")
            {
                var json = await _blobStorage.ReadTextAsync(path, cancellationToken: cancellationToken);
                return JsonConvert.DeserializeObject(json, _serializerSettings);
            }

            return await _blobStorage.ReadBytesAsync(path, cancellationToken);
        }

        public override async ValueTask DeleteAsync(WorkflowStorageContext context, string key, CancellationToken cancellationToken = default)
        {
            var path = GetFullPath(context, key);
            await _blobStorage.DeleteAsync(path, cancellationToken);
        }

        public override async ValueTask DeleteAsync(WorkflowStorageContext context, CancellationToken cancellationToken = default)
        {
            var path = GetContainerPath(context);
            await _blobStorage.DeleteAsync(path, cancellationToken);
        }
        
        private string GetFullPath(WorkflowStorageContext context, string key)
        {
            var containerPath = GetContainerPath(context);
            var activityId = context.ActivityId;
            return $"${containerPath}/{activityId}/{key}.dat";
        }
        
        private string GetContainerPath(WorkflowStorageContext context) => context.WorkflowInstance.Id;
    }
}