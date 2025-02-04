namespace Elsa.Api.Client.Resources.Alterations.Models;

/// <summary>
/// Represents a log of alterations.
/// </summary>
public class AlterationLog 
{

    /// <summary>
    /// The log entries.
    /// </summary>
    public ICollection<AlterationLogEntry> LogEntries { get; set; } = new List<AlterationLogEntry>();
}