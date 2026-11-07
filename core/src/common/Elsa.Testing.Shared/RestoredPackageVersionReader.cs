using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text.Json;

namespace Elsa.Testing.Shared;

/// <summary>
/// Reads restored NuGet package versions from the nearest <c>project.assets.json</c>.
/// </summary>
public static class RestoredPackageVersionReader
{
    /// <summary>
    /// Returns every restored version of <paramref name="packageId"/> in the calling assembly's target graph.
    /// The top-level <c>libraries</c> bag is a union across TFMs, so this reads <c>targets</c> for the caller TFM.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IReadOnlyList<Version> Read(string packageId, string? startDirectory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);

        using var document = JsonDocument.Parse(File.ReadAllText(FindAssetsPath(startDirectory ?? AppContext.BaseDirectory)));
        var prefix = packageId + "/";
        var versions = new List<Version>();
        var moniker = GetTargetMoniker(Assembly.GetCallingAssembly());
        var matched = false;

        foreach (var target in document.RootElement.GetProperty("targets").EnumerateObject())
        {
            if (!string.Equals(target.Name.Split('/')[0], moniker, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            matched = true;
            foreach (var library in target.Value.EnumerateObject())
            {
                if (!library.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                versions.Add(Version.Parse(library.Name[prefix.Length..].Split('-', '+')[0]));
            }
        }

        if (!matched)
        {
            throw new InvalidOperationException($"No project.assets.json target matched {moniker}.");
        }

        return versions;
    }

    private static string GetTargetMoniker(Assembly assembly)
    {
        var frameworkName = assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName
                            ?? throw new InvalidOperationException($"{assembly.GetName().Name} has no TargetFrameworkAttribute.");
        const string prefix = ".NETCoreApp,Version=v";
        if (!frameworkName.StartsWith(prefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unsupported target framework '{frameworkName}'.");
        }

        return "net" + frameworkName[prefix.Length..];
    }

    private static string FindAssetsPath(string startDirectory)
    {
        for (var directory = new DirectoryInfo(startDirectory); directory is not null; directory = directory.Parent)
        {
            var assets = Path.Combine(directory.FullName, "obj", "project.assets.json");
            if (File.Exists(assets))
            {
                return assets;
            }
        }

        throw new FileNotFoundException("project.assets.json", startDirectory);
    }
}
