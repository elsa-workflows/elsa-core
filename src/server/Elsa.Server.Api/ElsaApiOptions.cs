using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Server.Api
{
    public class ElsaApiOptions
    {
        public static void AddDefaultApiVersioning(IServiceCollection services)
        {
            services.AddVersionedApiExplorer(o =>
            {
                o.GroupNameFormat = "'v'VVV";
                o.SubstituteApiVersionInUrl = true;
            });

            services.AddApiVersioning(
                options =>
                {
                    options.ReportApiVersions = true;
                    options.DefaultApiVersion = ApiVersion.Default;
                    options.AssumeDefaultVersionWhenUnspecified = true;
                });
        }

        public Action<MvcNewtonsoftJsonOptions>? SetupNewtonsoftJson { get; set; } = default;
        public Action<IServiceCollection>? SetupApiVersioning { get; set; } = AddDefaultApiVersioning;

        public void DisableApiVersioning() => SetupApiVersioning = null;
    }
}