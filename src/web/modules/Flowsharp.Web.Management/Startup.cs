using System;
using Flowsharp.Serialization;
using Flowsharp.Serialization.Formatters;
using Flowsharp.Serialization.Tokenizers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Modules;

namespace Flowsharp.Web.Management
{
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services
                .AddSingleton<IWorkflowSerializer, WorkflowSerializer>()
                .AddSingleton<IWorkflowTokenizer, WorkflowTokenizer>()
                .AddSingleton<ITokenizerInvoker, TokenizerInvoker>()
                .AddSingleton<ITokenFormatter, YamlTokenFormatter>();
            
        }

        public override void Configure(IApplicationBuilder app, IRouteBuilder routes, IServiceProvider serviceProvider)
        {
//            routes.MapAreaRoute
//            (
//                name: "Home",
//                areaName: "Flowsharp.Web.Management",
//                template: "",
//                defaults: new {controller = "Home", action = "Index"}
//            );
        }
    }
}