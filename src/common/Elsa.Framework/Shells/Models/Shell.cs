using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Framework.Shells;

public class Shell(IServiceCollection serviceCollection, IServiceProvider serviceProvider)
{
    public IServiceCollection ServiceCollection { get; init; } = serviceCollection;
    public IServiceProvider ServiceProvider { get; init; } = serviceProvider;
}