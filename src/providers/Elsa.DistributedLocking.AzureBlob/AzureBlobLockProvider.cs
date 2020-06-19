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
    public class AzureBlobLockProvider : DistributedLockProvider
    {
        private const string Prefix = "elsa";
        private const string ContainerName = "elsa-lock-container"; 
        private readonly AutoResetEvent _leaseSemaphore = new AutoResetEvent(true);

        private readonly List<LockedBlob> _lockedBlobs = new List<LockedBlob>();
        private readonly string _connectionString;
        private TimeSpan _leaseTime;
        private TimeSpan _renewInterval;
        private Timer _renewTimer;
        private readonly ILogger<AzureBlobLockProvider> _logger;
        private CloudBlobContainer cloudBlobContainer;
        private readonly IClock _clock;

        public AzureBlobLockProvider(string connectionString, TimeSpan leaseTime, TimeSpan renewInterval,
                                        ILogger<AzureBlobLockProvider> logger,IClock clock)
        {
            _logger = logger;
            _clock = clock;
            _connectionString = connectionString;
            if (leaseTime > TimeSpan.FromSeconds(60) || leaseTime < TimeSpan.FromSeconds(15))
            {
                _logger.LogInformation($"Leasetime must be between 15 Seconds and 60 seconds, Found {leaseTime.TotalSeconds} seconds. " +
                                       $"Setting default value of 60 seconds");
                _leaseTime = TimeSpan.FromSeconds(60);
            }
            else
            {
                _leaseTime = leaseTime;
            }
            if (renewInterval > leaseTime)
            {
                _logger.LogError($"Renew Interval can not be greater than  LeaseTime {leaseTime.TotalSeconds}.");
                throw new InvalidDataException($"Renew Interval can not be greater than  LeaseTime {leaseTime.TotalSeconds}.");
            }
            _renewInterval = renewInterval;
        }
        public override async Task<bool> SetupAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (cloudBlobContainer == null)
                {
                    cloudBlobContainer = CloudBlobClient.GetContainerReference(ContainerName);
                    await cloudBlobContainer.CreateIfNotExistsAsync(cancellationToken);
                }

                _renewTimer = new Timer(RenewLeases, null, _renewInterval, _renewInterval);
                _logger.LogInformation($"Azure blob lock provider setup successful at {_clock.GetCurrentInstant()}. Elsa will use {ContainerName}.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unable to setup azure blob lock provider {ex}");
                throw;
            }

        }
        public override async Task<bool> AcquireLockAsync(string name, CancellationToken cancellationToken = default)
        {
            var blobName = $"{Prefix}:{name}";
            var blob = cloudBlobContainer.GetBlockBlobReference(blobName);
            if (!await blob.ExistsAsync())
                await blob.UploadTextAsync(string.Empty);

            _logger.LogInformation($"Lock provider will try to acquire lock for {blobName}");

            if (_leaseSemaphore.WaitOne())
            {
                try
                {
                    var leaseId = await blob.AcquireLeaseAsync(_leaseTime);
                    _lockedBlobs.Add(new LockedBlob { Blob = blob, LeaseId = leaseId, Identifier = blobName });
                    return true;
                }
                catch (StorageException ex)
                {
                    _logger.LogDebug($"Failed to acquire lock for {blobName} - {ex}");
                    return false;
                }
                finally
                {
                    _leaseSemaphore.Set();
                    _logger.LogInformation($"Lock provider acquired lock for {blobName}  at {_clock.GetCurrentInstant()}");
                }
            }
            return false;

        }
        public override async Task ReleaseLockAsync(string name, CancellationToken cancellationToken = default)
        {
            var blobName = $"{Prefix}:{name}";
            _logger.LogInformation($"Lock provider will try to release lock for {blobName}");
            if (_leaseSemaphore.WaitOne())
            {
                try
                {
                    var lockedBlob = _lockedBlobs.FirstOrDefault(x => x.Identifier == blobName);

                    if (lockedBlob != null)
                    {
                        try
                        {
                            await lockedBlob.Blob.ReleaseLeaseAsync(AccessCondition.GenerateLeaseCondition(lockedBlob.LeaseId));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error while releasing lock - {ex.Message}");
                        }
                        _lockedBlobs.Remove(lockedBlob);
                    }
                }
                finally
                {
                    _leaseSemaphore.Set(); 
                    _logger.LogInformation($"Lock provider released lock for {blobName}  at {_clock.GetCurrentInstant()}");

                }
            }
        }

        public override Task DisposeAsync(CancellationToken cancellationToken = default)
        {
            if (_renewTimer != null)
            {
                _renewTimer.Dispose();
                _renewTimer = null;
            }
            
            return Task.CompletedTask;
        }

        private CloudBlobClient CloudBlobClient
        {
            get
            {

                var blobClient = CloudStorageAccount.Parse(_connectionString)
                                                    .CreateCloudBlobClient();
                blobClient.DefaultRequestOptions.RetryPolicy = new ExponentialRetry(TimeSpan.FromSeconds(2.0), 3);
                return blobClient;
            }
        }


        private async void RenewLeases(object state)
        {
            _logger.LogDebug("Renew active leases");
            if (_leaseSemaphore.WaitOne())
            {

                try
                {
                    foreach (var lockedBlobs in _lockedBlobs)
                        await RenewLock(lockedBlobs);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error renewing leases - {ex.Message}");
                }
                finally
                {
                    _leaseSemaphore.Set();
                }
            }
        }

        private async Task RenewLock(LockedBlob lockedBlob)
        {
            try
            {
                await lockedBlob.Blob.RenewLeaseAsync(AccessCondition.GenerateLeaseCondition(lockedBlob.LeaseId));
                _logger.LogInformation($"Renewed active leases for {lockedBlob.Identifier}  at {_clock.GetCurrentInstant()}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while renewing lease");
            }
        }


    }
}
