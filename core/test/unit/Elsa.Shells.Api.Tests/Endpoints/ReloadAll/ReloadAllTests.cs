using System.Net;
using System.Net.Http.Json;
using CShells.Lifecycle;
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
    public async Task Post_WhenAnyShellFails_Returns503WithFailedStatus()
    {
        // CShells 0.0.15 surfaces per-shell failures via ReloadResult.Error rather than throwing.
        ShellRegistry
            .ReloadActiveAsync(Arg.Any<ReloadOptions?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ReloadResult>>(new[]
            {
                new ReloadResult("shell-a", null, null, new InvalidOperationException("Shell reload failed"))
            }));

        var response = await HttpClient.PostAsync("/shells/reload", null);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ShellReloadResult>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("Failed", body.Status);
        Assert.Contains("Shell reload failed", body.Message);
    }
}
