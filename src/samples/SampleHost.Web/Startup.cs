using Elsa.Activities.Console.Extensions;
using Elsa.Activities.Http.Extensions;
using Elsa.Activities.Primitives.Extensions;
using Elsa.Extensions;
using Elsa.Persistence.FileSystem.Extensions;
using Esla.Runtime.Extensions;
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
                .AddWorkflowsCore()
                .AddWorkflowsHost()
                .AddWorkflowsFileSystemPersistence(Configuration.GetSection("FileStore"))
                .AddWorkflowsPrimitives()
                .AddWorkflowsConsole()
                .AddWorkflowsHttp();
            
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Latest);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpActivities();
            app.UseMvc();
        }
    }
}