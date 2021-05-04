using Elsa.Samples.Server.Host;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Elsa.ComponentTests.Helpers
{
    public class ElsaHostApplicationFactory : WebApplicationFactory<Startup>
    {
        private string _dbConnectionString = default!;

        public void SetDbConnectionString(string connectionString)
        {
            _dbConnectionString = connectionString;
        }
        
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder
                .ConfigureAppConfiguration(config => config.AddInMemoryCollection(FakeConfiguration.Create(_dbConnectionString)));
        }
    }
}