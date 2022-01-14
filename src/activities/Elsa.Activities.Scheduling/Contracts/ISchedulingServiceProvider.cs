using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Scheduling.Contracts;

public interface ISchedulingServiceProvider
{
    void ConfigureServices(IServiceCollection services);
}