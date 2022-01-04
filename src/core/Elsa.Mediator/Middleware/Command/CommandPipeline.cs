using Elsa.Mediator.Middleware.Command.Contracts;

namespace Elsa.Mediator.Middleware.Command;

public class CommandPipeline : ICommandPipeline
{
    private readonly IServiceProvider _serviceProvider;
    private CommandMiddlewareDelegate? _pipeline;

    public CommandPipeline(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;
    public CommandMiddlewareDelegate Pipeline => _pipeline ??= CreateDefaultPipeline();

    public CommandMiddlewareDelegate Setup(Action<ICommandPipelineBuilder>? setup = default)
    {
        var builder = new CommandPipelineBuilder(_serviceProvider);
        setup?.Invoke(builder);
        _pipeline = builder.Build();
        return _pipeline;
    }

    public async Task ExecuteAsync(CommandContext context) => await Pipeline(context);

    private CommandMiddlewareDelegate CreateDefaultPipeline() => Setup(x => x.UseCommandHandlers());
}