using Elsa.Studio.Extensions;
using Elsa.Studio.ExternalAuthentication.Client;
using Elsa.Studio.ExternalAuthentication.Extensions;
using Elsa.Studio.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elsa.Studio.ExternalAuthentication.Tests.Compatibility;

/// <summary>
/// Elsa.Api.Client names each Refit HttpClient after the API's simple type name and resolves the host's authentication
/// handler as a scoped service. A module API sharing a simple name with a default Elsa.Api.Client API ends up with the
/// same handler instance added twice to one pipeline, which fails every call with "DelegatingHandler instances provided
/// to HttpMessageHandlerBuilder must not be reused or cached".
/// </summary>
public class ApiClientRegistrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public ApiClientRegistrationTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var backendApiConfig = new BackendApiConfig
        {
            ConfigureBackendOptions = options => options.Url = new Uri("https://backend.example/"),
            ConfigureHttpClientBuilder = options => options.AuthenticationHandler = typeof(PassthroughAuthenticationHandler)
        };

        services.AddRemoteBackend(backendApiConfig);
        services.AddExternalAuthenticationModule(backendApiConfig);
        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose() => _serviceProvider.Dispose();

    public static TheoryData<Type> ModuleApiTypes => new(typeof(IIdentityRolesApi).Assembly.GetTypes()
        .Where(type => type.IsInterface && type.Namespace == typeof(IIdentityRolesApi).Namespace));

    [Theory]
    [MemberData(nameof(ModuleApiTypes))]
    public void ModuleApiClient_BuildsItsHandlerPipeline(Type apiType)
    {
        var handlerFactory = _serviceProvider.GetRequiredService<IHttpMessageHandlerFactory>();

        var exception = Record.Exception(() => handlerFactory.CreateHandler(apiType.Name));

        Assert.Null(exception);
    }

    private sealed class PassthroughAuthenticationHandler : DelegatingHandler;
}
