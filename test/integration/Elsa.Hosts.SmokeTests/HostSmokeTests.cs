using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using TUnit.AspNetCore;

namespace Elsa.Hosts.SmokeTests;

/// <summary>
/// Boots a host the way its own entry point does and asserts it comes up serving gated routes.
/// </summary>
/// <remarks>
/// This repo runs two parallel feature systems -- the classic <c>Features/</c> path and the CShells
/// <c>ShellFeatures/</c> path -- and every module must register in both. Nothing exercised either until
/// now: the unit and integration suites construct services directly, so a service missing from one path,
/// or a feature registered in one and not the other, passes every test and fails only when a host starts.
/// Three such bugs in #7980 were found by running these two hosts by hand.
/// <para>
/// Each host is booted through <see cref="TestWebApplicationFactory{TEntryPoint}"/>, which runs the real
/// <c>Program</c> with its full feature registration. Assembling a service collection here instead would
/// reproduce exactly the blind spot these tests exist to close.
/// </para>
/// <para>
/// The assertions go through HTTP rather than by resolving services out of the container. The two hosts
/// have genuinely different container topologies -- the classic host is flat, while CShells gives each
/// shell its own provider, so the module services are simply not in the root one -- and a test that
/// reached into either would have to encode that difference and would break whenever CShells changed its
/// internals. Behaviour at the edge is both host-agnostic and the thing actually worth pinning: whichever
/// feature system a host uses, the observable result has to be the same.
/// </para>
/// </remarks>
public abstract class HostSmokeTests<TFactory, TEntryPoint> : WebApplicationTest<TFactory, TEntryPoint>, IAsyncDisposable
    where TFactory : HostWebApplicationFactory<TEntryPoint>, new()
    where TEntryPoint : class
{
    private string? _testRoot;
    private IReadOnlyDictionary<string, string?>? _testConfiguration;
    private bool _entryPointProcessStateCaptured;

    /// <summary>
    /// Routes this host is expected to serve behind a permission. Each names a different module, so the
    /// set doubles as an inventory of what this host's feature system is supposed to have registered.
    /// </summary>
    protected abstract IReadOnlyCollection<string> GatedRoutes { get; }

    [Test]
    public async Task HostStarts()
    {
        // TUnit.AspNetCore materializes the server before entering the test, which is where a feature that
        // fails to register or an option that fails validation throws.
        await Assert.That(Services).IsNotNull();
    }

    [Test]
    public async Task EveryGatedRouteChallengesInsteadOfFailing()
    {
        using var client = Factory.CreateClient();
        var problems = new List<string>();

        foreach (var route in GatedRoutes)
        {
            using var response = await client.GetAsync(route);
            var status = (int)response.StatusCode;

            // Each way this can go wrong is a distinct bug, so they are named rather than collapsed into one
            // "expected 401" message that leaves the reader to work out which failure they are looking at.
            var problem = status switch
            {
                404 => "404: this host never registered the module serving it",
                >= 500 => $"{status}: the endpoint was found but could not be activated, so a dependency is missing",
                200 => "200: reachable without credentials, so no permission gate ran",
                401 => null,
                _ => $"{status}: expected 401"
            };

            if (problem is not null)
                problems.Add($"{route} -> {problem}");
        }

        // Every route is reported at once: when a feature system stops registering a group of modules, one
        // failure per run turns a single cause into a queue of identical-looking investigations.
        await Assert.That(problems).IsEmpty()
            .Because($"{problems.Count} of {GatedRoutes.Count} gated route(s) did not challenge:{Environment.NewLine}{string.Join(Environment.NewLine, problems)}");
    }

    protected string TestRoot => _testRoot ?? throw new InvalidOperationException("The per-test host root has not been initialized.");

    protected string GetConnectionString(string fileName) => $"Data Source={Path.Join(TestRoot, fileName)};Pooling=False";

    protected string CreateDirectory(string name)
    {
        var path = Path.Join(TestRoot, name);
        Directory.CreateDirectory(path);
        return path;
    }

    protected abstract IReadOnlyDictionary<string, string?> CreateTestConfiguration();

    protected virtual void CaptureEntryPointProcessState()
    {
    }

    protected virtual ValueTask RestoreEntryPointProcessStateAsync() => ValueTask.CompletedTask;

    protected override async Task SetupAsync()
    {
        _testRoot = Path.GetFullPath(Path.Join(Path.GetTempPath(), $"elsa-hosts-smoke-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(_testRoot);
        CaptureEntryPointProcessState();
        _entryPointProcessStateCaptured = true;
        _testConfiguration = CreateTestConfiguration();
        GlobalFactory.SetStartupConfiguration(_testConfiguration);
        await base.SetupAsync();
    }

    protected override void ConfigureTestOptions(WebApplicationTestOptions options)
    {
        options.AutoConfigureOpenTelemetry = false;
        options.AutoPropagateHttpClientFactory = false;
    }

    protected override void ConfigureTestConfiguration(IConfigurationBuilder config) =>
        config.AddInMemoryCollection(_testConfiguration ?? throw new InvalidOperationException("The per-test host configuration has not been initialized."));

    /// <remarks>
    /// TUnit disposes the test instance after inherited <c>After(Test)</c> hooks, so the application factory
    /// and its SQLite connections are gone before this invocation-owned directory is removed.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        Exception? cleanupException = null;

        try
        {
            if (_entryPointProcessStateCaptured)
            {
                await RestoreEntryPointProcessStateAsync();
                _entryPointProcessStateCaptured = false;
            }
        }
        catch (Exception ex)
        {
            cleanupException = ex;
        }

        try
        {
            if (_testRoot is not null && Directory.Exists(_testRoot))
                Directory.Delete(_testRoot, true);
        }
        catch (Exception ex)
        {
            cleanupException = cleanupException is null ? ex : new AggregateException(cleanupException, ex);
        }

        if (cleanupException is not null)
            throw cleanupException;
    }
}

/// <summary>Runs a real top-level host while preserving TUnit.AspNetCore's host customization.</summary>
public abstract class HostWebApplicationFactory<TEntryPoint> : TestWebApplicationFactory<TEntryPoint> where TEntryPoint : class
{
    private static readonly IReadOnlyDictionary<string, string?> ReloadConfiguration = new Dictionary<string, string?>
    {
        ["HostBuilder:reloadConfigOnChange"] = "false"
    };

    private IReadOnlyDictionary<string, string?> _startupConfiguration = new Dictionary<string, string?>();

    internal void SetStartupConfiguration(IReadOnlyDictionary<string, string?> configuration) => _startupConfiguration = configuration;

    protected override void ConfigureStartupConfiguration(IConfigurationBuilder configurationBuilder)
    {
        base.ConfigureStartupConfiguration(configurationBuilder);
        configurationBuilder.AddInMemoryCollection(_startupConfiguration);
        configurationBuilder.AddInMemoryCollection(ReloadConfiguration);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        // Both hosts refuse to start outside Development while the signing key is a known default. That is
        // the guard working as intended, so the test satisfies it rather than configuring around it.
        builder.UseEnvironment(Environments.Development);
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // These are host-configuration values, not a deferred application-configuration callback. For a
        // minimal top-level Program, WebApplicationFactory turns them into command-line arguments before
        // invoking the entry point, so startup code sees the isolated paths before registering Nuplane feeds.
        builder.ConfigureHostConfiguration(configurationBuilder =>
        {
            configurationBuilder.AddInMemoryCollection(_startupConfiguration);
            configurationBuilder.AddInMemoryCollection(ReloadConfiguration);
        });

        return base.CreateHost(builder);
    }
}

internal static class HostSmokeTestConstraints
{
    public const string EntryPointProcessState = "ElsaHostsEntryPointProcessState";
}
