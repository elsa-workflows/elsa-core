using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Serialization;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using FluentStorage.Storage;

namespace Elsa.Providers.WorkflowStorage
{
    public class BlobStorageWorkflowStorageProvider : WorkflowStorageProvider
    {
        public const string ProviderName = "BlobStorage";

        private readonly IStore _blobStorage;
        private readonly JsonSerializerSettings _serializerSettings;

        public BlobStorageWorkflowStorageProvider(IOptions<BlobStorageWorkflowStorageProviderOptions> options)
        {
            _blobStorage = options.Value.BlobStorageFactory();
            _serializerSettings = DefaultContentSerializer.CreateDefaultJsonSerializationSettings();
            _serializerSettings.TypeNameHandling = TypeNameHandling.All;
        }

        public override string DisplayName => "Blob Storage";

        public override async ValueTask SaveAsync(WorkflowStorageContext context, string key, object? value, CancellationToken cancellationToken = default)
        {
            if (value == null)
                return;

            var path = GetFullPath(context, key);

            if (value is Stream stream)
            {
                await _blobStorage.SetObject(path, stream, cancellationToken: cancellationToken);
                var blob = await _blobStorage.GetObjectInfo(path, cancellationToken);
                blob.Metadata["ContentType"] = "Binary";
                await _blobStorage.SetObjectInfo(blob, cancellationToken);
            }
            else if (value is byte[] bytes)
            {
                await _blobStorage.SetBytes(path, bytes, cancellationToken: cancellationToken);
                var blob = await _blobStorage.GetObjectInfo(path, cancellationToken);
                blob.Metadata["ContentType"] = "Binary";
                await _blobStorage.SetObjectInfo(blob, cancellationToken);
            }
            else
            {
                var json = JsonConvert.SerializeObject(value, _serializerSettings);
                var jsonBytes = Encoding.UTF8.GetBytes(json);
                await _blobStorage.SetBytes(path, jsonBytes, cancellationToken: cancellationToken);
                var blob = await _blobStorage.GetObjectInfo(path, cancellationToken);
                blob.Metadata["ContentType"] = "Json";
                await _blobStorage.SetObjectInfo(blob, cancellationToken);
            }
        }

        public override async ValueTask<object?> LoadAsync(WorkflowStorageContext context, string key, CancellationToken cancellationToken = default)
        {
            var path = GetFullPath(context, key);

            if (!await _blobStorage.ObjectExists(path, cancellationToken))
                return null;

            var blob = await _blobStorage.GetObjectInfo(path, cancellationToken);
            var contentType = blob.Metadata.GetItem("ContentType") ?? "Json";

            if (contentType == "Json")
            {
                var json = await _blobStorage.GetText(path, cancellationToken: cancellationToken);
                return JsonConvert.DeserializeObject(json, _serializerSettings);
            }

            return await _blobStorage.GetBytes(path, cancellationToken);
        }

        public override async ValueTask DeleteAsync(WorkflowStorageContext context, string key, CancellationToken cancellationToken = default)
        {
            var path = GetFullPath(context, key);
            await _blobStorage.DeleteObject(path, cancellationToken);
        }

        public override async ValueTask DeleteAsync(WorkflowStorageContext context, CancellationToken cancellationToken = default)
        {
            var path = GetContainerPath(context);
            await _blobStorage.DeleteObject(path, cancellationToken);
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