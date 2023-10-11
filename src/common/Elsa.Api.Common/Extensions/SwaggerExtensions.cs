using Elsa.Features.Services;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Extensions
{
    public static class SwaggerExtensions
    {
        /// <summary>
        /// Registers Swagger.
        /// </summary>
        public static IModule AddSwagger(this IModule module)
        {
            // Swagger API documentation
            module.Services.SwaggerDocument(o =>
            {
                o.EnableJWTBearerAuth = false;
                o.DocumentSettings = s =>
                {
                    s.DocumentName = "v3";
                    s.Title = "Elsa API";
                    s.Version = "v3.0";
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

        public static IApplicationBuilder UseSwaggerUI(this IApplicationBuilder app)
        {
            app.UseSwaggerGen();

            return app;
        }

    }
}
