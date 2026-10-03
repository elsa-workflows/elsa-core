using Elsa.Studio.Contracts;

namespace Elsa.Studio.Dashboard.Tests;

/// <summary>A feature service that reports it is initialized once <see cref="CompleteInitialization"/> is called.</summary>
internal sealed class StubFeatureService : IFeatureService
{
    private readonly List<Action> _subscribers = [];
    private readonly List<Action> _everSubscribed = [];

    public event Action? Initialized
    {
        add
        {
            _subscribers.Add(value!);
            _everSubscribed.Add(value!);
        }
        remove => _subscribers.Remove(value!);
    }

    public int SubscriberCount => _subscribers.Count;
    public bool IsInitialized { get; private set; }
    public IEnumerable<IFeature> GetFeatures() => [];
    public Task InitializeFeaturesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void CompleteInitialization()
    {
        IsInitialized = true;

        foreach (var subscriber in _subscribers.ToList())
            subscriber();
    }

    /// <summary>Raises the event to handlers that have since unsubscribed, as when it was raised from a copy of the subscribers.</summary>
    public void RaiseToEveryoneEverSubscribed()
    {
        foreach (var subscriber in _everSubscribed.ToList())
            subscriber();
    }
}
