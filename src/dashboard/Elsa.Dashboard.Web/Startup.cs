using System.Collections.Generic;
using System.IO;
using Elsa.Dashboard.Extensions;
using Elsa.Dashboard.Models;
using Elsa.Dashboard.Web.Activities;
using Elsa.Persistence.EntityFrameworkCore.Extensions;
using Elsa.WorkflowDesigner;
using Elsa.WorkflowDesigner.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Elsa.Dashboard.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        private IConfiguration Configuration { get; }
        private IHostingEnvironment Environment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddMvc( /*options => options.Conventions.Add(new AddLocalhostFilterConvention())*/)
                .SetCompatibilityVersion(CompatibilityVersion.Latest)
                .AddRazorOptions(
                    options =>
                    {
                        // Workaround to get Razor views in Elsa.Dashboard recompiled when changing .cshtml.
                        if (Environment.IsDevelopment())
                            options.FileProviders.Add(
                                new PhysicalFileProvider(
                                    Path.Combine(Environment.ContentRootPath, @"..\Elsa.Dashboard")
                                )
                            );
                    }
                );

            services
                .AddEntityFrameworkCore(
                    options => options
                        .UseSqlite(Configuration.GetConnectionString("Sqlite"))
                )
                .AddEntityFrameworkCoreWorkflowDefinitionStore()
                .AddEntityFrameworkCoreWorkflowInstanceStore()
                .AddElsaDashboard(
                    options => options
                        .Bind(Configuration.GetSection("WorkflowDesigner"))
                        .Configure(
                            x => x.ActivityDefinitions
                                .Add(ActivityDescriber.Describe<SampleActivity>())
                                .Add(
                                    new ActivityDefinition
                                    {
                                        Type = "MyCustomActivity1",
                                        DisplayName = "My Custom Activity 1",
                                        Category = "Custom",
                                        Description = "Demonstrates adding custom activities to the designer",
                                        Properties = new[]
                                        {
                                            new ActivityPropertyDescriptor
                                            {
                                                Name = "Property1",
                                                Label = "Property 1",
                                                Type = ActivityPropertyTypes.Expression,
                                                Hint = "Specify any value you like.",
                                                Options = new WorkflowExpressionOptions
                                                {
                                                    Multiline = true
                                                }
                                            },
                                        },
                                        Designer = new ActivityDesignerSettings
                                        {
                                            Outcomes = new[] { OutcomeNames.Done }
                                        }
                                    }
                                )
                        )
                );
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app
                .UseDeveloperExceptionPage()
                .UseStaticFiles()
                .UseMvcWithDefaultRoute()
                .UseWelcomePage();
        }
    }
}