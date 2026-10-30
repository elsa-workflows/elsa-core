namespace Elsa.Persistence.VNext.Extensions;

public class PersistenceVNextOptions
{
    public string SchemaName { get; set; } = "Elsa";
    public int SchemaVersion { get; set; } = 1;
    public bool MaterializeOnStartup { get; set; } = true;
}
