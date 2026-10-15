using Elsa.Mediator.Middleware.Command.Contracts;

namespace Elsa.Mediator.Middleware.Command;

/// <inheritdoc />
public class CommandPipeline : ICommandPipeline
{
    private readonly CommandPipelineBuilder _builder;
    private CommandMiddlewareDelegate _pipeline = null!;

    /// <summary>
    /// Constructor.
    /// </summary>
    public CommandPipeline(IServiceProvider serviceProvider)
    {
        _builder = new(serviceProvider);
        Setup(x => x.UseCommandInvoker().UseCommandLogging());
    }

    /// <inheritdoc />
    public CommandMiddlewareDelegate Pipeline => _pipeline;

    /// <inheritdoc />
    public CommandMiddlewareDelegate Setup(Action<ICommandPipelineBuilder>? setup = null)
    {
        setup?.Invoke(_builder);
        _pipeline = _builder.Build();
        return _pipeline;
    }

    /// <inheritdoc />
    public async Task InvokeAsync(CommandContext context) => await Pipeline(context);
}