using Elsa.Common.DistributedHosting;
using Elsa.Workflows.Runtime.Distributed;
using Medallion.Threading;
using Medallion.Threading.FileSystem;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elsa.Workflows.Runtime.UnitTests.Distributed;

public class DistributedRuntimeLockProviderValidatorTests
{
    private readonly FileDistributedSynchronizationProvider _fileProvider = new(new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))));

    [Fact]
    public void Validate_Throws_WhenFileSystemProviderIsUsedWithoutOptIn()
    {
        var validator = CreateValidator(_fileProvider);

        var exception = Assert.Throws<InvalidOperationException>(validator.Validate);

        Assert.Contains("local-only distributed lock provider", exception.Message);
        Assert.Contains(nameof(DistributedLockingOptions.AllowLocalLockProviderInDistributedRuntime), exception.Message);
        Assert.Contains("Redis, SQL Server, or PostgreSQL", exception.Message);
    }

    [Fact]
    public void Validate_DoesNotThrow_WhenFileSystemProviderIsExplicitlyAllowed()
    {
        var validator = CreateValidator(_fileProvider, options => options.AllowLocalLockProviderInDistributedRuntime = true);

        validator.Validate();
    }

    [Fact]
    public void Validate_Throws_WhenWrappedProviderUsesFileSystemProvider()
    {
        var validator = CreateValidator(new WrappedDistributedLockProvider(_fileProvider));

        var exception = Assert.Throws<InvalidOperationException>(validator.Validate);

        Assert.Contains(nameof(FileDistributedSynchronizationProvider), exception.Message);
        Assert.Contains(nameof(WrappedDistributedLockProvider), exception.Message);
    }

    [Fact]
    public void Validate_DoesNotThrow_WhenProviderIsNotKnownLocalOnly()
    {
        var validator = CreateValidator(new CustomDistributedLockProvider());

        validator.Validate();
    }

    private static DistributedRuntimeLockProviderValidator CreateValidator(IDistributedLockProvider provider, Action<DistributedLockingOptions>? configure = null)
    {
        var options = new DistributedLockingOptions();
        configure?.Invoke(options);

        return new(provider, Microsoft.Extensions.Options.Options.Create(options), NullLogger<DistributedRuntimeLockProviderValidator>.Instance);
    }

    private class WrappedDistributedLockProvider(IDistributedLockProvider innerProvider) : IDistributedLockProvider
    {
        public IDistributedLockProvider InnerProvider { get; } = innerProvider;

        public IDistributedLock CreateLock(string name) => InnerProvider.CreateLock(name);
    }

    private class CustomDistributedLockProvider : IDistributedLockProvider
    {
        public IDistributedLock CreateLock(string name) => new CustomDistributedLock(name);
    }

    private class CustomDistributedLock(string name) : IDistributedLock
    {
        public string Name { get; } = name;

        public IDistributedSynchronizationHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public IDistributedSynchronizationHandle? TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<IDistributedSynchronizationHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<IDistributedSynchronizationHandle?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
