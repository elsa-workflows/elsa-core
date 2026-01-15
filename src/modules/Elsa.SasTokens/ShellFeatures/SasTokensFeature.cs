using CShells.Features;
using Elsa.SasTokens.Contracts;
using JetBrains.Annotations;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.SasTokens.ShellFeatures;

/// <summary>
/// Adds the SAS tokens feature to the workflow runtime.
/// </summary>
[ShellFeature(
    DisplayName = "SAS Tokens",
    Description = "Provides shared access signature token generation and validation")]
[UsedImplicitly]
public class SasTokensFeature : IShellFeature
{
    private Func<IServiceProvider, ITokenService> TokenService { get; set; } = sp => ActivatorUtilities.CreateInstance<DataProtectorTokenService>(sp);

    public void ConfigureServices(IServiceCollection services)
    {
        var builder = services.AddDataProtection();
        builder.SetApplicationName("Elsa Workflows");

        services.AddScoped(TokenService);
    }
}
