using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace Elsa.DistributedLocking.AzureBlob
{
    public static class ElsaOptionsExtensions
    {
        public static ElsaOptions UseAzureBlobLockProvider(this ElsaOptions options, string connectionString, TimeSpan? leaseTime=null, TimeSpan? renewInterval=null)
        {
            options
                .UseDistributedLockProvider(sp => new AzureBlobLockProvider(connectionString,
                                                                        leaseTime ?? TimeSpan.FromSeconds(60),
                                                                        renewInterval ?? TimeSpan.FromSeconds(45),
                                                                        sp.GetRequiredService<ILogger<AzureBlobLockProvider>>()));
            return options;
        }
    }
}
