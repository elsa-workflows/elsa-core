using System.Text.Json;
using System.Text.Json.Serialization;
using CShells.Lifecycle;
using Elsa.Shells.Api.ShellFeatures;
using Elsa.Workflows;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Shells.Api.Tests;

public abstract class ShellsApiTestBase : IAsyncLifetime
{
    private WebApplication? _app;
    private bool _wasSecurityEnabled;

    protected IShellRegistry ShellRegistry { get; } = Substitute.For<IShellRegistry>();
    protected HttpClient HttpClient { get; private set; } = null!;

    // Shared options that match the mock IApiSerializer — used when deserializing responses in tests.
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task InitializeAsync()
    {
        _wasSecurityEnabled = EndpointSecurityOptions.SecurityIsEnabled;
        EndpointSecurityOptions.SecurityIsEnabled = false;

        var apiSerializer = Substitute.For<IApiSerializer>();
        apiSerializer.GetOptions().Returns(JsonOptions);

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddFastEndpoints(o => o.Assemblies = [typeof(ShellsApiFeature).Assembly]);
        builder.Services.AddSingleton(ShellRegistry);
        builder.Services.AddSingleton(apiSerializer);
        builder.Services.AddLogging();

        // Default behavior: every reload succeeds. Tests override per-call by re-stubbing on the substitute.
        ShellRegistry.ReloadActiveAsync(Arg.Any<ReloadOptions?>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<IReadOnlyList<ReloadResult>>(Array.Empty<ReloadResult>()));
        ShellRegistry.ReloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci => Task.FromResult(new ReloadResult(ci.Arg<string>(), null, null, null)));

        _app = builder.Build();
        _app.UseFastEndpoints();

        await _app.StartAsync();
        HttpClient = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        EndpointSecurityOptions.SecurityIsEnabled = _wasSecurityEnabled;
        HttpClient.Dispose();
        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    // Local DTO mirroring the shape of the internal ShellReloadResponse returned by the endpoints.
    protected record ShellReloadResult(string Status, string? Message, string? RequestedShellId);
}
