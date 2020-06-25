using Elsa.DistributedLock;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.RetryPolicies;
using Microsoft.Extensions.Logging;
using NodaTime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.DistributedLocking.AzureBlob
{
    internal class LockedBlob
    {
        public string Identifier { get; set; }
        public string LeaseId { get; set; }
        public CloudBlockBlob Blob { get; set; }
    }
}
