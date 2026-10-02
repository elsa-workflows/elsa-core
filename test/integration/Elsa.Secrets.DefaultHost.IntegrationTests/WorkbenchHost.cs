using Elsa.Workflows.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Quartz.Logging;

namespace Elsa.Secrets.DefaultHost.IntegrationTests;

/// <summary>
/// Boots the Workbench sample host (<c>samples/extensions/workbench/Elsa.Server.Web</c>) through its own <c>Program</c>,
/// with its tracked <c>appsettings.json</c> as the configuration.
/// </summary>
/// <remarks>
/// Settings are passed as launch arguments, which the top-level statements in <c>Program</c> read before the host is
/// built. The only one always overridden is the SQLite connection string, which points at a private temporary database
/// instead of the tracked <c>App_Data</c>. <c>Features:Secrets:Enabled</c> is set only where requested, which is the same
/// explicit launch override the #8326 live replay used.
/// </remarks>
public abstract class WorkbenchHost(bool secretsEnabled) : WebApplicationFactory<Program>
{
    private readonly DirectoryInfo _dataDirectory = Directory.CreateTempSubdirectory("elsa-secrets-default-host-");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Quartz keeps the logger factory of the host that configured it in a process-wide static. Without this reset, the
        // next Workbench host booted in this process fails while Quartz logs through the previous host's disposed factory.
        LogContext.SetCurrentLogProvider(NullLoggerFactory.Instance);
        builder.UseSetting("ConnectionStrings:Sqlite", $"Data Source={Path.Combine(_dataDirectory.FullName, "workbench.sqlite")}");
        if (secretsEnabled)
        {
            builder.UseSetting("Features:Secrets:Enabled", "true");
        }
    }

    /// <summary>Every route the host registered, read from the same endpoint data source its router uses.</summary>
    public IReadOnlyList<RegisteredRoute> Routes()
    {
        var routePrefix = ApiRoutePrefix(Services.GetRequiredService<IOptions<ApiEndpointOptions>>());
        return Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .SelectMany(endpoint => RegisteredRoute.From(endpoint, routePrefix))
            .ToList();
    }

    protected static string ApiRoutePrefix(IOptions<ApiEndpointOptions> options) => "/" + options.Value.RoutePrefix.Trim('/');

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        SqliteConnection.ClearAllPools();
        _dataDirectory.Delete(recursive: true);
    }
}

public sealed class SecretsEnabledWorkbench() : WorkbenchHost(secretsEnabled: true);

public sealed class DefaultWorkbench() : WorkbenchHost(secretsEnabled: false);

/// <summary>
/// The Secrets-enabled Workbench with every legacy route from the contract also mapped under the Elsa API prefix, where
/// a mounted legacy endpoint package would register it. This is the positive control for the route contract check.
/// </summary>
public sealed class WorkbenchWithLegacyRoutes() : WorkbenchHost(secretsEnabled: true)
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services => services.AddTransient<IStartupFilter, LegacyRouteMount>());
    }

    private sealed class LegacyRouteMount(IOptions<ApiEndpointOptions> options) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            next(app);
            var routePrefix = ApiRoutePrefix(options);
            app.UseEndpoints(endpoints =>
            {
                foreach (var route in SecretsApiContract.Pinned.LegacyRoutes)
                {
                    endpoints.MapMethods(routePrefix + route.Path, [route.Verb], () => Results.Ok());
                }
            });
        };
    }
}
