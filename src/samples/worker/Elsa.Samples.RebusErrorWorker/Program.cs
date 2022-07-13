using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Samples.RebusErrorWorker;

/// <summary>
/// When this program starts, it will continuously process messages from the error queue and send them to the return address for re-processing.
/// </summary>
public class Program
{
    public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();
    private static IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args).ConfigureServices((_, services) => { services.AddHostedService<ProcessErrorQueue>(); });
}