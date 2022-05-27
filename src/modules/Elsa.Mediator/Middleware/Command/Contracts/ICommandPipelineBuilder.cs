namespace Elsa.Mediator.Middleware.Command.Contracts;

public interface ICommandPipelineBuilder
{
    public IDictionary<string, object?> Properties { get; }
    IServiceProvider ApplicationServices { get; }
    ICommandPipelineBuilder Use(Func<CommandMiddlewareDelegate, CommandMiddlewareDelegate> middleware);
    public CommandMiddlewareDelegate Build();
}