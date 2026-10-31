using Elsa.Mediator.Middleware.Command.Contracts;
using Elsa.Mediator.Middleware.Command.Components;

namespace Elsa.Mediator.Middleware.Command;

/// <summary>
/// Provides extension methods for <see cref="ICommandPipelineBuilder"/>.
/// </summary>
public static class CommandPipelineBuilderExtensions
{
    /// <summary>
    /// Adds the <see cref="CommandHandlerInvokerMiddleware"/> to the pipeline.
    /// </summary>
    /// <param name="builder">The pipeline builder.</param>
    /// <returns>The pipeline builder.</returns>
    public static ICommandPipelineBuilder UseCommandInvoker(this ICommandPipelineBuilder builder) => builder.UseMiddleware<CommandHandlerInvokerMiddleware>();
    
    /// <summary>
    /// Adds the <see cref="CommandLoggingMiddleware"/> to the pipeline.
    /// </summary>
    /// <param name="builder">The pipeline builder.</param>
    /// <returns>The pipeline builder.</returns>
    public static ICommandPipelineBuilder UseCommandLogging(this ICommandPipelineBuilder builder) => builder.UseMiddleware<CommandLoggingMiddleware>();
}