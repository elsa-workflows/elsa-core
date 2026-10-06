using System.Reflection;
using System.Security.Cryptography;

// Fixture metadata only. Authentication and all product endpoints stay normal.
static class RuntimeEvidence
{
    public static object LoadedAssemblies() => AppDomain.CurrentDomain.GetAssemblies()
        .Where(assembly => assembly.GetName().Name?.StartsWith("Elsa", StringComparison.Ordinal) == true)
        .OrderBy(assembly => assembly.FullName, StringComparer.Ordinal)
        .Select(assembly => new
        {
            name = assembly.GetName().Name, fullName = assembly.FullName,
            version = assembly.GetName().Version?.ToString(),
            informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            location = assembly.Location,
            sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assembly.Location))).ToLowerInvariant()
        });
}
