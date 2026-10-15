using Microsoft.Extensions.DependencyModel;

namespace Elsa.Secrets.DefaultHost.IntegrationTests;

/// <summary>
/// Issue #8301 criterion 2: the default consolidated host exposes one canonical Core endpoint for every shared Secrets
/// route, and the legacy Secrets endpoint package is inactive.
/// </summary>
[Collection(nameof(WorkbenchHostCollection))]
public class SecretsEnabledWorkbenchTests(SecretsEnabledWorkbench host) : IClassFixture<SecretsEnabledWorkbench>
{
    [Fact]
    public void EachCanonicalRouteHasExactlyOneCoreEndpointAndNoLegacyRouteIsRegistered()
    {
        var violations = SecretsApiContract.Pinned.Violations(host.Routes());

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void NoLegacySecretsAssemblyIsLoadedOrDeployedWithTheHost()
    {
        // Core Secrets endpoints are live, so FastEndpoints has already loaded every endpoint assembly it was given.
        Assert.Contains(host.Routes(), SecretsApiContract.Pinned.IsSecretsRoute);
        var loaded = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetName().Name).ToList();
        var deployed = DependencyContext.Load(typeof(Program).Assembly)!.RuntimeLibraries.Select(library => library.Name).ToList();

        var legacyOnlyPackages = SecretsApiContract.Pinned.LegacyOnlyPackages;
        Assert.NotEmpty(legacyOnlyPackages);
        Assert.Contains(SecretsApiContract.CoreAssembly, loaded);
        Assert.Contains(SecretsApiContract.CoreAssembly, deployed);
        Assert.Empty(legacyOnlyPackages.Intersect(loaded));
        Assert.Empty(legacyOnlyPackages.Intersect(deployed));
    }
}

[Collection(nameof(WorkbenchHostCollection))]
public class DefaultWorkbenchTests(DefaultWorkbench host) : IClassFixture<DefaultWorkbench>
{
    [Fact]
    public void DefaultConfigurationRegistersNoSecretsRoute()
    {
        var routes = host.Routes();

        // The host did map its workflow API, so an empty Secrets set is not an empty enumeration.
        Assert.Contains(routes, route => route.HandlerAssembly == "Elsa.Workflows.Api");
        Assert.DoesNotContain(routes, SecretsApiContract.Pinned.IsSecretsRoute);
    }
}

[Collection(nameof(WorkbenchHostCollection))]
public class WorkbenchWithLegacyRoutesTests(WorkbenchWithLegacyRoutes host) : IClassFixture<WorkbenchWithLegacyRoutes>
{
    [Fact]
    public void RouteCheckReportsEveryLegacyRouteMountedBesideCoreAndNothingElse()
    {
        var contract = SecretsApiContract.Pinned;

        var reported = contract.Violations(host.Routes()).Select(violation => violation.RouteKey).Distinct().Order();

        Assert.Equal(contract.LegacyRoutes.Select(route => SecretsApiContract.Key(route.Verb, route.Path)).Distinct().Order(), reported);
    }
}

/// <summary>
/// The Workbench <c>Program</c> sets process-wide static state, so two of its hosts must never boot at the same time in
/// one test process.
/// </summary>
[CollectionDefinition(nameof(WorkbenchHostCollection), DisableParallelization = true)]
public sealed class WorkbenchHostCollection;
