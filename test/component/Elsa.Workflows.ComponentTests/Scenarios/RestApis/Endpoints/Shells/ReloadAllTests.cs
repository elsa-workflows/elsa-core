using System.Net;
using CShells;
using Elsa.Api.Client.Resources.Shells.Models;
using Elsa.Api.Client.Resources.Shells.Responses;
using Elsa.Testing.Shared.Extensions;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.RestApis.Endpoints.Shells;

public class ReloadAllTests : AppComponentTest
{
    private readonly TestShellEnvironment _shellEnvironment;

    public ReloadAllTests(App app) : base(app)
    {
        _shellEnvironment = app.WorkflowServer.Services.GetRequiredService<TestShellEnvironment>();
        _shellEnvironment.Reset();
    }

    [Fact]
    public async Task Post_ShouldReloadAllShellsAndReturnPerShellResults()
    {
        _shellEnvironment.Reset(
        [
            CreateShell("alpha", "v1"),
            CreateShell("beta", "v1")
        ],
        [
            CreateShell("alpha", "v2"),
            CreateShell("gamma", "v1")
        ]);

        var client = WorkflowServer.CreateHttpClient();
        using var response = await client.PostAsync("actions/shells/reload", content: null);
        var model = await response.ReadAsJsonAsync<ShellReloadResponse>(WorkflowServer.Services);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ShellReloadStatus.Completed, model.Status);
        Assert.Collection(model.Shells.OrderBy(x => x.ShellId),
            alpha =>
            {
                Assert.Equal("alpha", alpha.ShellId);
                Assert.Equal(ShellReloadItemOutcome.Reloaded, alpha.Outcome);
                Assert.False(alpha.Requested);
            },
            beta =>
            {
                Assert.Equal("beta", beta.ShellId);
                Assert.Equal(ShellReloadItemOutcome.Removed, beta.Outcome);
                Assert.False(beta.Requested);
            },
            gamma =>
            {
                Assert.Equal("gamma", gamma.ShellId);
                Assert.Equal(ShellReloadItemOutcome.Reloaded, gamma.Outcome);
                Assert.False(gamma.Requested);
            });

        Assert.Equal("v2", _shellEnvironment.FindCurrent("alpha")!.ConfigurationData["Version"]);
        Assert.Null(_shellEnvironment.FindCurrent("beta"));
        Assert.Equal("v1", _shellEnvironment.FindCurrent("gamma")!.ConfigurationData["Version"]);
    }

    private static ShellSettings CreateShell(string shellId, string version, bool invalid = false)
    {
        var shell = new ShellSettings(shellId, ["WorkflowsApi"]);
        shell.ConfigurationData["Version"] = version;

        if (invalid)
            shell.ConfigurationData["Test:IsInvalid"] = true;

        return shell;
    }
}