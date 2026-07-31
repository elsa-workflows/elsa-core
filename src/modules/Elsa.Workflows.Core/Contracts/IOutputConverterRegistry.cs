using Elsa.Workflows.Models;

namespace Elsa.Workflows;

/// <summary>
/// Provides registered output converter descriptors and registrations.
/// </summary>
public interface IOutputConverterRegistry
{
    /// <summary>Lists every registered descriptor.</summary>
    IEnumerable<OutputConverterDescriptor> ListAll();

    /// <summary>Finds a descriptor by its ordinal, case-sensitive ID.</summary>
    OutputConverterDescriptor? Find(string id);

    /// <summary>Finds the keyed service registration for a converter ID.</summary>
    OutputConverterRegistration? FindRegistration(string id);

    /// <summary>Lists converters whose declared types are compatible with the supplied binding types.</summary>
    IEnumerable<OutputConverterDescriptor> FindCompatible(Type sourceType, Type destinationType);
}
