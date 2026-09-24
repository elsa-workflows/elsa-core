using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Features;
using Elsa.Secrets.Persistence.EFCore.Extensions;
using Elsa.Secrets.Persistence.EFCore.Sqlite.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.UnitTests;

public sealed class SecretServiceLifetimeTests
{
    [Fact]
    public void EntityFrameworkCoreSecretsServicesPassValidateOnBuild()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton<ITenantAccessor, DefaultTenantAccessor>();
        var module = services.CreateModule();
        var secrets = module.Configure<SecretsFeature>();
        secrets.UseEntityFrameworkCore(feature => feature.UseSqlite("Data Source=:memory:"));
        module.Apply();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });

        using var scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ISecretManager>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ISecretResolver>());
    }
}
