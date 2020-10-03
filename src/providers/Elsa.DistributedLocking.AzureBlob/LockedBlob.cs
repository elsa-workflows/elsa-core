using Microsoft.Azure.Storage.Blob;

namespace Elsa.DistributedLocking.AzureBlob
{
    internal class LockedBlob
    {
        public string Identifier { get; set; }
        public string LeaseId { get; set; }
        public CloudBlockBlob Blob { get; set; }
    }
}
