using Elsa;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Persistence.EntityFramework.Sqlite;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ElsaDashboard.Samples.AspNetCore.Monolith
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
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
                        .AddWorkflowsFrom<Startup>()
                );

            services
                .AddElsaSwagger()
                .AddElsaApiEndpoints();
            
            // Allow arbitrary client browser apps to access the API.
            // In a production environment, make sure to allow only origins you trust.
            services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().WithExposedHeaders("Content-Disposition")));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
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
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseCors();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();
            app.UseHttpActivities();
            app.UseEndpoints(endpoints =>
            {
                // Elsa Server uses ASP.NET Core Controllers.
                endpoints.MapControllers();
                
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
