using Microsoft.Extensions.Logging;

namespace Elsa.Logging.Core.UnitTests.Helpers;

class TestLoggerFactory(ILogger logger) : ILoggerFactory
{
    public ILogger CreateLogger(string categoryName) => logger;
    public void AddProvider(ILoggerProvider provider) { }
    public void Dispose() { }
}