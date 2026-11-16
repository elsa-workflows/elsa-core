using Elsa.Common.Multitenancy;
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
    IOptions<StructuredLogsOptions> options,
    ITenantAccessor? tenantAccessor) : ILoggerProvider, ISupportExternalScope
{
    public StructuredLogLoggerProvider(
        IStructuredLogProvider logProvider,
        IStructuredLogRedactor redactor,
        IStructuredLogSourceRegistry sourceRegistry,
        IOptions<StructuredLogsOptions> options) : this(logProvider, redactor, sourceRegistry, options, null)
    {
    }

    private IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();

    public ILogger CreateLogger(string categoryName)
    {
        return new StructuredLogLogger(categoryName, logProvider, redactor, sourceRegistry, options.Value, () => _scopeProvider, tenantAccessor);
    }
    
    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    public void Dispose()
    {
    }
}
