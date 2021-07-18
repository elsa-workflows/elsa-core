using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Persistence.EntityFramework.Sqlite;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ElsaDashboard.Samples.AspNetCore.SubPath
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
            services.AddRazorPages();
            
            // Elsa Server.
            var elsaSection = Configuration.GetSection("Elsa");

            services
                .AddElsa(options => options
                    .UseEntityFrameworkPersistence(ef => ef.UseSqlite())
                    .AddConsoleActivities()
                    .AddHttpActivities(elsaSection.GetSection("Server").Bind)
                    .AddEmailActivities(elsaSection.GetSection("Smtp").Bind)
                    .AddQuartzTemporalActivities()
                    .AddJavaScriptActivities()
                    .AddActivitiesFrom<Startup>()
                    .AddFeatures(new[] { typeof(Startup) }, Configuration)
                );

            services
                .AddElsaSwagger()
                .AddElsaApiEndpoints();

            // Allow arbitrary client browser apps to access the API.
            // In a production environment, make sure to allow only origins you trust.
            services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().WithExposedHeaders("Content-Disposition")));
        }
        
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Elsa"));
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }
            
            app.UseCors();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();
            app.UseHttpActivities();
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapFallbackToPage("/ElsaDashboard");
            });
        }
    }
}