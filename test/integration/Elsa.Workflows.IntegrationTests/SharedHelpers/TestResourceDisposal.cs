namespace Elsa.Workflows.IntegrationTests.SharedHelpers;

internal static class TestResourceDisposal
{
    public static async ValueTask DisposeAsync(object? resource)
    {
        if (resource is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync();
        else if (resource is IDisposable disposable)
            disposable.Dispose();
    }
}
