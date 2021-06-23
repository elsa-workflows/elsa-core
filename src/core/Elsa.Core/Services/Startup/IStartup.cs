using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Elsa.Services.Startup
{
    public interface IStartup
    {
        void ConfigureElsa(ElsaOptionsBuilder elsa, IConfiguration configuration);
        void ConfigureApp(IApplicationBuilder app);
    }
}