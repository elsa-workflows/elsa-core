using Elsa.Activities.Console.Extensions;
using Elsa.Activities.Email.Extensions;
using Elsa.Activities.Http.Extensions;
using Elsa.Activities.Primitives.Extensions;
using Elsa.Extensions;
using Elsa.Persistence.FileSystem.Extensions;
using Elsa.Runtime.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SampleHost.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddLocalization()
                .AddWorkflowsHost()
                .AddWorkflowsFileSystemPersistence(Configuration.GetSection("FileStore"))
                .AddPrimitiveWorkflowDrivers()
                .AddConsoleWorkflowDrivers()
                .AddHttpWorkflowDrivers()
                .AddEmailDrivers(Configuration.GetSection("Smtp"));
            
            services
                .AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Latest);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpWorkflows();
            app.UseMvc();
        }
    }
}