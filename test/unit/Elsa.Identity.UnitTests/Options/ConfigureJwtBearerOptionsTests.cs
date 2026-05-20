using Elsa.Extensions;
using Elsa.Identity.Constants;
using Elsa.Identity.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Elsa.Identity.UnitTests.Options;

public class ConfigureJwtBearerOptionsTests
{
    private const string SigningKey = "test-signing-key-with-at-least-32-chars";

    [Fact]
    public async Task Configure_UsesAccessTokenValidationForDefaultBearerScheme()
    {
        var options = Configure(JwtBearerDefaults.AuthenticationScheme);
        var result = await IdentityTokenOptionsTokenUseTests.ValidateTokenUseAsync(options, actualTokenUse: TokenUse.Refresh);

        Assert.NotNull(result.Failure);
    }

    [Fact]
    public async Task Configure_UsesRefreshTokenValidationForRefreshTokenScheme()
    {
        var options = Configure(IdentityAuthenticationSchemes.RefreshToken);
        var result = await IdentityTokenOptionsTokenUseTests.ValidateTokenUseAsync(options, actualTokenUse: TokenUse.Access);

        Assert.NotNull(result.Failure);
    }

    private static JwtBearerOptions Configure(string scheme)
    {
        var configureOptions = new ConfigureJwtBearerOptions(Microsoft.Extensions.Options.Options.Create(new IdentityTokenOptions
        {
            SigningKey = SigningKey
        }));
        var options = new JwtBearerOptions();

        configureOptions.Configure(scheme, options);

        return options;
    }
}
