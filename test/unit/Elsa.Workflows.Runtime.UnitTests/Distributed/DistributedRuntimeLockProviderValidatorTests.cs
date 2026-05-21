using Elsa.Common.DistributedHosting;
using Elsa.Common.DistributedHosting.DistributedLocks;
using Elsa.Workflows.Runtime.Distributed;
using Medallion.Threading;
using Medallion.Threading.FileSystem;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elsa.Workflows.Runtime.UnitTests.Distributed;

public class DistributedRuntimeLockProviderValidatorTests : IDisposable
{
    private readonly DirectoryInfo _lockDirectory;
    private readonly FileDistributedSynchronizationProvider _fileProvider;

    public DistributedRuntimeLockProviderValidatorTests()
    {
        _lockDirectory = Directory.CreateDirectory(Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        _fileProvider = new(_lockDirectory);
    }

    [Fact]
    public void Validate_Throws_WhenFileSystemProviderIsUsedWithoutOptIn()
    {
        var validator = CreateValidator(_fileProvider);

        var exception = Assert.Throws<InvalidOperationException>(validator.Validate);

        Assert.Contains(nameof(FileDistributedSynchronizationProvider), exception.Message);
        Assert.Contains(nameof(DistributedLockingOptions.AllowLocalLockProviderInDistributedRuntime), exception.Message);
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
    public void Validate_Throws_WhenProviderUsesFileSystemProviderThroughCustomProperty()
    {
        var validator = CreateValidator(new CustomWrappedDistributedLockProvider(_fileProvider));

        var exception = Assert.Throws<InvalidOperationException>(validator.Validate);

        Assert.Contains(nameof(FileDistributedSynchronizationProvider), exception.Message);
        Assert.Contains(nameof(CustomWrappedDistributedLockProvider), exception.Message);
    }

    [Fact]
    public void Validate_Throws_WhenProviderUsesFileSystemProviderThroughProviderCollection()
    {
        var validator = CreateValidator(new CompositeDistributedLockProvider([new CustomDistributedLockProvider(), _fileProvider]));

        var exception = Assert.Throws<InvalidOperationException>(validator.Validate);

        Assert.Contains(nameof(FileDistributedSynchronizationProvider), exception.Message);
        Assert.Contains(nameof(CompositeDistributedLockProvider), exception.Message);
    }

    [Fact]
    public void Validate_Throws_WhenProviderUsesFileSystemProviderThroughNonGenericProviderCollection()
    {
        System.Collections.IEnumerable providers = new IDistributedLockProvider[] { new CustomDistributedLockProvider(), _fileProvider };
        var validator = CreateValidator(new NonGenericCompositeDistributedLockProvider(providers));

        var exception = Assert.Throws<InvalidOperationException>(validator.Validate);

        Assert.Contains(nameof(FileDistributedSynchronizationProvider), exception.Message);
        Assert.Contains(nameof(NonGenericCompositeDistributedLockProvider), exception.Message);
    }

    [Fact]
    public void Validate_IgnoresObjectCollections_WhenTheyContainProviderInstances()
    {
        var validator = CreateValidator(new ObjectCollectionDistributedLockProvider([new CustomDistributedLockProvider(), _fileProvider]));

        validator.Validate();
    }

    [Fact]
    public void Validate_IgnoresNullEntries_WhenProviderCollectionContainsNull()
    {
        var validator = CreateValidator(new NullableCompositeDistributedLockProvider([new CustomDistributedLockProvider(), null]));

        validator.Validate();
    }

    [Fact]
    public void Validate_Throws_WhenNullableProviderCollectionContainsFileSystemProvider()
    {
        var validator = CreateValidator(new NullableCompositeDistributedLockProvider([new CustomDistributedLockProvider(), null, _fileProvider]));

        var exception = Assert.Throws<InvalidOperationException>(validator.Validate);

        Assert.Contains(nameof(FileDistributedSynchronizationProvider), exception.Message);
        Assert.Contains(nameof(NullableCompositeDistributedLockProvider), exception.Message);
    }

    [Fact]
    public void Validate_IgnoresThrowingProviderProperties()
    {
        var validator = CreateValidator(new ThrowingPropertyDistributedLockProvider());

        validator.Validate();
    }

    [Fact]
    public void Validate_IgnoresProviderCollectionsThatThrowDuringEnumeration()
    {
        var validator = CreateValidator(new ThrowingEnumerableDistributedLockProvider());

        validator.Validate();
    }

    [Fact]
    public void Validate_IgnoresProviderCollectionsThatThrowObjectDisposedDuringEnumeration()
    {
        var validator = CreateValidator(new ObjectDisposedEnumerableDistributedLockProvider());

        validator.Validate();
    }

    [Fact]
    public void Validate_UsesReferenceEquality_WhenProvidersOverrideEquality()
    {
        var validator = CreateValidator(new CompositeDistributedLockProvider([
            new ValueEqualDistributedLockProvider("same"),
            new ValueEqualDistributedLockProvider("same", _fileProvider)
        ]));

        var exception = Assert.Throws<InvalidOperationException>(validator.Validate);

        Assert.Contains(nameof(FileDistributedSynchronizationProvider), exception.Message);
        Assert.Contains(nameof(CompositeDistributedLockProvider), exception.Message);
    }

    [Fact]
    public void Validate_Throws_WhenNoopProviderIsUsedWithoutOptIn()
    {
        var validator = CreateValidator(new NoopDistributedSynchronizationProvider());

        var exception = Assert.Throws<InvalidOperationException>(validator.Validate);

        Assert.Contains(nameof(NoopDistributedSynchronizationProvider), exception.Message);
    }

    [Fact]
    public void Validate_DoesNotThrow_WhenNoopProviderIsExplicitlyAllowed()
    {
        var validator = CreateValidator(new NoopDistributedSynchronizationProvider(), options => options.AllowLocalLockProviderInDistributedRuntime = true);

        validator.Validate();
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

    public void Dispose()
    {
        try
        {
            _lockDirectory.Delete(true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DirectoryNotFoundException)
        {
            // Best-effort cleanup for temp directories that may still be held by the OS.
        }
    }

    private class WrappedDistributedLockProvider(IDistributedLockProvider innerProvider) : IDistributedLockProvider
    {
        public IDistributedLockProvider InnerProvider { get; } = innerProvider;

        public IDistributedLock CreateLock(string name) => InnerProvider.CreateLock(name);
    }

    private class CustomWrappedDistributedLockProvider(IDistributedLockProvider provider) : IDistributedLockProvider
    {
        public IDistributedLockProvider LockProvider { get; } = provider;

        public IDistributedLock CreateLock(string name) => LockProvider.CreateLock(name);
    }

    private class CompositeDistributedLockProvider(IReadOnlyCollection<IDistributedLockProvider> providers) : IDistributedLockProvider
    {
        public IReadOnlyCollection<IDistributedLockProvider> Providers { get; } = providers;

        public IDistributedLock CreateLock(string name) => Providers.First().CreateLock(name);
    }

    private class NonGenericCompositeDistributedLockProvider(System.Collections.IEnumerable providers) : IDistributedLockProvider
    {
        public System.Collections.IEnumerable Providers { get; } = providers;

        public IDistributedLock CreateLock(string name) => Providers.OfType<IDistributedLockProvider>().First().CreateLock(name);
    }

    private class NullableCompositeDistributedLockProvider(IReadOnlyCollection<IDistributedLockProvider?> providers) : IDistributedLockProvider
    {
        public IReadOnlyCollection<IDistributedLockProvider?> Providers { get; } = providers;

        public IDistributedLock CreateLock(string name) => Providers.OfType<IDistributedLockProvider>().First().CreateLock(name);
    }

    private class ObjectCollectionDistributedLockProvider(IReadOnlyCollection<object> providers) : IDistributedLockProvider
    {
        public IReadOnlyCollection<object> Providers { get; } = providers;

        public IDistributedLock CreateLock(string name) => new CustomDistributedLock(name);
    }

    private class ThrowingPropertyDistributedLockProvider : IDistributedLockProvider
    {
        public IDistributedLockProvider InnerProvider => throw new NotSupportedException("Property unavailable.");

        public IDistributedLock CreateLock(string name) => new CustomDistributedLock(name);
    }

    private class ThrowingEnumerableDistributedLockProvider : IDistributedLockProvider
    {
        public IEnumerable<IDistributedLockProvider> Providers => new ThrowingDistributedLockProviderEnumerable();

        public IDistributedLock CreateLock(string name) => new CustomDistributedLock(name);
    }

    private class ThrowingDistributedLockProviderEnumerable : IEnumerable<IDistributedLockProvider>
    {
        public IEnumerator<IDistributedLockProvider> GetEnumerator() => throw new NotSupportedException("Enumeration unavailable.");

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private class ObjectDisposedEnumerableDistributedLockProvider : IDistributedLockProvider
    {
        public System.Collections.IEnumerable Providers => new ObjectDisposedDistributedLockProviderEnumerable();

        public IDistributedLock CreateLock(string name) => new CustomDistributedLock(name);
    }

    private class ObjectDisposedDistributedLockProviderEnumerable : System.Collections.IEnumerable
    {
        public System.Collections.IEnumerator GetEnumerator() => throw new ObjectDisposedException(nameof(ObjectDisposedDistributedLockProviderEnumerable));
    }

    private class CustomDistributedLockProvider : IDistributedLockProvider
    {
        public IDistributedLock CreateLock(string name) => new CustomDistributedLock(name);
    }

    private class ValueEqualDistributedLockProvider(string key, IDistributedLockProvider? innerProvider = null) : IDistributedLockProvider
    {
        public IDistributedLockProvider? InnerProvider { get; } = innerProvider;

        public IDistributedLock CreateLock(string name) => InnerProvider?.CreateLock(name) ?? new CustomDistributedLock(name);

        public override bool Equals(object? obj) => obj is ValueEqualDistributedLockProvider provider && provider.Key == Key;

        public override int GetHashCode() => Key.GetHashCode();

        private string Key { get; } = key;
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
