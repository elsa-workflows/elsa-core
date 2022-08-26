using Elsa.Server.Api;
using Elsa.Server.Authentication.ExtensionOptions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using NetBox.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Server.Authentication.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddElsaCookieAuthentication(this IServiceCollection services, Action<ElsaCookieOptions>? configureOptions = default)
        {
            var elsaCookieOptions = new ElsaCookieOptions();
            configureOptions?.Invoke(elsaCookieOptions);
            services.AddAuthentication(options => {
                options.DefaultScheme = elsaCookieOptions.DefaultScheme;
            }).AddCookie(option => {
                option.LoginPath = elsaCookieOptions.LoginPath;
                option.Cookie.SameSite = elsaCookieOptions.SameSite;
                option.Cookie.Name = elsaCookieOptions.DefaultScheme;
            });
            return services;
        }

        public static IServiceCollection AddElsaOpenIdConnect(this IServiceCollection services, Action<ElsaOpenIdConnectOptions> configureOptions = default)
        {
            var elsaOpenIdConnectOptions = new ElsaOpenIdConnectOptions();
            configureOptions?.Invoke(elsaOpenIdConnectOptions);
            services.AddAuthentication(options => {
                options.DefaultScheme = elsaOpenIdConnectOptions.DefaultScheme;
                options.DefaultChallengeScheme = elsaOpenIdConnectOptions.DefaultChallengeScheme;
            }).AddCookie(option => {
                option.LoginPath = elsaOpenIdConnectOptions.LoginPath;
            }).AddOpenIdConnect(elsaOpenIdConnectOptions.DefaultScheme, options => {
                options.SignInScheme = elsaOpenIdConnectOptions.DefaultScheme;
                options.Authority = elsaOpenIdConnectOptions.Authority;
                options.ClientId = elsaOpenIdConnectOptions.ClientId;
                options.ResponseType = elsaOpenIdConnectOptions.ResponseType;
                options.UsePkce = elsaOpenIdConnectOptions.UsePkce;
                options.Scope.AddRange(elsaOpenIdConnectOptions.Scopes);
                options.SaveTokens = elsaOpenIdConnectOptions.SaveTokens;
                options.ClientSecret = elsaOpenIdConnectOptions.ClientSecret;
                options.GetClaimsFromUserInfoEndpoint = elsaOpenIdConnectOptions.GetClaimsFromUserInfoEndpoint;
            });
            return services;
        }
    }
}
