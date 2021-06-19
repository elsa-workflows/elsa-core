using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Elsa.Services.Startup
{
    public abstract class StartupBase : IStartup
    {
        public virtual void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration)
        {
        }

        public virtual void ConfigureApp(IApplicationBuilder app)
        {
        }
    }
}