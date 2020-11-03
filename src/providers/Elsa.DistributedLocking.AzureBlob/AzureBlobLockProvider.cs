using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.DistributedLock;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.RetryPolicies;
using Microsoft.Extensions.Logging;

namespace Elsa
{
    // See also:
    // The lock duration can be 15 to 60 seconds, or can be infinite, Reference: https://docs.microsoft.com/en-us/rest/api/storageservices/lease-blob

    public class AzureBlobLockProvider : IDistributedLockProvider
    {
        private const string Prefix = "elsa";
        private const string ContainerName = "elsa-lock-container";
        private const int MaxLeaseTime = 60;
        private const int MinLeaseTime = 15;
        private static readonly object SyncRoot = new object();
        private readonly AutoResetEvent _leaseSemaphore = new AutoResetEvent(true);
        private readonly List<LockedBlob> _lockedBlobs = new List<LockedBlob>();
        private readonly string _connectionString;
        private readonly TimeSpan _leaseTime;
        private readonly ILogger _logger;
        private readonly TimeSpan _renewInterval;
        private CloudBlobContainer? _cloudBlobContainer;
        private Timer _renewTimer = default!;

        public AzureBlobLockProvider(
            string connectionString,
            TimeSpan leaseTime,
            TimeSpan renewInterval,
            ILogger<AzureBlobLockProvider> logger)
        {
            _logger = logger;
            _connectionString = connectionString;
            
            if (leaseTime >= TimeSpan.FromSeconds(MaxLeaseTime) || leaseTime <= TimeSpan.FromSeconds(MinLeaseTime))
            {
                _logger.LogInformation("Lease time must be between 15 Seconds and 60 seconds, Found {leaseTime.TotalSeconds} seconds. Setting default value of 60 seconds", leaseTime.TotalSeconds);
                _leaseTime = TimeSpan.FromSeconds(MaxLeaseTime);
            }
            else
            {
                _leaseTime = leaseTime;
            }

            if (renewInterval > leaseTime)
            {
                _logger.LogError("Renew Interval can not be greater than  LeaseTime {leaseTime.TotalSeconds}.", leaseTime.TotalSeconds);
                throw new InvalidDataException($"Renew Interval can not be greater than  LeaseTime {leaseTime.TotalSeconds}.");
            }

            _renewInterval = renewInterval;
        }

        public Task<bool> AcquireLockAsync(string name, CancellationToken cancellationToken = default) => CreateLockAsync(name, cancellationToken);

        private async Task<bool> CreateLockAsync(string name, CancellationToken cancellationToken = default)
        {
            var resourceName = $"{Prefix}:{name}";
            var blob = CloudBlobContainer.GetBlockBlobReference(resourceName);

            if (!await blob.ExistsAsync(cancellationToken).ConfigureAwait(false))
                await blob.UploadTextAsync(string.Empty, cancellationToken).ConfigureAwait(false);

            _renewTimer = new Timer(RenewLeases, null, _renewInterval, _renewInterval);
            _logger.LogInformation("Lock provider will try to acquire lock for {resourceName}", resourceName);

            if (_leaseSemaphore.WaitOne())
            {
                try
                {
                    var leaseId = await blob.AcquireLeaseAsync(_leaseTime, null, cancellationToken).ConfigureAwait(false);
                    _lockedBlobs.Add(new LockedBlob { Blob = blob, LeaseId = leaseId, Identifier = resourceName });

                    _logger.LogInformation("Lock provider acquired lock for {resourceName}", resourceName);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to acquire lock for {resourceName}. Reason > {ex}", resourceName, ex);
                    return false;
                }
                finally
                {
                    _leaseSemaphore.Set();
                }
            }

            return false;
        }

        public Task ReleaseLockAsync(string name, CancellationToken cancellationToken = default)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return ReleaseLeaseAsync(name, cancellationToken);
        }

        private async Task ReleaseLeaseAsync(string name, CancellationToken cancellationToken = default)
        {
            var resourceName = $"{Prefix}:{name}";
            _logger.LogInformation("Lock provider will try to release lock for {resourceName}", resourceName);

            if (_leaseSemaphore.WaitOne())
            {
                try
                {
                    var lockedBlob = _lockedBlobs.FirstOrDefault(x => x.Identifier == resourceName);

                    if (lockedBlob != null)
                    {
                        try
                        {
                            await lockedBlob.Blob
                                .ReleaseLeaseAsync(AccessCondition.GenerateLeaseCondition(lockedBlob.LeaseId), cancellationToken)
                                .ConfigureAwait(false);

                            _logger.LogInformation("Lock provider released lock for {resourceName}", resourceName);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("Failed to release lock for {resourceName}. Reason > {ex}", resourceName, ex);
                        }

                        _lockedBlobs.Remove(lockedBlob);
                    }
                }
                finally
                {
                    _leaseSemaphore.Set();
                    _renewTimer?.Dispose();
                }
            }
        }

        private CloudBlobContainer CloudBlobContainer
        {
            get
            {
                if (_cloudBlobContainer == null)
                {
                    lock (SyncRoot)
                    {
                        if (_cloudBlobContainer == null)
                        {
                            var blobClient = CloudStorageAccount.Parse(_connectionString)
                                .CreateCloudBlobClient();
                            blobClient.DefaultRequestOptions.RetryPolicy = new ExponentialRetry(TimeSpan.FromSeconds(2.0), 3);
                            _cloudBlobContainer = blobClient.GetContainerReference(ContainerName);
                            if (!_cloudBlobContainer.Exists())
                            {
                                _cloudBlobContainer.CreateIfNotExists(BlobContainerPublicAccessType.Off);
                            }
                        }
                    }
                }

                return _cloudBlobContainer;
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
                        await RenewLock(lockedBlobs).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error while renewing leases");
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
                _logger.LogInformation("Renewed active leases for {lockedBlob.Identifier}", lockedBlob.Identifier);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while renewing lock");
            }
        }
    }
}