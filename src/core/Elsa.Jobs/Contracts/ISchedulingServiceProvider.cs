using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Jobs.Contracts;

public interface ISchedulingServiceProvider
{
    void ConfigureServices(IServiceCollection services);
}