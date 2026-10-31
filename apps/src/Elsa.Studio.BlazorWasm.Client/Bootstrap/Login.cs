using Elsa.Studio.BlazorServer.Helpers;
using Elsa.Studio.Login.BlazorWasm.Extensions;
using Elsa.Studio.Login.Extensions;
using Shared.Constants;
using Shared.Options;
using AuthenticationOptions = Shared.Options.AuthenticationOptions;

namespace Elsa.Studio.BlazorWasm.Client.Bootstrap
{
    public static class Login
    {
        public static IServiceCollection ConfigureLogin(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddLoginModule();

            var identityOptions = configuration.GetOptions<IdentityOptions>(SettingKeys.Identity);
            var authenticationOptions = configuration.GetOptionsOrDefault<AuthenticationOptions>(SettingKeys.Authentication);

            switch (identityOptions.Type)
            {
                case IdentityOptionType.ElsaIdentity:
                    services.UseElsaIdentity();
                    break;

                case IdentityOptionType.OpenIdConnect:
                    ConfigureOpenIdConnect(services, authenticationOptions);
                    break;

                case IdentityOptionType.OAuth2:
                    ConfigureOAuth2(services, authenticationOptions);
                    break;

                default:
                    services.UseElsaIdentity();
                    break;
            }

            return services;
        }

        private static void ConfigureOpenIdConnect(IServiceCollection services, AuthenticationOptions? authenticationOptions)
        {
            EnsureAuthOptionsNotNull(authenticationOptions);
            services.UseOpenIdConnect(configuration =>
            {
                configuration.TokenEndpoint = authenticationOptions!.TokenEndpoint;
                configuration.AuthEndpoint = authenticationOptions.AuthorizationEndpoint!;
                configuration.ClientId = authenticationOptions.ClientId;
                configuration.ClientSecret = authenticationOptions.ClientSecret;
                configuration.Scopes = authenticationOptions.Scopes;
                configuration.UsePkce = authenticationOptions.UsePkce;
            });
        }

        private static void ConfigureOAuth2(IServiceCollection services, AuthenticationOptions? authenticationOptions)
        {
            EnsureAuthOptionsNotNull(authenticationOptions);

            services.UseOAuth2(configuration =>
            {
                configuration.TokenEndpoint = authenticationOptions!.TokenEndpoint;
                configuration.ClientId = authenticationOptions.ClientId;
                configuration.ClientSecret = authenticationOptions.ClientSecret;
                configuration.Scope = string.Join(' ', authenticationOptions.Scopes);
            });
        }

        static void EnsureAuthOptionsNotNull(AuthenticationOptions? options)
        {
            if (options is null)
            {
                throw new InvalidProgramException($"Authentication options must be provided when using OpenID Connect or OAuth2 authentication.");
            }
        }
   }
}
