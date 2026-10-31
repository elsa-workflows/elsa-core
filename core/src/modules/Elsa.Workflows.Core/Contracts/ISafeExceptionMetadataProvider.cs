namespace Elsa.Workflows;

/// <summary>
/// Provides an explicitly allow-listed set of privacy-safe exception metadata.
/// </summary>
public interface ISafeExceptionMetadataProvider
{
    IReadOnlyDictionary<string, string> GetSafeMetadata();
}
