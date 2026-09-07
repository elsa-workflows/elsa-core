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
    /// <remarks>
    /// Rows are matched after trimming, and every data row must yield at least one replacement. Requiring a
    /// row to start with an unindented <c>| `</c> looked equivalent and was not: a formatting-only change that
    /// indented the table, or a left column written without backticks, would drop rows silently while the
    /// totals still looked plausible. A skipped row is an unchecked permission, which is the one outcome this
    /// test exists to prevent, so it is made loud rather than merely unlikely.
    /// </remarks>
    private static IReadOnlyCollection<string> ReadReplacements()
    {
        var guide = FindGuide();
        var lines = File.ReadAllLines(guide);
        var start = Array.FindIndex(lines, x => x.Trim().StartsWith("## Full mapping", StringComparison.Ordinal));
        Assert.True(start >= 0, $"No '## Full mapping' section in {guide}. If the section was renamed, this test is looking in the wrong place rather than passing vacuously.");

        var rows = lines.Skip(start + 1)
            .TakeWhile(x => !x.Trim().StartsWith("## ", StringComparison.Ordinal))
            .Select(x => x.Trim())
            .Where(x => x.StartsWith('|'))
            .Select(x => x.Split('|'))
            .Where(x => x.Length >= 3)
            // The header names the columns and the next row is the --- separator; neither maps a permission.
            .Where(x => !x[1].Trim().Trim('-', ':', ' ').Equals(string.Empty, StringComparison.Ordinal))
            .Where(x => x[1].Contains('`'))
            .ToArray();

        var replacements = new List<string>();
        var emptyRows = new List<string>();

        foreach (var row in rows)
        {
            var matches = Regex.Matches(row[2], "`([^`]+)`").Select(x => x.Groups[1].Value).ToArray();

            if (matches.Length > 0)
                replacements.AddRange(matches);
            // A permission that was dropped rather than translated has no replacement to check. Those rows say
            // so in words, so they are recognised rather than treated as a parse failure -- but only those.
            else if (!row[2].Contains("removed", StringComparison.OrdinalIgnoreCase))
                emptyRows.Add(row[1].Trim());
        }

        Assert.True(emptyRows.Count == 0, $"{emptyRows.Count} mapping row(s) yielded no replacement and do not say the permission was removed, so what they document is going unchecked: {string.Join(", ", emptyRows)}.");

        // A parser that silently matches nothing passes forever.
        Assert.True(rows.Length > 20, $"Only {rows.Length} mapping row(s) parsed out of the table; the format has changed and this test is no longer reading it.");
        return replacements.Distinct(StringComparer.Ordinal).ToArray();
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
