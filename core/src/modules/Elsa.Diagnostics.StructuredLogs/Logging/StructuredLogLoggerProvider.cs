using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.StructuredLogs.Logging;

[ProviderAlias("ElsaStructuredLogs")]
public class StructuredLogLoggerProvider(
    IStructuredLogProvider logProvider,
    IStructuredLogRedactor redactor,
    IStructuredLogSourceRegistry sourceRegistry,
    IOptions<StructuredLogsOptions> options) : ILoggerProvider, ISupportExternalScope
{
    private IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();

    public ILogger CreateLogger(string categoryName)
    {
        return new StructuredLogLogger(categoryName, logProvider, redactor, sourceRegistry, options.Value, () => _scopeProvider);
    }
    
    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    public void Dispose()
    {
    }
}
