using Elsa.Agents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace Elsa.Agents.Core.UnitTests.Services;

public class AgentFactoryTests
{
    [Fact]
    public void CreateAgent_UsesHostLoggerFactory()
    {
        var hostLoggerFactory = LoggerFactory.Create(_ => { });
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(hostLoggerFactory);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new AgentFactory(
            new EmptySkillDiscoverer(),
            serviceProvider,
            OptionsFactory.Create(new AgentsOptions()),
            NullLogger<AgentFactory>.Instance);

        var agent = factory.CreateAgent(new AgentConfig
        {
            Name = "TestAgent",
            Description = "Test",
            PromptTemplate = "You are a test agent."
        });

        Assert.Same(hostLoggerFactory, agent.Kernel.Services.GetService<ILoggerFactory>());
    }

    [Fact]
    public void CreateAgent_ResolvesGenericLoggerFromHostFactory()
    {
        var hostLoggerFactory = LoggerFactory.Create(_ => { });
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(hostLoggerFactory);
        var serviceProvider = services.BuildServiceProvider();
        var factory = new AgentFactory(
            new EmptySkillDiscoverer(),
            serviceProvider,
            OptionsFactory.Create(new AgentsOptions()),
            NullLogger<AgentFactory>.Instance);

        var agent = factory.CreateAgent(new AgentConfig
        {
            Name = "TestAgent",
            Description = "Test",
            PromptTemplate = "You are a test agent."
        });

        var logger = agent.Kernel.Services.GetRequiredService<ILogger<SomeType>>();
        var loggerFactories = agent.Kernel.Services.GetServices<ILoggerFactory>().ToList();

        Assert.NotNull(logger);
        Assert.Single(loggerFactories);
        Assert.Same(hostLoggerFactory, loggerFactories[0]);
    }

    private sealed class SomeType;

    private sealed class EmptySkillDiscoverer : ISkillDiscoverer
    {
        public IEnumerable<SkillDescriptor> DiscoverSkills() => [];
    }
}
