using System.IO;
using Elsa.Providers.Workflows;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FluentStorage;

namespace Elsa.Samples.SignalApi
{
    public class Startup
    {
        private readonly IHostEnvironment _environment;

        public Startup(IConfiguration configuration, IHostEnvironment environment)
        {
            _environment = environment;
            Configuration = configuration;
        }
        
        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddControllers();
            
            services
                .AddElsa(options => options
                    .AddConsoleActivities()
                    .AddHttpActivities(httpOptions => Configuration.GetSection("Elsa:Http").Bind(httpOptions))
                    .AddQuartzTemporalActivities()
                );

            // Configure blob storage for blob storage workflow storage provider. 
            services.Configure<BlobStorageWorkflowProviderOptions>(options => options.BlobStorageFactory = () => StorageFactory.Blobs.DirectoryFiles(Path.Combine(_environment.ContentRootPath, "Workflows")));
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
            app.UseWelcomePage();
        }
    }
}