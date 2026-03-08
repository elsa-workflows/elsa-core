using System.Net;
using CShells;
using Elsa.Api.Client.Resources.Shells.Models;
using Elsa.Api.Client.Resources.Shells.Responses;
using Elsa.Testing.Shared.Extensions;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.RestApis.Endpoints.Shells;

public class ReloadTests : AppComponentTest
{
    private readonly TestShellEnvironment _shellEnvironment;

    public ReloadTests(App app) : base(app)
    {
        _shellEnvironment = app.WorkflowServer.Services.GetRequiredService<TestShellEnvironment>();
        _shellEnvironment.Reset();
    }

    [Fact]
    public async Task Post_WithKnownShell_ShouldReturnRequestedShellResults()
    {
        _shellEnvironment.Reset(
        [
            CreateShell("alpha", "v1"),
            CreateShell("beta", "v1")
        ],
        [
            CreateShell("alpha", "v2"),
            CreateShell("beta", "v2", invalid: true)
        ]);

        var client = WorkflowServer.CreateHttpClient();
        using var response = await client.PostAsync("shells/alpha/reload", content: null);
        var model = await response.ReadAsJsonAsync<ShellReloadResponse>(WorkflowServer.Services);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ShellReloadStatus.Partial, model.Status);
        Assert.Equal("alpha", model.RequestedShellId);
        Assert.Contains(model.Shells, x => x.ShellId == "alpha" && x.Requested && x.Outcome == ShellReloadItemOutcome.Reloaded);
        Assert.Contains(model.Shells, x => x.ShellId == "beta" && !x.Requested && x.Outcome == ShellReloadItemOutcome.InvalidConfiguration);
        Assert.Equal("v2", _shellEnvironment.FindCurrent("alpha")!.ConfigurationData["Version"]);
        Assert.Equal("v1", _shellEnvironment.FindCurrent("beta")!.ConfigurationData["Version"]);
    }

    [Fact]
    public async Task Post_WithUnknownShell_ShouldReturnNotFound()
    {
        _shellEnvironment.Reset([CreateShell("alpha", "v1")], [CreateShell("alpha", "v2")]);

        var client = WorkflowServer.CreateHttpClient();
        using var response = await client.PostAsync("shells/missing/reload", content: null);
        var model = await response.ReadAsJsonAsync<ShellReloadResponse>(WorkflowServer.Services);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(ShellReloadStatus.NotFound, model.Status);
        Assert.Single(model.Shells);
        Assert.Equal(ShellReloadItemOutcome.Unknown, model.Shells.Single().Outcome);
    }

    [Fact]
    public async Task Post_WhenRequestedShellFails_ShouldReturnUnprocessableEntity()
    {
        _shellEnvironment.Reset(
        [
            CreateShell("alpha", "v1"),
            CreateShell("beta", "v1")
        ],
        [
            CreateShell("alpha", "v2", invalid: true),
            CreateShell("beta", "v2")
        ]);

        var client = WorkflowServer.CreateHttpClient();
        using var response = await client.PostAsync("shells/alpha/reload", content: null);
        var model = await response.ReadAsJsonAsync<ShellReloadResponse>(WorkflowServer.Services);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(ShellReloadStatus.RequestedShellFailed, model.Status);
        Assert.Contains(model.Shells, x => x.ShellId == "alpha" && x.Requested && x.Outcome == ShellReloadItemOutcome.InvalidConfiguration);
        Assert.Contains(model.Shells, x => x.ShellId == "beta" && x.Outcome == ShellReloadItemOutcome.Reloaded);
        Assert.Equal("v1", _shellEnvironment.FindCurrent("alpha")!.ConfigurationData["Version"]);
        Assert.Equal("v2", _shellEnvironment.FindCurrent("beta")!.ConfigurationData["Version"]);
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