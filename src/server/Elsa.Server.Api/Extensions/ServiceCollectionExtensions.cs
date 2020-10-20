using Elsa.Server.Api.Extensions;
using Microsoft.AspNetCore.Mvc;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddElsaApiEndpoints(this IServiceCollection services)
        {
            services.AddControllers().AddJsonSerialization();
            services.AddRouting(options => options.LowercaseUrls = true);

            services.AddApiVersioning(
                options =>
                {
                    options.ReportApiVersions = true;
                    options.DefaultApiVersion = ApiVersion.Default;
                    options.AssumeDefaultVersionWhenUnspecified = true;
                });

            return services;
        }
    }
}