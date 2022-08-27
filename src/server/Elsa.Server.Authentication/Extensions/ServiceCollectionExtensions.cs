using Elsa.Server.Api;
using Elsa.Server.Authentication.ExtensionOptions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
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

        public static IServiceCollection AddElsaOpenIdConnect(this IServiceCollection services,string authenticationScheme , Action<ElsaOpenIdConnectOptions>? configureOptions = default)
        {
            var elsaOpenIdConnectOptions = new ElsaOpenIdConnectOptions();
            configureOptions?.Invoke(elsaOpenIdConnectOptions);
            services.AddAuthentication(options => {
                options.DefaultScheme = elsaOpenIdConnectOptions.DefaultScheme;
                options.DefaultChallengeScheme = elsaOpenIdConnectOptions.DefaultChallengeScheme;
            }).AddCookie(option => {
                option.LoginPath = elsaOpenIdConnectOptions.LoginPath;
            }).AddOpenIdConnect(authenticationScheme, options => {
                options.SignInScheme = elsaOpenIdConnectOptions.DefaultScheme;
                options.Authority = elsaOpenIdConnectOptions.Authority;
                options.ClientId = elsaOpenIdConnectOptions.ClientId;
                options.ResponseType = elsaOpenIdConnectOptions.ResponseType;
                options.UsePkce = elsaOpenIdConnectOptions.UsePkce;
                options.Scope.AddRange(elsaOpenIdConnectOptions.Scopes);
                foreach (var item in elsaOpenIdConnectOptions.UniqueJsonKeys)
                {
                    options.ClaimActions.MapUniqueJsonKey(item.Key, item.Value);
                }
               
                options.SaveTokens = elsaOpenIdConnectOptions.SaveTokens;
                options.ClientSecret = elsaOpenIdConnectOptions.ClientSecret;
                options.GetClaimsFromUserInfoEndpoint = elsaOpenIdConnectOptions.GetClaimsFromUserInfoEndpoint;
            });
            return services;
        }
    }
}



//services.AddAuthentication(options => {
//    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
//    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
//}).AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options => {

//    options.LoginPath = "/signin-oidc";
//}

// ).AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options => {
//     options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
//     options.Authority = "https://localhost:44318/";
//     options.ClientId = "ElsaDashboardClientServer";
//     options.ResponseType = "code";
//     options.UsePkce = false;
//     options.Scope.Add("openid");
//     options.Scope.Add("profile");
//     options.SaveTokens = true;
//     options.ClientSecret = "Elsa";
//     options.GetClaimsFromUserInfoEndpoint = true;
// });
