using Elsa.Mediator.Middleware.Command.Contracts;

namespace Elsa.Mediator.Middleware.Command;

/// <inheritdoc />
public class CommandPipeline : ICommandPipeline
{
    private readonly IServiceProvider _serviceProvider;
    private CommandMiddlewareDelegate? _pipeline;

    /// <summary>
    /// Constructor.
    /// </summary>
    public CommandPipeline(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;
    
    /// <inheritdoc />
    public CommandMiddlewareDelegate Pipeline => _pipeline ??= CreateDefaultPipeline();

    /// <inheritdoc />
    public CommandMiddlewareDelegate Setup(Action<ICommandPipelineBuilder>? setup = default)
    {
        var builder = new CommandPipelineBuilder(_serviceProvider);
        setup?.Invoke(builder);
        _pipeline = builder.Build();
        return _pipeline;
    }

    /// <inheritdoc />
    public async Task InvokeAsync(CommandContext context) => await Pipeline(context);

    private CommandMiddlewareDelegate CreateDefaultPipeline() => Setup(x => x.UseCommandInvoker().UseCommandLogging());
}