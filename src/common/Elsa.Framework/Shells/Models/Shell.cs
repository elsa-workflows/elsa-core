using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Framework.Shells;

public class Shell(ShellBlueprint blueprint, IServiceCollection serviceCollection, IServiceProvider serviceProvider)
{
    public ShellBlueprint Blueprint { get; } = blueprint;
    public IServiceCollection ServiceCollection { get; } = serviceCollection;
    public IServiceProvider ServiceProvider { get; } = serviceProvider;
}