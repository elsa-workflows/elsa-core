using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NodaTime;
using System;

namespace Elsa.DistributedLocking.AzureBlob
{
    public static class ElsaOptionsExtensions
    {
        public static ElsaOptions UseAzureBlobLockProvider(this ElsaOptions options, string connectionString, TimeSpan? leaseTime=null, TimeSpan? renewInterval=null)
        {
            options
                .UseDistributedLockProvider(sp => new AzureBlobLockProvider(connectionString,
                                                                        leaseTime.GetValueOrDefault(TimeSpan.FromSeconds(60)),
                                                                        renewInterval.GetValueOrDefault(TimeSpan.FromSeconds(45)),
                                                                        sp.GetRequiredService<ILogger<AzureBlobLockProvider>>(),
                                                                        sp.GetRequiredService<IClock>()));
            return options;
        }
    }
}
