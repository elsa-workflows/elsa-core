using System.Reflection;
using System.Text.RegularExpressions;
using Elsa.Authorization;
using Elsa.Permissions;
using Elsa.ModularServer.Web;
using Elsa.Server.Web;

namespace Elsa.Hosts.SmokeTests;

/// <summary>
/// Checks the upgrade guide's mapping table against the catalog a host actually builds.
/// </summary>
/// <remarks>
/// The table tells operators how to rewrite every stored permission, and nothing had ever checked that what
/// it tells them to write is a permission Elsa accepts. That is not hypothetical: the cutover left several
/// checks comparing against legacy constants which the guide itself instructs you to replace, so following
/// it silently disabled them. A table entry that does not parse, or that names a resource or verb no module
/// advertises, is a deployment locked out of an endpoint by doing exactly as it was told.
/// <para>
/// Reading the published document rather than a copy is the point: this fails when the guide drifts from the
/// code, which is the direction the drift actually goes.
/// </para>
/// </remarks>
public class MigrationGuideTests
{
    [Fact]
    public void EveryReplacementInTheMappingTableIsAWellFormedPermission()
    {
        var malformed = ReadReplacements()
            .Where(x => !Permission.TryParse(x, out _))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.True(malformed.Length == 0, $"The guide tells operators to write {malformed.Length} value(s) that are not well-formed permissions, so nobody can hold them: {string.Join(", ", malformed)}.");
    }

    [Fact]
    public void EveryReplacementInTheMappingTableIsAdvertisedByTheCatalog()
    {
        var catalog = BuildCatalog();

        // A catalog that came up empty would pass every check below without testing anything.
        Assert.True(catalog.Count > 20, $"Only {catalog.Count} resource(s) were discovered; the catalog is not being built and this test would pass vacuously.");

        var unadvertised = ReadReplacements()
            .Where(x => Permission.TryParse(x, out var permission) && !permission.HasWildcard && !IsAdvertised(catalog, permission))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.True(unadvertised.Length == 0, $"{unadvertised.Length} replacement(s) name a resource or verb no module advertises, so a role rewritten as the guide says would not authorize anything: {string.Join(", ", unadvertised)}.");
    }

    private static bool IsAdvertised(IReadOnlyDictionary<string, IReadOnlyCollection<string>> catalog, Permission permission) =>
        catalog.TryGetValue(permission.Resource, out var verbs) && verbs.Contains(permission.Verb, StringComparer.Ordinal);

    /// <summary>The resources and verbs every Elsa module shipped alongside these tests contributes.</summary>
    /// <remarks>
    /// Built by loading every <c>Elsa.*.dll</c> in the output directory, which is deterministic and complete.
    /// Two lazier approaches were tried and both under-report. <c>AppDomain.CurrentDomain.GetAssemblies()</c>
    /// describes whatever earlier tests happened to touch, so this class saw 29 resources as missing when run
    /// alone and none in a full run. Walking <c>GetReferencedAssemblies()</c> from the two hosts is no better:
    /// the compiler drops references to assemblies whose types the app never names, so AI, OpenTelemetry and
    /// Shells vanished despite being project references. The output directory has every module either host
    /// pulls in, regardless of whether its types are mentioned.
    /// </remarks>
    private static Dictionary<string, IReadOnlyCollection<string>> BuildCatalog() =>
        Directory.GetFiles(AppContext.BaseDirectory, "Elsa.*.dll")
            .Select(TryLoad)
            .Where(x => x is not null)
            .SelectMany(x => SafeGetTypes(x!))
            .Where(x => x is { IsClass: true, IsAbstract: false } && typeof(IPermissionDescriptorProvider).IsAssignableFrom(x) && x.GetConstructor(Type.EmptyTypes) is not null)
            .Select(x => (IPermissionDescriptorProvider)Activator.CreateInstance(x)!)
            .SelectMany(x => x.GetDescriptors())
            .GroupBy(x => x.Resource, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => (IReadOnlyCollection<string>)x.SelectMany(d => d.SupportedVerbs).Distinct(StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);

    private static Assembly? TryLoad(string path)
    {
        try
        {
            return Assembly.LoadFrom(path);
        }
        catch (Exception ex) when (ex is BadImageFormatException or FileLoadException or FileNotFoundException)
        {
            // A native or otherwise unloadable file in the output directory contributes nothing.
            return null;
        }
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(x => x is not null)!;
        }
    }

    /// <summary>Every permission the mapping table's right-hand column tells an operator to write.</summary>
    private static IReadOnlyCollection<string> ReadReplacements()
    {
        var guide = FindGuide();
        var lines = File.ReadAllLines(guide);
        var start = Array.FindIndex(lines, x => x.StartsWith("## Full mapping", StringComparison.Ordinal));
        Assert.True(start >= 0, $"No '## Full mapping' section in {guide}. If the section was renamed, this test is looking in the wrong place rather than passing vacuously.");

        var replacements = lines.Skip(start)
            .TakeWhile(x => !x.StartsWith("## ", StringComparison.Ordinal) || x.StartsWith("## Full mapping", StringComparison.Ordinal))
            .Where(x => x.StartsWith("| `", StringComparison.Ordinal))
            .Select(x => x.Split('|'))
            .Where(x => x.Length >= 3)
            .SelectMany(x => Regex.Matches(x[2], "`([^`]+)`").Select(m => m.Groups[1].Value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        // A parser that silently matches nothing passes forever.
        Assert.True(replacements.Length > 20, $"Only {replacements.Length} replacement(s) parsed out of the mapping table; the table format has changed and this test is no longer reading it.");
        return replacements;
    }

    private static string FindGuide()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "doc", "migrations", "authorization-model.md");

            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException("Could not locate doc/migrations/authorization-model.md from the test output directory.");
    }
}
