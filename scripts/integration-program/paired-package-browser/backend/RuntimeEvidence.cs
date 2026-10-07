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
            sha256 = AssemblySha256(assembly)
        });

    public static LoadedElsaAssemblyIdentity? GetLoadedElsaAssemblyIdentity(Assembly assembly)
    {
        if (!AppDomain.CurrentDomain.GetAssemblies().Any(candidate => ReferenceEquals(candidate, assembly)) ||
            assembly.GetName().Name?.StartsWith("Elsa", StringComparison.Ordinal) != true ||
            string.IsNullOrEmpty(assembly.FullName))
            return null;

        return new LoadedElsaAssemblyIdentity(assembly.GetName().Name!, assembly.FullName, AssemblySha256(assembly));
    }

    private static string AssemblySha256(Assembly assembly) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assembly.Location))).ToLowerInvariant();
}

internal sealed record LoadedElsaAssemblyIdentity(string Name, string FullName, string Sha256);
