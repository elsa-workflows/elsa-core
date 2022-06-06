using System.Text.Json.Serialization;

namespace Elsa.Workflows.Core.Models;

/// <summary>
/// Represents a workflow-specific memory drive definition that is configured with a specific storage driver.
/// </summary>
public class DataDriveDefinition
{
    [JsonConstructor]
    public DataDriveDefinition()
    {
    }

    public DataDriveDefinition(string id, string driverId, string displayName)
    {
        Id = id;
        DriverId = driverId;
        DisplayName = displayName;
    }
    
    public string Id { get; set; } = default!;
    public string DriverId { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
}