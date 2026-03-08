using CShells;
using CShells.Configuration;
using CShells.Management;

namespace Elsa.Workflows.ComponentTests.Scenarios.RestApis.Endpoints.Shells;

public class TestShellEnvironment : IShellManager, IShellSettingsProvider, IShellSettingsCache
{
    private const string InvalidConfigurationKey = "Test:IsInvalid";
    private readonly object _lock = new();
    private Dictionary<string, ShellSettings> _currentShells = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, ShellSettings> _sourceShells = new(StringComparer.OrdinalIgnoreCase);
    private TaskCompletionSource<bool>? _providerGate;
    private TaskCompletionSource<bool>? _providerEntered;

    public Exception? ProviderException { get; set; }

    public void Reset(IEnumerable<ShellSettings>? currentShells = null, IEnumerable<ShellSettings>? sourceShells = null)
    {
        lock (_lock)
        {
            _currentShells = (currentShells ?? []).Select(Clone).ToDictionary(x => x.Id.Name, StringComparer.OrdinalIgnoreCase);
            _sourceShells = (sourceShells ?? currentShells ?? []).Select(Clone).ToDictionary(x => x.Id.Name, StringComparer.OrdinalIgnoreCase);
            ProviderException = null;
            _providerGate = null;
            _providerEntered = null;
        }
    }

    public (TaskCompletionSource<bool> Gate, Task Entered) BlockProvider()
    {
        lock (_lock)
        {
            _providerGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _providerEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
            return (_providerGate, _providerEntered.Task);
        }
    }

    public ShellSettings? FindCurrent(string shellId)
    {
        lock (_lock)
            return _currentShells.TryGetValue(shellId, out var settings) ? Clone(settings) : null;
    }

    public Task<IEnumerable<ShellSettings>> GetShellSettingsAsync(CancellationToken cancellationToken = default)
    {
        TaskCompletionSource<bool>? providerGate;
        Exception? providerException;
        IEnumerable<ShellSettings> settings;

        lock (_lock)
        {
            providerGate = _providerGate;
            _providerEntered?.TrySetResult(true);
            providerException = ProviderException;
            settings = _sourceShells.Values.Select(Clone).ToArray();
        }

        return GetShellSettingsInternalAsync(providerGate, providerException, settings, cancellationToken);
    }

    public IReadOnlyCollection<ShellSettings> GetAll()
    {
        lock (_lock)
            return _currentShells.Values.Select(Clone).ToArray();
    }

    public ShellSettings? GetById(ShellId id)
    {
        lock (_lock)
            return _currentShells.TryGetValue(id.Name, out var settings) ? Clone(settings) : null;
    }

    public Task AddShellAsync(ShellSettings settings, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _currentShells[settings.Id.Name] = Clone(settings);

            if (IsInvalid(settings))
                throw new InvalidOperationException($"Shell '{settings.Id.Name}' has invalid configuration.");
        }

        return Task.CompletedTask;
    }

    public Task RemoveShellAsync(ShellId shellId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
            _currentShells.Remove(shellId.Name);

        return Task.CompletedTask;
    }

    public Task UpdateShellAsync(ShellSettings settings, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _currentShells.Remove(settings.Id.Name);
            _currentShells[settings.Id.Name] = Clone(settings);

            if (IsInvalid(settings))
                throw new InvalidOperationException($"Shell '{settings.Id.Name}' has invalid configuration.");
        }

        return Task.CompletedTask;
    }

    public Task ReloadAllShellsAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
            _currentShells = _sourceShells.Values.Select(Clone).ToDictionary(x => x.Id.Name, StringComparer.OrdinalIgnoreCase);

        return Task.CompletedTask;
    }

    private static bool IsInvalid(ShellSettings settings) =>
        settings.ConfigurationData.TryGetValue(InvalidConfigurationKey, out var value) && value is true;

    private static async Task<IEnumerable<ShellSettings>> GetShellSettingsInternalAsync(TaskCompletionSource<bool>? providerGate, Exception? providerException, IEnumerable<ShellSettings> settings, CancellationToken cancellationToken)
    {
        if (providerGate != null)
            await providerGate.Task.WaitAsync(cancellationToken);

        if (providerException != null)
            throw providerException;

        return settings;
    }

    private static ShellSettings Clone(ShellSettings source)
    {
        var clone = new ShellSettings(source.Id, source.EnabledFeatures)
        {
            ConfigurationData = new Dictionary<string, object>(source.ConfigurationData, StringComparer.OrdinalIgnoreCase)
        };

        foreach (var configurator in source.FeatureConfigurators)
            clone.FeatureConfigurators[configurator.Key] = configurator.Value;

        return clone;
    }
}
