using Microsoft.Extensions.Logging;

namespace Elsa.Api.Client.Resources.Alterations.Models;

/// <summary>
/// An individual log entry about an alteration
/// </summary>
/// <param name="Message"></param>
/// <param name="LogLevel"></param>
/// <param name="Timestamp"></param>
/// <param name="EventName"></param>
public record AlterationLogEntry(string Message, LogLevel LogLevel, DateTimeOffset Timestamp, string? EventName = null);