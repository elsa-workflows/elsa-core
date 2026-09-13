using ConsoleLogStreaming.Core;
using ConsoleLogStreaming.Core.Capture;
using Elsa.Expressions.Helpers;
using Elsa.ModularServer.Web;
using Elsa.Server.Web;
using System.Text.Json.Nodes;

namespace Elsa.Hosts.SmokeTests;

/// <summary>Elsa.Server.Web, which registers its modules through the classic <c>Features/</c> path.</summary>
[InheritsTests]
[NotInParallel(HostSmokeTestConstraints.EntryPointProcessState)]
public class ClassicHostSmokeTests : HostSmokeTests<ClassicHostWebApplicationFactory, ClassicServerHost>
{
    private bool _originalStrictMode;

    /// <inheritdoc />
    protected override IReadOnlyCollection<string> GatedRoutes =>
    [
        "/elsa/api/workflow-definitions",
        "/elsa/api/workflow-instances",
        "/elsa/api/identity/roles",
        "/elsa/api/identity/permissions",
        "/elsa/api/dashboard/overview"
    ];

    protected override IReadOnlyDictionary<string, string?> CreateTestConfiguration()
    {
        var connectionString = GetConnectionString("classic.db");
        return new Dictionary<string, string?>
        {
            ["Multitenancy:Tenants:0:Configuration:ConnectionStrings:Sqlite"] = connectionString,
            ["Multitenancy:Tenants:1:Configuration:ConnectionStrings:Sqlite"] = connectionString,
            ["Multitenancy:Tenants:2:Configuration:ConnectionStrings:Sqlite"] = connectionString,
            ["Multitenancy:Tenants:0:Configuration:Http:Host"] = "localhost",
            ["Multitenancy:Tenants:1:Configuration:Http:Host"] = "localhost",
            ["Multitenancy:Tenants:2:Configuration:Http:Host"] = "localhost",
            ["Http:BaseUrl"] = "http://localhost",
            ["Scripting:Python:AllowHostCodeExecution"] = "false"
        };
    }

    protected override void CaptureEntryPointProcessState()
    {
        base.CaptureEntryPointProcessState();
        _originalStrictMode = ObjectConverter.StrictMode;
    }

    protected override ValueTask RestoreEntryPointProcessStateAsync()
    {
        ObjectConverter.StrictMode = _originalStrictMode;
        return base.RestoreEntryPointProcessStateAsync();
    }
}

/// <summary>Elsa.ModularServer.Web, which registers its modules through the CShells <c>ShellFeatures/</c> path.</summary>
[InheritsTests]
[NotInParallel(HostSmokeTestConstraints.EntryPointProcessState)]
public class ShellHostSmokeTests : HostSmokeTests<ModularHostWebApplicationFactory, ModularServerHost>
{
    /// <inheritdoc />
    /// <remarks>
    /// The two route sets overlap but are not identical: each lists what its own host actually configures,
    /// and External Authentication and User Tasks are enabled only here. Keeping them separate is the point --
    /// a route that disappears from one host and not the other is the divergence these tests are looking for.
    /// </remarks>
    protected override IReadOnlyCollection<string> GatedRoutes =>
    [
        "/elsa/api/workflow-definitions",
        "/elsa/api/workflow-instances",
        "/elsa/api/identity/permissions",
        "/elsa/api/external-authentication/connections",
        "/elsa/api/external-authentication/descriptors/adapters",
        "/elsa/api/user-tasks"
    ];

    protected override async Task SetupAsync()
    {
        // Recover from a previous partial host startup before Program installs the process-wide hook again.
        await ConsoleLogStreamingHost.ShutdownAsync();
        ConsoleStreamHook.Uninstall();
        await base.SetupAsync();
    }

    protected override IReadOnlyDictionary<string, string?> CreateTestConfiguration()
    {
        var connectionString = GetConnectionString("modular.db");
        var packageStore = CreateDirectory("package-store");
        var shellOverlayPath = Path.Join(TestRoot, "platform-shell-overrides.json");
        var overlayConfiguration = new Dictionary<string, string?>
        {
            ["CShells:Shells:Default:Features:SqliteWorkflowPersistence:ConnectionString"] = connectionString,
            ["CShells:Shells:Default:Features:SqliteIdentityPersistence:ConnectionString"] = connectionString,
            ["CShells:Shells:Default:Features:SqliteExternalAuthenticationPersistence:ConnectionString"] = connectionString,
            ["CShells:Shells:Default:Features:SqliteAlterationsPersistence:ConnectionString"] = connectionString,
            ["CShells:Shells:Default:Features:SqliteUserTasksPersistence:ConnectionString"] = connectionString,
            ["CShells:Shells:Default:Features:SqliteStructuredLogPersistence:ConnectionString"] = GetConnectionString("structured-logs.db"),
            ["CShells:Shells:Default:Features:Http:HttpActivityOptions:BaseUrl"] = "https://localhost",
            ["CShells:Shells:Default:Features:ExternalAuthentication:Redirects:ExternalCallbackBaseUri"] = "https://localhost/elsa/api/",
            ["CShells:Shells:Default:Features:ExternalAuthentication:AuthenticationClients:0:CallbackUris:0"] = "https://localhost/authentication/external/callback",
            ["CShells:Shells:Default:Features:ExternalAuthentication:AuthenticationClients:0:LogoutCallbackUris:0"] = "https://localhost/authentication/external/logout-callback",
            ["Diagnostics:OpenTelemetry:Exporter:Endpoint"] = "http://127.0.0.1:1",
            ["Elsa:PlatformIntegration:Enabled"] = "false",
            ["Nuplane:Setup:AutomaticReconciliation"] = "false",
            ["Nuplane:Setup:UseInMemoryStore"] = "true",
            ["Nuplane:Setup:Feeds:0:DirectoryPath"] = CreateDirectory("packages"),
            ["Nuplane:Setup:Feeds:0:Directory:Watch"] = "false",
            ["Nuplane:FeedResolution:PackageInstallRoot"] = packageStore,
            ["Nuplane:LockFile:Path"] = Path.Join(TestRoot, "nuplane.lock.json"),
            ["Nuplane:Loading:Enabled"] = "false",
            ["Nuplane:Loading:ActiveStoreRoot"] = packageStore
        };

        WriteOverlay(shellOverlayPath, overlayConfiguration);
        return new Dictionary<string, string?>
        {
            ["Elsa:PlatformIntegration:ShellOverlayPath"] = shellOverlayPath
        };
    }

    protected override async ValueTask RestoreEntryPointProcessStateAsync()
    {
        try
        {
            await ConsoleLogStreamingHost.ShutdownAsync();
        }
        finally
        {
            try
            {
                ConsoleStreamHook.Uninstall();
            }
            finally
            {
                await base.RestoreEntryPointProcessStateAsync();
            }
        }
    }

    private static void WriteOverlay(string path, IReadOnlyDictionary<string, string?> configuration)
    {
        var root = new JsonObject();

        foreach (var (key, value) in configuration)
        {
            var segments = key.Split(':');
            var section = root;

            foreach (var segment in segments[..^1])
            {
                if (section[segment] is not JsonObject child)
                {
                    child = new JsonObject();
                    section[segment] = child;
                }

                section = child;
            }

            section[segments[^1]] = value;
        }

        File.WriteAllText(path, root.ToJsonString());
    }
}

public sealed class ClassicHostWebApplicationFactory : HostWebApplicationFactory<ClassicServerHost>;

public sealed class ModularHostWebApplicationFactory : HostWebApplicationFactory<ModularServerHost>;
