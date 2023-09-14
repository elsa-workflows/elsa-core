using Elsa;
using Elsa.Models;
using Elsa.Server.Api;
using Elsa.Server.Api.Extensions;
using Elsa.Server.Api.Extensions.SchemaFilters;
using Elsa.Server.Api.Mapping;
using Elsa.Server.Api.Services;
using Elsa.Server.Api.Swagger.Examples;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddElsaApiEndpoints(this IServiceCollection services, ElsaApiOptions apiOptions) =>
            services.AddElsaApiEndpoints(options =>
            {
                options.SetupNewtonsoftJson = apiOptions.SetupNewtonsoftJson;
            });

        public static IServiceCollection AddElsaApiEndpoints(this IServiceCollection services, Action<ElsaApiOptions>? configureApiOptions = default)
        {
            var apiOptions = new ElsaApiOptions();
            configureApiOptions?.Invoke(apiOptions);

            var setupNewtonsoftJson = apiOptions.SetupNewtonsoftJson ?? (_ => { });

            //Don't set Newtonsoft globally
            services.AddControllers();//.AddNewtonsoftJson(setupNewtonsoftJson);
            services.AddRouting(options => { options.LowercaseUrls = true; });

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

            services
                .AddSingleton<ConnectionConverter>()
                .AddSingleton<ActivityBlueprintConverter>()
                .AddScoped<IWorkflowBlueprintMapper, WorkflowBlueprintMapper>()
                .AddSingleton<IEndpointContentSerializerSettingsProvider, EndpointContentSerializerSettingsProvider>()
                .AddAutoMapperProfile<AutoMapperProfile>()
                .AddSignalR();
            services.AddMvc(options =>
            {
                //Use this conventions to set ElsaNewtonsoftJsonConvention to all controllers in Elsa.Server.Api
                options.Conventions.Add(new ElsaNewtonsoftJsonConvention());
            });
            return services;
        }

        public static IServiceCollection AddElsaSwagger(this IServiceCollection services, Action<SwaggerGenOptions>? configure = default) =>
            services
                .AddSwaggerExamplesFromAssemblyOf<WorkflowDefinitionExample>()
                .AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Elsa", Version = "v1" });
                    c.EnableAnnotations();
                    //c.ExampleFilters(); I don't know why, this line will make swagger error
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

                    //Allow enums to be displayed
                    c.SchemaFilter<XEnumNamesSchemaFilter>();
                    configure?.Invoke(c);
                });
    }
}