using Elsa.Api.Common.Implementations;
using Elsa.Api.Common.Options;
using Elsa.Api.Common.Services;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Api.Common.Features;

public class CommonApiFeature : FeatureBase
{
    public CommonApiFeature(IModule module) : base(module)
    {
    }

    public string TokenSigningKey { get; set; } = default!;
    public Func<IServiceProvider, ICredentialsValidator> CredentialsValidator { get; set; } = ActivatorUtilities.GetServiceOrCreateInstance<AllowAnyCredentialsValidator>;
    public Func<IServiceProvider, IAccessTokenIssuer> AccessTokenIssuer { get; set; } = ActivatorUtilities.GetServiceOrCreateInstance<AdminAccessTokenIssuer>;
    
    public override void Apply()
    {
        Services.Configure<AccessTokenOptions>(options => options.SigningKey = TokenSigningKey);
        Services.AddSingleton(sp => CredentialsValidator(sp));
        Services.AddSingleton(sp => AccessTokenIssuer(sp));
    }
}