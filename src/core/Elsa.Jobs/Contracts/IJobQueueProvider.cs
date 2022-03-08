using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Jobs.Contracts;

public interface IJobQueueProvider
{
    void ConfigureServices(IServiceCollection services);
}