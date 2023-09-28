using System.Collections.ObjectModel;
using Elsa.Alterations.Core.Contexts;
using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Models;
using Elsa.Common.Contracts;
using Microsoft.Extensions.Logging;

namespace Elsa.Alterations.Core.Services;

public class DefaultAlterationLog : IAlterationLog
{
    private readonly ISystemClock _systemClock;
    private ICollection<AlterationLogEntry> _logEntries = new Collection<AlterationLogEntry>();

    public DefaultAlterationLog(ISystemClock systemClock)
    {
        _systemClock = systemClock;
    }
    
    public void Log(string batchId, string alterationId, string message, LogLevel logLevel = LogLevel.Information)
    {
        throw new NotImplementedException();
    }
}