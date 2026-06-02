namespace Elsa.ModularPersistence.Options;

/// <summary>
/// Configures provider-neutral modular persistence startup behavior.
/// </summary>
public sealed class ModularPersistenceOptions
{
    public bool MaterializeOnStartup { get; set; } = true;
}
