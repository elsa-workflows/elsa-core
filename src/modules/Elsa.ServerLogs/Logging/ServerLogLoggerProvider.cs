using Elsa.ServerLogs.Contracts;
using Elsa.ServerLogs.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.ServerLogs.Logging;

[ProviderAlias("ElsaServerLogStreaming")]
public class ServerLogLoggerProvider(
    IServerLogProvider logProvider,
    IServerLogRedactor redactor,
    IServerLogSourceRegistry sourceRegistry,
    IOptions<ServerLogStreamingOptions> options) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new ServerLogLogger(categoryName, logProvider, redactor, sourceRegistry, options.Value);
    }
    
    public void Dispose()
    {
    }
}
