using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Jobs.Services;

public interface IJobSchedulerProvider
{
    void ConfigureServices(IServiceCollection services);
}