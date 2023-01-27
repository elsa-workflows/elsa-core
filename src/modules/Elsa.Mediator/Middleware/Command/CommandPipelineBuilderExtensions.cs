using Elsa.Mediator.Middleware.Command.Contracts;
using Elsa.Mediator.Middleware.Command.Components;

namespace Elsa.Mediator.Middleware.Command;

public static class CommandPipelineBuilderExtensions
{
    public static ICommandPipelineBuilder UseCommandInvoker(this ICommandPipelineBuilder builder) => builder.UseMiddleware<CommandHandlerInvokerMiddleware>();
    public static ICommandPipelineBuilder UseCommandLogging(this ICommandPipelineBuilder builder) => builder.UseMiddleware<CommandLoggingMiddleware>();
}