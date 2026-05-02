using System.Net;
using System.Net.Http.Json;
using CShells.Lifecycle;
using NSubstitute;

namespace Elsa.Shells.Api.Tests.Endpoints.Reload;

public class ReloadTests : ShellsApiTestBase
{
    private const string ShellId = "test-shell";

    [Fact]
    public async Task Post_WhenSuccessful_Returns200WithCompletedStatus()
    {
        var response = await HttpClient.PostAsync($"/shells/{ShellId}/reload", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ShellReloadResult>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("Completed", body.Status);
        Assert.Equal(ShellId, body.RequestedShellId);
    }

    [Fact]
    public async Task Post_WhenShellNotFound_Returns404WithNotFoundStatus()
    {
        // CShells 0.0.15 surfaces failures via ReloadResult.Error rather than throwing — the endpoint maps any
        // non-null Error to a 404 (the historical "blueprint not found" response).
        ShellRegistry
            .ReloadAsync(ShellId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ReloadResult(ShellId, null, null, new InvalidOperationException("Shell 'test-shell' not found"))));

        var response = await HttpClient.PostAsync($"/shells/{ShellId}/reload", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ShellReloadResult>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("NotFound", body.Status);
        Assert.Equal(ShellId, body.RequestedShellId);
        Assert.Equal("Shell 'test-shell' not found", body.Message);
    }
}
