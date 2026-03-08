using System.Net;
using CShells;
using Elsa.Api.Client.Resources.Shells.Models;
using Elsa.Api.Client.Resources.Shells.Responses;
using Elsa.Testing.Shared.Extensions;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.RestApis.Endpoints.Shells;

public class ShellReloadFailureTests : AppComponentTest
{
    private readonly TestShellEnvironment _shellEnvironment;

    public ShellReloadFailureTests(App app) : base(app)
    {
        _shellEnvironment = app.WorkflowServer.Services.GetRequiredService<TestShellEnvironment>();
        _shellEnvironment.Reset();
    }

    [Fact]
    public async Task Post_WhenReloadIsBusy_ShouldReturnConflict()
    {
        _shellEnvironment.Reset([CreateShell("alpha", "v1")], [CreateShell("alpha", "v2")]);
        var gate = _shellEnvironment.BlockProvider();
        var client = WorkflowServer.CreateHttpClient();
        var firstRequestTask = client.PostAsync("actions/shells/reload", content: null);

        await Task.Delay(100);

        using var secondResponse = await client.PostAsync("actions/shells/reload", content: null);
        var secondModel = await secondResponse.ReadAsJsonAsync<ShellReloadResponse>(WorkflowServer.Services);
        gate.TrySetResult(true);
        using var firstResponse = await firstRequestTask;

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
        Assert.Equal(ShellReloadStatus.Busy, secondModel.Status);
    }

    [Fact]
    public async Task Post_WhenProviderIsUnavailable_ShouldReturnServiceUnavailable()
    {
        _shellEnvironment.Reset([CreateShell("alpha", "v1")], [CreateShell("alpha", "v2")]);
        _shellEnvironment.ProviderException = new InvalidOperationException("Shell settings provider is unavailable.");

        var client = WorkflowServer.CreateHttpClient();
        using var response = await client.PostAsync("actions/shells/reload", content: null);
        var model = await response.ReadAsJsonAsync<ShellReloadResponse>(WorkflowServer.Services);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(ShellReloadStatus.Failed, model.Status);
        Assert.Equal("v1", _shellEnvironment.FindCurrent("alpha")!.ConfigurationData["Version"]);
    }

    [Fact]
    public async Task Post_WhenOneShellIsInvalid_ShouldReturnPartialAndKeepPreviousConfiguration()
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
        using var response = await client.PostAsync("actions/shells/reload", content: null);
        var model = await response.ReadAsJsonAsync<ShellReloadResponse>(WorkflowServer.Services);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ShellReloadStatus.Partial, model.Status);
        Assert.Contains(model.Shells, x => x.ShellId == "alpha" && x.Outcome == ShellReloadItemOutcome.Reloaded);
        Assert.Contains(model.Shells, x => x.ShellId == "beta" && x.Outcome == ShellReloadItemOutcome.InvalidConfiguration);
        Assert.Equal("v2", _shellEnvironment.FindCurrent("alpha")!.ConfigurationData["Version"]);
        Assert.Equal("v1", _shellEnvironment.FindCurrent("beta")!.ConfigurationData["Version"]);
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