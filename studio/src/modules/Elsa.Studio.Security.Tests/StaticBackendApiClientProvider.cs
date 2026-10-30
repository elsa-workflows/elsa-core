using Elsa.Studio.Contracts;

namespace Elsa.Studio.Security.Tests;

internal sealed class StaticBackendApiClientProvider(params object[] apis) : IBackendApiClientProvider
{
    private readonly IReadOnlyDictionary<Type, object> _apis = apis.ToDictionary(x => x.GetType().GetInterfaces().Single(IsApiInterface));

    public Uri Url { get; } = new("https://elsa.example/");

    public ValueTask<T> GetApiAsync<T>(CancellationToken cancellationToken = default) where T : class =>
        ValueTask.FromResult((T)_apis[typeof(T)]);

    private static bool IsApiInterface(Type type) => type.Name.EndsWith("Api", StringComparison.Ordinal);
}
