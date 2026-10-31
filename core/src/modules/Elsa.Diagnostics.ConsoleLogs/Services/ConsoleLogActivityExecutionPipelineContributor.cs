namespace Elsa.Diagnostics.ConsoleLogs.Services;

/// <summary>
/// Installs console log metadata capture into activity execution.
/// </summary>
public sealed class ConsoleLogActivityExecutionPipelineContributor : IActivityExecutionPipelineContributor
{
    public void Configure(IActivityExecutionPipelineBuilder builder)
    {
        builder.UseMiddleware<ConsoleLogActivityExecutionMiddleware>();
    }
}
