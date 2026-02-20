using Medallion.Threading;

namespace Elsa.Workflows.ComponentTests.Scenarios.DistributedLockResilience.Mocks;

/// <summary>
/// A lock provider that selectively mocks specific locks while allowing others to use the real implementation.
///
/// WHY THIS IS NECESSARY:
/// - Workflow operations trigger background processes (trigger indexing, state persistence, etc.)
/// - These background operations acquire their own locks concurrently
/// - If we mock ALL locks globally, background operations can consume configured failures
/// - This makes test assertions unreliable and flaky
///
/// SOLUTION:
/// - Only mock locks matching specific prefixes configured by tests
/// - Background operations use real locks (not counted, not mocked)
/// - Test operations use mocked locks (counted, failures injected)
/// - Result: Deterministic, reliable test assertions
/// </summary>
public class SelectiveMockLockProvider : IDistributedLockProvider, IDisposable
{
    private readonly IDistributedLockProvider _realProvider;
    private readonly Dictionary<string, TestDistributedLockProvider> _mockProvidersByPrefix = new();
    private readonly object _lock = new();

    public SelectiveMockLockProvider(IDistributedLockProvider realProvider)
    {
        _realProvider = realProvider;
    }

    /// <summary>
    /// Gets the real/inner provider being wrapped.
    /// </summary>
    public IDistributedLockProvider RealProvider => _realProvider;

    /// <summary>
    /// Configures mocking for locks matching the specified prefix.
    /// Returns a test provider that allows configuring failures for these locks.
    /// </summary>
    public TestDistributedLockProvider MockLock(string lockNamePrefix)
    {
        lock (_lock)
        {
            if (!_mockProvidersByPrefix.TryGetValue(lockNamePrefix, out var mockProvider))
            {
                mockProvider = new TestDistributedLockProvider(_realProvider);
                _mockProvidersByPrefix[lockNamePrefix] = mockProvider;
            }
            return mockProvider;
        }
    }

    /// <summary>
    /// Removes mocking for the specified lock prefix, allowing it to use the real provider.
    /// </summary>
    public void Unmock(string lockNamePrefix)
    {
        lock (_lock)
        {
            _mockProvidersByPrefix.Remove(lockNamePrefix);
        }
    }

    /// <summary>
    /// Clears all mock configurations, resetting to real provider for all locks.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            foreach (var mockProvider in _mockProvidersByPrefix.Values)
            {
                mockProvider.Reset();
            }
            _mockProvidersByPrefix.Clear();
        }
    }

    /// <summary>
    /// Creates a lock that will be mocked if it matches a configured prefix, otherwise uses the real provider.
    /// </summary>
    public IDistributedLock CreateLock(string name)
    {
        lock (_lock)
        {
            // Check if this lock name matches any configured mock prefix
            foreach (var (prefix, mockProvider) in _mockProvidersByPrefix)
            {
                if (name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return mockProvider.CreateLock(name);
                }
            }

            // No mock configured, use real provider
            return _realProvider.CreateLock(name);
        }
    }

    public void Dispose()
    {
        Reset();
    }
}
