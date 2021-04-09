using System;
using ElsaDashboard.Backend.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ElsaDashboard.Samples.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public IConfiguration Configuration { get; }
        public IHostEnvironment Environment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();

            var elsaServerUrl = Configuration.GetValue<Uri>("Elsa:HostUrl");
            
            services
                .AddElsaDashboardUI(options => options.ElsaServerUrl = elsaServerUrl)
                .AddElsaDashboardBackend(options => options.ServerUrl = elsaServerUrl);
            
            if (Program.RuntimeModel == BlazorRuntimeModel.Server) 
                services.AddServerSideBlazor(options =>
                {
                    options.DetailedErrors = !Environment.IsProduction();
                    options.JSInteropDefaultCallTimeout = TimeSpan.FromSeconds(30);
                });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                
                if (Program.RuntimeModel == BlazorRuntimeModel.WebAssembly)
                    app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            if (Program.RuntimeModel == BlazorRuntimeModel.WebAssembly)
                app.UseBlazorFrameworkFiles();
            
            app.UseStaticFiles();
            app.UseRouting();
            app.UseElsaGrpcServices();
            
            app.UseEndpoints(endpoints =>
            {
                if (Program.RuntimeModel == BlazorRuntimeModel.Server)
                    endpoints.MapBlazorHub();
                
                endpoints.MapRazorPages();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}