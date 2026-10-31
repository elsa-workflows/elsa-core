using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Common.Services;
using Elsa.Extensions;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Services;
using Elsa.Identity.Contracts;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Features;
using Elsa.Secrets.Persistence.EFCore.Extensions;
using Elsa.Secrets.Persistence.EFCore.Sqlite.Extensions;
using Elsa.Workflows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Secrets.UnitTests;

public sealed class SecretServiceLifetimeTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EntityFrameworkCoreSecretsServicesPassValidateOnBuild(bool withExternalAuthenticationBridge)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton<ISystemClock, SystemClock>();
        services.AddSingleton<ITenantAccessor, DefaultTenantAccessor>();
        var module = services.CreateModule();
        var secrets = module.Configure<SecretsFeature>();
        secrets.UseEntityFrameworkCore(feature => feature.UseSqlite("Data Source=:memory:"));
        module.Apply();
        if (withExternalAuthenticationBridge)
        {
            services.AddSingleton(Substitute.For<IUserProvider>());
            services.AddSingleton(Substitute.For<IUserStore>());
            services.AddSingleton(Substitute.For<IRoleProvider>());
            services.AddSingleton(Substitute.For<IElsaTokenService>());
            services.AddSingleton(Substitute.For<IRoleAuthorizationService>());
            services.AddSingleton(Substitute.For<IIdentityGenerator>());
            services.AddSingleton(Substitute.For<IUserCredentialsValidator>());
            services.AddSingleton(Substitute.For<IIdentityRefreshTokenService>());
            services.AddExternalAuthenticationServices();
            services.AddElsaSecretsExternalAuthentication();
        }

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });

        using var scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ISecretManager>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ISecretResolver>());
        if (withExternalAuthenticationBridge)
        {
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<ISecretBindingResolver>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<IManagedSecretBindingWriter>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<IIdentityProviderConnectionValidityAssessor>());
        }
    }
}
