using Microsoft.Azure.Storage.Blob;

namespace Elsa
{
    internal class LockedBlob
    {
        public string Identifier { get; set; } = default!;
        public string LeaseId { get; set; } = default!;
        public CloudBlockBlob Blob { get; set; } = default!;
    }
}