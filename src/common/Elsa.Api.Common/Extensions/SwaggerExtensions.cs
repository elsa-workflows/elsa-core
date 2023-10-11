using Elsa.Features.Services;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Extensions
{
    /// <summary>
    /// Extensions to enable the generation of Swagger API Documentation and associated Swagger UI.
    /// </summary>
    public static class SwaggerExtensions
    {
        /// <summary>
        /// Registers Swagger document generator.
        /// </summary>
        public static IModule AddSwagger(this IModule module)
        {
            Version ver = new(3, 0);

            // Swagger API documentation
            module.Services.SwaggerDocument(o =>
            {
                o.EnableJWTBearerAuth = false;
                o.DocumentSettings = s =>
                {
                    s.DocumentName = $"v{ver.Major}";
                    s.Title = "Elsa API";
                    s.Version = $"v{ver.Major}.{ver.Minor}";
                    s.AddAuth("ApiKey", new()
                    {
                        Name = "Authorization",
                        In = NSwag.OpenApiSecurityApiKeyLocation.Header,
                        Type = NSwag.OpenApiSecuritySchemeType.ApiKey,
                        Description = "Enter: ApiKey [your API key]"
                    });
                };
            });

            return module;
        }

        /// <summary>
        /// Adds middleware to enable the Swagger UI at '/swagger'
        /// </summary>
        public static IApplicationBuilder UseSwaggerUI(this IApplicationBuilder app)
        {
            return app.UseSwaggerGen();
        }

    }
}
