using Microsoft.Extensions.Logging;

namespace Elsa.Alterations.Core.Contracts;

public interface IAlterationLog
{
    void Log(string batchId, string alterationId, string message, LogLevel logLevel = LogLevel.Information);
}