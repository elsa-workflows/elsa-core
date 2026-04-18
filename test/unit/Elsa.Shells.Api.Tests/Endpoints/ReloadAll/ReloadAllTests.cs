using System.Net;
using System.Net.Http.Json;
using CShells.Management;
using NSubstitute;

namespace Elsa.Shells.Api.Tests.Endpoints.ReloadAll;

public class ReloadAllTests : ShellsApiTestBase
{
    [Fact]
    public async Task Post_WhenSuccessful_Returns200WithCompletedStatus()
    {
        var response = await HttpClient.PostAsync("/shells/reload", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ShellReloadResult>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("Completed", body.Status);
    }

    [Fact]
    public async Task Post_WhenManagerThrows_Returns503WithFailedStatus()
    {
        ShellManager
            .ReloadAllShellsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Shell reload failed")));

        var response = await HttpClient.PostAsync("/shells/reload", null);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ShellReloadResult>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("Failed", body.Status);
        Assert.Equal("Shell reload failed", body.Message);
    }
}
