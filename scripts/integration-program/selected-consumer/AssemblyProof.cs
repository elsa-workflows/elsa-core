using System.Reflection;
using System.Security.Cryptography;

internal static class SelectedAssemblyProof
{
    public static object[] Snapshot() => AppDomain.CurrentDomain.GetAssemblies()
        .Where(assembly => assembly.GetName().Name?.StartsWith("Elsa", StringComparison.Ordinal) == true)
        .OrderBy(assembly => assembly.FullName, StringComparer.Ordinal)
        .Select(assembly => (object)new
        {
            name = assembly.GetName().Name,
            fullName = assembly.FullName,
            version = assembly.GetName().Version?.ToString(),
            informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
            location = assembly.Location,
            sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assembly.Location))).ToLowerInvariant()
        }).ToArray();
}
