using JetBrains.Annotations;

namespace Elsa.Server.Web.Middleware;

/// <summary>
/// Simulates latency by delaying the request for a random amount of time between the specified minimum and maximum durations.
/// </summary>
/// <param name="next"></param>
/// <param name="min"></param>
/// <param name="max"></param>
[UsedImplicitly]
public class SimulatedLatencyMiddleware(
    RequestDelegate next,
    TimeSpan min,
    TimeSpan max)
{
    private readonly int _minDelayInMs = (int)min.TotalMilliseconds;
    private readonly int _maxDelayInMs = (int)max.TotalMilliseconds;
    private readonly ThreadLocal<Random> _random = new(() => new Random());

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context"></param>
    [UsedImplicitly]
    public async Task Invoke(HttpContext context)
    {
        int delayInMs = _random.Value!.Next(
            _minDelayInMs,
            _maxDelayInMs
        );

        await Task.Delay(delayInMs);
        await next(context);
    }
}

/// <summary>
/// Provides extension methods for the <see cref="IApplicationBuilder"/> interface.
/// </summary>
public static class AppBuilderExtensions
{
    /// <summary>
    /// Adds the <see cref="SimulatedLatencyMiddleware"/> to the application's request pipeline.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="min"></param>
    /// <param name="max"></param>
    public static IApplicationBuilder UseSimulatedLatency(
        this IApplicationBuilder app,
        TimeSpan min,
        TimeSpan max)
    {
        return app.UseMiddleware<SimulatedLatencyMiddleware>(min, max);
    }
}