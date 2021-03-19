﻿using System;
using Elsa;
using Elsa.Models;
using Elsa.Server.Api.Mapping;
using Elsa.Server.Api.Services;
using Elsa.Server.Api.Swagger.Examples;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddElsaApiEndpoints(this IServiceCollection services, Action<MvcNewtonsoftJsonOptions>? setupNewtonsoftJson = default)
        {
            setupNewtonsoftJson ??= _ => { }; 
            services.AddControllers().AddNewtonsoftJson(setupNewtonsoftJson);
                
            services.AddRouting(options => options.LowercaseUrls = true);

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

            services.AddSingleton<ConnectionConverter>();
            services.AddSingleton<ActivityBlueprintConverter>();
            services.AddSingleton<IWorkflowBlueprintMapper, WorkflowBlueprintMapper>();
            services.AddAutoMapperProfile<AutoMapperProfile>();
            return services;
        }

        public static IServiceCollection AddElsaSwagger(this IServiceCollection services, Action<SwaggerGenOptions>? configure = default) =>
            services
                .AddSwaggerExamplesFromAssemblyOf<WorkflowDefinitionExample>()
                .AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo {Title = "Elsa", Version = "v1"});
                    c.EnableAnnotations();
                    c.ExampleFilters();
                    c.MapType<VersionOptions?>(() => new OpenApiSchema
                    {
                        Type = PrimitiveType.String.ToString().ToLower(),
                        Example = new OpenApiString("Latest"),
                        Description = "Any of Latest, Published, Draft, LatestOrPublished or a specific version number.",
                        Nullable = true,
                        Default = new OpenApiString("Latest")
                    });
                    
                    c.MapType<Type>(() => new OpenApiSchema
                    {
                        Type = PrimitiveType.String.ToString().ToLower(),
                        Example = new OpenApiString("System.String, mscorlib")
                    });

                    configure?.Invoke(c);
                });
    }
}