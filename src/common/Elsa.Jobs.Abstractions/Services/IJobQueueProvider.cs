using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Jobs.Services;

public interface IJobQueueProvider
{
    void ConfigureServices(IServiceCollection services);
}