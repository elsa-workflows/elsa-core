// ReSharper disable once CheckNamespace
using Elsa.Connections.Middleware;
using Microsoft.Extensions.Logging;

namespace Elsa.Connections.Core.Extensions;

/// <summary>
/// 
/// </summary>
public static partial class LogConnectionExtensions
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Connection {name} was not found"
        )]
    public static partial void LogConnectionNotFound(ILogger logger, string name);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Connection is null"
        )]
    public static partial void LogConnectionIsNull(ILogger logger);
    
}