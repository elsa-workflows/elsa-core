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
        ShellRegistry
            .ReloadAsync(ShellId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ReloadResult>(new ShellBlueprintNotFoundException(ShellId)));

        var response = await HttpClient.PostAsync($"/shells/{ShellId}/reload", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ShellReloadResult>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("NotFound", body.Status);
        Assert.Equal(ShellId, body.RequestedShellId);
        Assert.Contains(ShellId, body.Message);
    }

    [Fact]
    public async Task Post_WhenReloadFails_Returns503WithFailedStatus()
    {
        ShellRegistry
            .ReloadAsync(ShellId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ReloadResult(ShellId, null, null, new InvalidOperationException("Shell reload failed"))));

        var response = await HttpClient.PostAsync($"/shells/{ShellId}/reload", null);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ShellReloadResult>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("Failed", body.Status);
        Assert.Equal(ShellId, body.RequestedShellId);
        Assert.Equal("Shell reload failed", body.Message);
    }

    [Fact]
    public async Task Post_WhenBlueprintUnavailable_Returns503WithFailedStatus()
    {
        ShellRegistry
            .ReloadAsync(ShellId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ReloadResult>(new ShellBlueprintUnavailableException(ShellId, new InvalidOperationException())));

        var response = await HttpClient.PostAsync($"/shells/{ShellId}/reload", null);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ShellReloadResult>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("Failed", body.Status);
        Assert.Equal(ShellId, body.RequestedShellId);
        Assert.Contains(ShellId, body.Message);
    }
}
