using Elsa.Identity.Entities;

namespace Elsa.Identity.Options;

/// <summary>
/// Represents options that stores available applications.
/// </summary>
public class ApplicationsOptions
{
    /// <summary>
    /// Gets or sets the applications.
    /// </summary>
    public ICollection<Application> Applications { get; set; } = new List<Application>();
}