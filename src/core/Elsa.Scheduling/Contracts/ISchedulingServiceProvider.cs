using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scheduling.Contracts;

public interface ISchedulingServiceProvider
{
    void ConfigureServices(IServiceCollection services);
}