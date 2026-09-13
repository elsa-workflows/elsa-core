using System.Text.Json;
using System.Text.Json.Serialization;
using CShells.Lifecycle;
using Elsa.Workflows;
using FastEndpoints;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using TUnit.AspNetCore;

namespace Elsa.Shells.Api.Tests;

public abstract class ShellsApiTestBase : WebApplicationTest<ShellsApiWebApplicationFactory, ShellsApiTestEntryPoint>
{
    private HttpClient? _httpClient;

    protected ShellsApiTestBase()
    {
        ShellRegistry.ReloadActiveAsync(Arg.Any<ReloadOptions?>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<IReadOnlyList<ReloadResult>>(Array.Empty<ReloadResult>()));
        ShellRegistry.ReloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(new ReloadResult(callInfo.Arg<string>(), null, null, null)));
    }

    protected IShellRegistry ShellRegistry { get; } = Substitute.For<IShellRegistry>();

    protected HttpClient HttpClient => _httpClient ??= Factory.CreateClient();

    protected JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        var apiSerializer = Substitute.For<IApiSerializer>();
        apiSerializer.GetOptions().Returns(JsonOptions);

        services.AddSingleton(ShellRegistry);
        services.AddSingleton(apiSerializer);
    }

    protected record ShellReloadResult(string Status, string? Message, string? RequestedShellId);
}
