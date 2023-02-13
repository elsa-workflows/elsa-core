namespace Elsa.Workflows.Core.Models;

public class Port
{
    public string Name { get; set; } = default!;
    public string? DisplayName { get; set; }
    public PortMode Mode { get; set; }
    public bool IsBrowsable { get; set; } = true;
}