using Elsa.Server.Authentication.Contexts;
using Elsa.Server.Authentication.Controllers;
using Elsa.Server.Authentication.ExtensionOptions;
using Elsa.Server.Authentication.TenantAccessors;
using Elsa.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NetBox.Extensions;
using System.Text;

namespace Elsa.Server.Authentication.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddElsaCookieAuthentication(this IServiceCollection services, Action<ElsaCookieOptions>? configureOptions = default)
        {
            var elsaCookieOptions = new ElsaCookieOptions();
            configureOptions?.Invoke(elsaCookieOptions);
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = elsaCookieOptions.DefaultScheme;
            }).AddCookie(option =>
            {
                option.LoginPath = elsaCookieOptions.LoginPath;
                option.Cookie.SameSite = elsaCookieOptions.SameSite;
                option.Cookie.Name = elsaCookieOptions.DefaultScheme;
            });

            ElsaAuthenticationContext.AuthenticationStyles.ToList().Add(AuthenticationStyles.ServerManagedCookie);
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
            ElsaAuthenticationContext.AuthenticationStyles.Add(AuthenticationStyles.OpenIdConnect);
            return services;
        }
        public static IServiceCollection AddElsaJwtBearerAuthentication(this IServiceCollection services, Action<ElsaJwtOptions>? configureOptions = default)
        {
            var elsaJwtOptions = new ElsaJwtOptions();
            configureOptions?.Invoke(elsaJwtOptions);
            services.AddAuthentication(elsaJwtOptions.DefaultJwtScheme).AddJwtBearer(elsaJwtOptions.DefaultJwtScheme, option => {
                option.RequireHttpsMetadata = elsaJwtOptions.RequireHttpsMetadata;
                option.SaveToken = elsaJwtOptions.SaveToken;
                option.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = elsaJwtOptions.ValidateIssuerSigningKey,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(elsaJwtOptions.IssuerSigningKey)),
                    ValidateIssuer = elsaJwtOptions.ValidateIssuer,
                    ValidIssuer = elsaJwtOptions.ValidIssuer,
                    ValidateAudience = elsaJwtOptions.ValidateAudience,
                    ValidAudience= elsaJwtOptions.Audience,
                };
            });
            ElsaAuthenticationContext.AuthenticationStyles.ToList().Add(AuthenticationStyles.JwtBearerToken);
            return services;
        }
        public static IServiceCollection AddElsaUserEndpoints(this IServiceCollection services)
        {
            services.AddControllers().AddApplicationPart((typeof(ElsaUserInfoController).Assembly));
            return services;
        }
        public static IServiceCollection AddTenantAccessorFromClaim(this IServiceCollection services,string claimName)
        {
            services.AddScoped<ITenantAccessor>(x=>new TenantAccessorFromClaim(x.GetService<IHttpContextAccessor>(), claimName));
            ElsaAuthenticationContext.CurrentTenantAccessorName = nameof(TenantAccessorFromClaim);
            ElsaAuthenticationContext.TenantAccessorKeyName = claimName;
            return services;
        }
        public static IServiceCollection AddTenantAccessorFromHeader(this IServiceCollection services, string header)
        {
            services.AddScoped<ITenantAccessor>(x => new TenantAccessorFromHeader(x.GetService<IHttpContextAccessor>(), header));
            ElsaAuthenticationContext.CurrentTenantAccessorName = nameof(TenantAccessorFromHeader);
            ElsaAuthenticationContext.TenantAccessorKeyName = header;
            return services;
        }
    }
}