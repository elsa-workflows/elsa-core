using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Jobs.Contracts;

public interface IJobSchedulerProvider
{
    void ConfigureServices(IServiceCollection services);
}