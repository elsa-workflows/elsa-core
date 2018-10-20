using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using OrchardCore.Modules;

namespace Flowsharp.Web.Management
{
    public class Startup : StartupBase
    {
        public override void Configure(IApplicationBuilder app, IRouteBuilder routes, IServiceProvider serviceProvider)
        {
            routes.MapAreaRoute
            (
                name: "Home",
                areaName: "Flowsharp.Web.Management",
                template: "",
                defaults: new {controller = "Home", action = "Index"}
            );
        }
    }
}