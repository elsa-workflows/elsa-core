namespace Elsa.Workflows;

/// <summary>
/// Identifies the stage at which output conversion failed.
/// </summary>
public enum OutputConversionFailureStage
{
    Resolution,
    SettingsValidation,
    SourceCompatibility,
    Invocation,
    ResultValidation
}
