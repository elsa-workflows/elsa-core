using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Elsa.Authorization;
using Elsa.Common.Serialization;
using Elsa.Expressions.Contracts;
using Elsa.Workflows.Api.Permissions;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.LogPersistence;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Models;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using WorkflowsApiFeature = Elsa.Workflows.Api.Features.WorkflowsApiFeature;

namespace Elsa.Workflows.Api.UnitTests.Endpoints.Descriptors;

/// <summary>
/// The designer loads the descriptor catalogs, and a definition's versions, to open any definition, so a caller who can
/// only view definitions must be able to read them. The catalogs describe what is installed rather than anything
/// stored, so they need an authenticated caller and no grant; the version list is stored data, so it follows the
/// permission that already lets the caller read every version of a definition.
/// </summary>
[Collection(nameof(EndpointSecurityCollection))]
public class DescriptorCatalogAuthorizationTests : IAsyncLifetime
{
    private const string VersionsPath = "/workflow-definitions/my-definition/versions";
    private const string TypeName = "Elsa.Test";
    private const string OptionsPath = "/descriptors/activities/" + TypeName + "/options/Property";

    private static readonly string DefinitionsView = new Permission(WorkflowPermissions.Definitions, CoreVerbs.View).ToString();
    private static readonly string VersionsView = new Permission(WorkflowPermissions.DefinitionVersions, CoreVerbs.View).ToString();
    private static readonly string InstancesView = new Permission(WorkflowPermissions.Instances, CoreVerbs.View).ToString();
    private static readonly string DescriptorsActivitiesView = new Permission(WorkflowPermissions.DescriptorsActivities, CoreVerbs.View).ToString();

    private static readonly string[] CatalogNamespaces =
    [
        "Elsa.Workflows.Api.Endpoints.ActivityDescriptorOptions",
        "Elsa.Workflows.Api.Endpoints.ActivityDescriptors",
        "Elsa.Workflows.Api.Endpoints.CommitStrategies",
        "Elsa.Workflows.Api.Endpoints.IncidentStrategies",
        "Elsa.Workflows.Api.Endpoints.LogPersistenceStrategies",
        "Elsa.Workflows.Api.Endpoints.OutputConverters",
        "Elsa.Workflows.Api.Endpoints.Scripting.ExpressionDescriptors",
        "Elsa.Workflows.Api.Endpoints.StorageDrivers",
        "Elsa.Workflows.Api.Endpoints.VariableTypes",
        "Elsa.Workflows.Api.Endpoints.WorkflowActivationStrategies"
    ];

    private static readonly string[] CatalogPathList =
    [
        "/descriptors/activities",
        "/descriptors/activities/" + TypeName,
        "/descriptors/variables",
        "/descriptors/storage-drivers",
        "/descriptors/output-converters?sourceType=string&destinationType=string",
        "/descriptors/expression-descriptors",
        "/descriptors/workflow-activation-strategies",
        "/descriptors/incident-strategies",
        "/descriptors/log-persistence-strategies",
        "/descriptors/commit-strategies/activities",
        "/descriptors/commit-strategies/workflows"
    ];

    public static readonly TheoryData<string> CatalogPaths = new(CatalogPathList);
    public static readonly TheoryData<string> AuthenticatedOnlyPaths = new([.. CatalogPathList, VersionsPath]);

    private readonly bool _wasSecurityEnabled = EndpointSecurityOptions.SecurityIsEnabled;
    private readonly WebApplication _app;
    private readonly IActivityRegistryPopulator _registryPopulator = Substitute.For<IActivityRegistryPopulator>();

    public DescriptorCatalogAuthorizationTests()
    {
        EndpointSecurityOptions.SecurityIsEnabled = true;

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddFastEndpoints(options =>
        {
            options.Assemblies = [typeof(WorkflowsApiFeature).Assembly];
            options.Filter = endpoint => endpoint.Name == "ListVersions" || CatalogNamespaces.Any(x => endpoint.Namespace?.StartsWith(x, StringComparison.Ordinal) == true);
        });

        var activityRegistry = Substitute.For<IActivityRegistry>();
        activityRegistry.ListAll().Returns([new ActivityDescriptor { TypeName = TypeName }]);
        var activityLookup = Substitute.For<IActivityRegistryLookupService>();
        activityLookup.FindAsync(TypeName).Returns(new ActivityDescriptor { TypeName = TypeName });
        var expressionRegistry = Substitute.For<IExpressionDescriptorRegistry>();
        expressionRegistry.ListAll().Returns([]);
        var storageDrivers = Substitute.For<IStorageDriverManager>();
        storageDrivers.List().Returns([]);
        var logPersistence = Substitute.For<ILogPersistenceStrategyService>();
        logPersistence.ListStrategies().Returns([]);
        var commitStrategies = Substitute.For<ICommitStrategyRegistry>();
        commitStrategies.ListWorkflowStrategyRegistrations().Returns([]);
        commitStrategies.ListActivityStrategyRegistrations().Returns([]);
        var definitionStore = Substitute.For<IWorkflowDefinitionStore>();
        definitionStore.FindManyAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<WorkflowDefinitionOrder<int>>(), Arg.Any<CancellationToken>()).Returns([new WorkflowDefinition()]);

        builder.Services
            .AddSingleton(activityRegistry)
            .AddSingleton(_registryPopulator)
            .AddSingleton(activityLookup)
            .AddSingleton(Substitute.For<IPropertyUIHandlerResolver>())
            .AddSingleton(expressionRegistry)
            .AddSingleton(storageDrivers)
            .AddSingleton(logPersistence)
            .AddSingleton(commitStrategies)
            .AddSingleton(Substitute.For<IOutputConverterRegistry>())
            .AddSingleton(Substitute.For<IWellKnownTypeRegistry>())
            .AddSingleton(SerializationTypeRegistry.CreateDefault())
            .AddSingleton(definitionStore)
            .AddSingleton<IOptions<ManagementOptions>>(new OptionsWrapper<ManagementOptions>(new ManagementOptions()));

        builder.Services
            .AddAuthentication(PermissionHeaderAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, PermissionHeaderAuthenticationHandler>(PermissionHeaderAuthenticationHandler.SchemeName, _ => { });
        builder.Services.AddAuthorization();

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.UseFastEndpoints();
    }

    public Task InitializeAsync() => _app.StartAsync();

    public async Task DisposeAsync()
    {
        EndpointSecurityOptions.SecurityIsEnabled = _wasSecurityEnabled;
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    [Theory]
    [MemberData(nameof(CatalogPaths))]
    public async Task Catalog_AuthenticatedUserWithoutAnyPermission_IsAllowed(string path)
    {
        var response = await SendAsync(path, authenticated: true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEmpty(await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ActivityCatalog_AuthenticatedUserWithoutAnyPermission_ListsTheActivity()
    {
        var response = await SendAsync("/descriptors/activities", authenticated: true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(TypeName, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ActivityDescriptor_AuthenticatedUserWithoutAnyPermission_ReturnsTheDescriptor()
    {
        var response = await SendAsync("/descriptors/activities/" + TypeName, authenticated: true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(TypeName, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task VersionList_WithDefinitionsView_IsAllowed()
    {
        var response = await SendAsync(VersionsPath, DefinitionsView);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEmpty(await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [MemberData(nameof(AuthenticatedOnlyPaths))]
    public async Task AnonymousCaller_IsRejected(string path)
    {
        var response = await SendAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RefreshingTheActivityRegistry_WithOnlyDefinitionsView_IgnoresTheFlag()
    {
        var response = await SendAsync("/descriptors/activities?refresh=true", DefinitionsView);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(TypeName, await response.Content.ReadAsStringAsync());
        await _registryPopulator.DidNotReceiveWithAnyArgs().PopulateRegistryAsync(default);
    }

    [Fact]
    public async Task RefreshingTheActivityRegistry_WithActivityDescriptorsView_IsAllowed()
    {
        var response = await SendAsync("/descriptors/activities?refresh=true", DescriptorsActivitiesView);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(TypeName, await response.Content.ReadAsStringAsync());
        await _registryPopulator.Received(1).PopulateRegistryAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VersionList_WithoutDefinitionsView_IsForbidden()
    {
        var response = await SendAsync(VersionsPath, InstancesView);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task VersionList_WithOnlyVersionsView_IsForbidden()
    {
        // The documented behaviour change: the versions permission alone no longer lists versions.
        var response = await SendAsync(VersionsPath, VersionsView);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ActivityPropertyOptions_WithoutActivitiesView_IsForbidden()
    {
        var response = await SendAsync(OptionsPath, DefinitionsView, method: HttpMethod.Post);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RefreshingTheActivityRegistry_WithSecurityDisabled_RefreshesForAnyCaller()
    {
        EndpointSecurityOptions.SecurityIsEnabled = false;

        var response = await SendAsync("/descriptors/activities?refresh=true", authenticated: true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await _registryPopulator.Received(1).PopulateRegistryAsync(Arg.Any<CancellationToken>());
    }

    private Task<HttpResponseMessage> SendAsync(string path, string? permissions = null, bool authenticated = false, HttpMethod? method = null)
    {
        var request = new HttpRequestMessage(method ?? HttpMethod.Get, path);

        if (permissions != null)
        {
            request.Headers.Add(PermissionHeaderAuthenticationHandler.PermissionsHeaderName, permissions);
        }

        if (authenticated || permissions != null)
        {
            request.Headers.Add(PermissionHeaderAuthenticationHandler.UserHeaderName, "test-user");
        }

        return _app.GetTestClient().SendAsync(request);
    }

    /// <summary>
    /// Authenticates a caller that names itself in a header, holding the permissions named in another header (none when
    /// absent); a request without the user header stays anonymous.
    /// </summary>
    private sealed class PermissionHeaderAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "Header";
        public const string UserHeaderName = "X-Test-User";
        public const string PermissionsHeaderName = "X-Test-Permissions";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey(UserHeaderName))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = Request.Headers[PermissionsHeaderName].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => new Claim(PermissionNames.ClaimType, x));
            var identity = new ClaimsIdentity(claims, SchemeName);

            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
        }
    }
}

[CollectionDefinition(nameof(EndpointSecurityCollection), DisableParallelization = true)]
public class EndpointSecurityCollection;
