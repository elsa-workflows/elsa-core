using Autofac;
using Elsa.Multitenancy;
using Elsa.Samples.Server.Host;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
            // tODO: this should be configured differently,
            // however there is a lack of documentation on pre ASP.NET Core 3.0 configuration
            builder
                .UseAutofacMultitenantRequestServices()
                .ConfigureServices(services => services.AddSingleton<IServiceProviderFactory<ContainerBuilder>>(new AutofacMultitenantServiceProviderFactory(container => MultitenantContainerFactory.CreateSampleMultitenantContainer(container))))
                .ConfigureAppConfiguration(config => config.AddInMemoryCollection(FakeConfiguration.Create(_dbConnectionString)));
        }
    }
}