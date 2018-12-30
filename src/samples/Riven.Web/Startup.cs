using Flowsharp.Activities.Console.Extensions;
using Flowsharp.Activities.Http.Extensions;
using Flowsharp.Activities.Primitives.Extensions;
using Flowsharp.Extensions;
using Flowsharp.Persistence.FileSystem.Extensions;
using Flowsharp.Runtime.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Riven.Web
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