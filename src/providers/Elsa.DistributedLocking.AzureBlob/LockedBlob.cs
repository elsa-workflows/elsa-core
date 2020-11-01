using Microsoft.Azure.Storage.Blob;

namespace Elsa
{
    internal class LockedBlob
    {
        public string Identifier { get; set; }
        public string LeaseId { get; set; }
        public CloudBlockBlob Blob { get; set; }
    }
}
