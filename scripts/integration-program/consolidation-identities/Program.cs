using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;

var arguments = Arguments.Parse(args);
var sdkToolsPath = Assembly.GetExecutingAssembly()
    .GetCustomAttributes<AssemblyMetadataAttribute>()
    .Single(attribute => attribute.Key == "MSBuildToolsPath")
    .Value ?? throw new InvalidOperationException("The evaluator did not record its MSBuild SDK path.");

var extensionsRoot = ResolvePhysicalPath(arguments.ExtensionsRoot);
var studioRoot = ResolvePhysicalPath(arguments.StudioRoot);
var consolidatedRoot = ResolvePhysicalPath(arguments.ConsolidatedRoot);
var importReceiptPath = ResolveInside(consolidatedRoot, "import-receipt.json");
var buildReceiptPath = ResolveInside(consolidatedRoot, "consolidated-build-receipt.json");
var inventoryPath = ResolveInside(consolidatedRoot, "doc/integration-program/inventory/inventory.json");
RequireSamePath(arguments.ImportReceipt, importReceiptPath, "import receipt");
RequireSamePath(arguments.BuildReceipt, buildReceiptPath, "build receipt");
RequireSamePath(arguments.Inventory, inventoryPath, "inventory");
var outputPath = ValidateOutputPath(arguments.Output, [extensionsRoot, studioRoot, consolidatedRoot], [importReceiptPath, buildReceiptPath, inventoryPath]);
ConfigureMsBuildEnvironment(sdkToolsPath);

var sourceReceipt = ReadJson<ImportReceipt>(importReceiptPath);
var buildReceipt = ReadJson<BuildReceipt>(buildReceiptPath);
using var inventory = ReadJson<JsonDocument>(inventoryPath);
var diagnostics = new EvaluationDiagnostics();
var sourceRoots = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["extensions"] = extensionsRoot,
    ["studio"] = studioRoot
};

var actualSourcePins = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["extensions"] = Git(extensionsRoot, "rev-parse", "HEAD"),
    ["studio"] = Git(studioRoot, "rev-parse", "HEAD")
};
var actualConsolidatedHead = Git(consolidatedRoot, "rev-parse", "HEAD");

RequirePin("prepared consolidated evaluation baseline", AuditConstants.ExpectedConsolidatedHead, actualConsolidatedHead);
RequirePin("extensions", sourceReceipt.SourceCommits["extensions"], actualSourcePins["extensions"]);
RequirePin("studio", sourceReceipt.SourceCommits["studio"], actualSourcePins["studio"]);
RequireAncestor(consolidatedRoot, sourceReceipt.RehearsalCommit, actualConsolidatedHead, "import rehearsal");
RequireAncestor(consolidatedRoot, buildReceipt.RehearsalCommit, actualConsolidatedHead, "build rehearsal");
RequireAncestor(consolidatedRoot, sourceReceipt.SourceCommits["core"], actualConsolidatedHead, "Core source pin");
RequirePin("import/build receipt Core source", sourceReceipt.SourceCommits["core"], buildReceipt.SourceCommits["core"]);
RequirePin("import/build receipt Extensions source", sourceReceipt.SourceCommits["extensions"], buildReceipt.SourceCommits["extensions"]);
RequirePin("import/build receipt Studio source", sourceReceipt.SourceCommits["studio"], buildReceipt.SourceCommits["studio"]);

var mappings = sourceReceipt.Mapping
    .Where(mapping => sourceRoots.ContainsKey(mapping.Repository) && mapping.Source.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
    .OrderBy(mapping => mapping.Repository, StringComparer.Ordinal)
    .ThenBy(mapping => mapping.Source, StringComparer.Ordinal)
    .ToArray();
var activeMappings = mappings
    .Where(mapping => mapping.Destination.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
    .ToArray();
var quarantinedMappings = mappings
    .Where(mapping => !mapping.Destination.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
    .ToArray();

Require(activeMappings.Length == buildReceipt.ImportedProjects,
    $"The import receipt maps {activeMappings.Length} active Extensions/Studio projects, but the build receipt records {buildReceipt.ImportedProjects}.");
Require(!buildReceipt.BuildCompatibilityVerified,
    "The retained build receipt unexpectedly claims build compatibility was verified; this audit must describe the exact recorded baseline.");

VerifyInventory(inventory.RootElement, mappings);
VerifySourceBlobs(sourceRoots, sourceReceipt.SourceCommits, mappings);
VerifyBuildReceiptFiles(consolidatedRoot, buildReceipt.Files);
VerifyCleanBuildInputs(sourceRoots.Append(new KeyValuePair<string, string>("consolidated", consolidatedRoot)));
VerifyProjectFilesAreAtRecordedHeads(sourceRoots, consolidatedRoot, activeMappings);

var projects = new List<ProjectComparison>(activeMappings.Length);
var evaluationFailures = new List<EvaluationFailure>();
var evaluatedInputs = new Dictionary<string, string>(AuditPaths.Comparer);
evaluatedInputs.Add(importReceiptPath, "audit-evidence");
evaluatedInputs.Add(buildReceiptPath, "audit-evidence");
evaluatedInputs.Add(inventoryPath, "audit-evidence");
foreach (var globalJson in FindGlobalJsonInputs(sourceRoots, consolidatedRoot, activeMappings))
{
    evaluatedInputs.TryAdd(globalJson, "global-json");
}
using var scratch = new EvaluationScratch();
using var evaluationCollection = new ProjectCollection();
evaluationCollection.RegisterLogger(diagnostics);
foreach (var mapping in activeMappings)
{
    var sourceRoot = sourceRoots[mapping.Repository];
    var originalPath = ResolveInside(sourceRoot, mapping.Source);
    var consolidatedPath = ResolveInside(consolidatedRoot, mapping.Destination);
    evaluatedInputs.TryAdd(originalPath, "project");
    evaluatedInputs.TryAdd(consolidatedPath, "project");
    try
    {
        var original = EvaluateProject(originalPath, mapping.Repository, mapping.Source, arguments.Configuration, evaluationCollection, scratch.RootPath, evaluatedInputs);
        var consolidated = EvaluateProject(consolidatedPath, mapping.Repository, mapping.Destination, arguments.Configuration, evaluationCollection, scratch.RootPath, evaluatedInputs);
        projects.Add(Compare(mapping, consolidatedPath, original, consolidated));
    }
    catch (Exception exception)
    {
        evaluationFailures.Add(new EvaluationFailure(mapping.Repository, mapping.Source, mapping.Destination, exception.ToString()));
    }
}

var importedInputs = VerifyAndHashImportedInputs(sourceRoots, consolidatedRoot, sdkToolsPath, evaluatedInputs);
var remainingScratchEntries = scratch.UnexpectedEntries;
if (remainingScratchEntries.Count > 0)
{
    evaluationFailures.Add(new EvaluationFailure("evaluation", "MSBuildProjectExtensionsPath", "MSBuildProjectExtensionsPath", $"MSBuild evaluation wrote unexpected entries beneath scratch root: {string.Join(", ", remainingScratchEntries)}"));
}
var propertyDifferences = projects.Sum(project => project.Differences.Count);
var projectDifferences = projects.Count(project => project.Differences.Count > 0);
var publicIdentityProperties = new HashSet<string>(["PackageId", "AssemblyName", "RootNamespace", "TargetFrameworks", "TargetFramework", "DeclaredTargetFramework"], StringComparer.Ordinal);
var releaseMetadataProperties = new HashSet<string>(["Version", "PackageVersion", "IsPackable"], StringComparer.Ordinal);
var publicIdentityDifferences = projects.SelectMany(project => project.Differences).Where(difference => publicIdentityProperties.Contains(difference.Property)).ToArray();
var releaseMetadataDifferences = projects.SelectMany(project => project.Differences).Where(difference => releaseMetadataProperties.Contains(difference.Property)).ToArray();
var report = new AuditReport(
    DateTimeOffset.UtcNow,
    new AuditMethod(
        "Microsoft.Build.Evaluation.ProjectCollection",
        Assembly.GetAssembly(typeof(ProjectCollection))?.GetName().Version?.ToString() ?? "unknown",
        ResolvePhysicalPath(sdkToolsPath),
        ResolvePhysicalPath(Directory.GetParent(ResolvePhysicalPath(sdkToolsPath))?.Parent?.FullName
            ?? throw new InvalidOperationException("Could not determine the .NET root from the selected MSBuild SDK.")),
        arguments.Configuration,
        "The evaluator clears inherited environment variables except PATH, TEMP, TMP, TMPDIR, and SystemRoot; it sets explicit SDK resolution paths and disables system/global Git configuration before project evaluation.",
        "The tool calls ProjectCollection evaluation only. It invokes no target, restore, build, test, pack, publish, or deployment command against input projects; imported MSBuild expressions still execute during evaluation, so this is not a security sandbox.",
        "Each evaluation gets a fresh empty MSBuildProjectExtensionsPath beneath a new temporary root, excluding existing obj/NuGet restore props and assets. The tool fails if evaluation writes there.",
        "Release identities are evaluated in the outer build and with TargetFramework set to every framework declared by that outer evaluation."),
    new AuditPins(sourceReceipt.SourceCommits, actualSourcePins, actualConsolidatedHead, sourceReceipt.RehearsalCommit, buildReceipt.RehearsalCommit, inventory.RootElement.GetProperty("snapshot_date").GetString() ?? "unknown"),
    new AuditSummary(
        activeMappings.Length,
        projects.Count,
        projectDifferences,
        propertyDifferences,
        publicIdentityDifferences.Length,
        projects.Count(project => project.Differences.Any(difference => publicIdentityProperties.Contains(difference.Property))),
        releaseMetadataDifferences.Length,
        projects.Count(project => project.Differences.Any(difference => releaseMetadataProperties.Contains(difference.Property))),
        quarantinedMappings.Length,
        evaluationFailures.Count,
        diagnostics.Warnings.Count,
        diagnostics.Errors.Count,
        importedInputs.Count),
    projects,
    quarantinedMappings.Select(mapping => new QuarantinedProject(mapping.Repository, mapping.Source, mapping.Destination, mapping.Blob)).ToArray(),
    importedInputs,
    evaluationFailures,
    diagnostics.Warnings,
    diagnostics.Errors,
    buildReceipt.BuildCompatibilityVerified,
    buildReceipt.PublicationAuthorized,
    "This report compares the clean prepared consolidation baseline at 926c91327efaecce286d50b31f19db376239f05e against the pinned Extensions and Studio sources. It excludes later source-reference corrections in the current rehearsal tree. Identity evaluation does not establish restore/build/test compatibility, package-consumer behavior, release readiness, or publication authorization.");

await using (var output = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
{
    await JsonSerializer.SerializeAsync(output, report, new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });
}

Console.WriteLine($"Identity audit wrote {outputPath}");
Console.WriteLine($"Projects: {projects.Count}/{activeMappings.Length}; projects with identity differences: {projectDifferences}; property differences: {propertyDifferences}; evaluation failures: {evaluationFailures.Count}; quarantined source projects: {quarantinedMappings.Length}.");
Console.WriteLine($"Compatibility verified: {buildReceipt.BuildCompatibilityVerified}; publication authorized: {buildReceipt.PublicationAuthorized}.");

return evaluationFailures.Count == 0 && diagnostics.Errors.Count == 0 ? 0 : 1;

static void ConfigureMsBuildEnvironment(string sdkToolsPath)
{
    var sdkRoot = Path.GetFullPath(sdkToolsPath);
    var dotnetRoot = Directory.GetParent(sdkRoot)?.Parent?.FullName;
    if (dotnetRoot is null)
    {
        throw new InvalidOperationException($"Could not determine the .NET root from MSBuild tools path '{sdkToolsPath}'.");
    }

    var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
    var tempPath = Path.GetTempPath();
    var systemRoot = Environment.GetEnvironmentVariable("SystemRoot");
    foreach (System.Collections.DictionaryEntry item in Environment.GetEnvironmentVariables())
    {
        if (item.Key is string name)
        {
            Environment.SetEnvironmentVariable(name, null);
        }
    }

    Environment.SetEnvironmentVariable("PATH", path);
    Environment.SetEnvironmentVariable("TEMP", tempPath);
    Environment.SetEnvironmentVariable("TMP", tempPath);
    Environment.SetEnvironmentVariable("TMPDIR", tempPath);
    if (!string.IsNullOrEmpty(systemRoot))
    {
        Environment.SetEnvironmentVariable("SystemRoot", systemRoot);
    }
    Environment.SetEnvironmentVariable("MSBuildSDKsPath", Path.Combine(sdkRoot, "Sdks"));
    Environment.SetEnvironmentVariable("MSBUILD_EXE_PATH", Path.Combine(sdkRoot, "MSBuild.dll"));
    Environment.SetEnvironmentVariable("DOTNET_MSBUILD_SDK_RESOLVER_SDKS_DIR", Path.Combine(sdkRoot, "Sdks"));
    Environment.SetEnvironmentVariable("DOTNET_ROOT", dotnetRoot);
    Environment.SetEnvironmentVariable("MSBuildExtensionsPath", sdkRoot);
    Environment.SetEnvironmentVariable("MSBuildExtensionsPath32", sdkRoot);
    Environment.SetEnvironmentVariable("MSBuildExtensionsPath64", sdkRoot);
    Environment.SetEnvironmentVariable("GIT_CONFIG_NOSYSTEM", "1");
    Environment.SetEnvironmentVariable("GIT_CONFIG_GLOBAL", Path.Combine(tempPath, "codex-identity-audit-no-global-git-config"));
}

static ProjectIdentity EvaluateProject(string projectPath, string repository, string relativePath, string configuration, ProjectCollection collection, string scratchRoot, Dictionary<string, string> evaluatedInputs)
{
    var baseGlobals = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Configuration"] = configuration,
        ["Platform"] = "AnyCPU",
        ["CustomBeforeMicrosoftCommonProps"] = string.Empty,
        ["CustomAfterMicrosoftCommonProps"] = string.Empty,
        ["CustomBeforeMicrosoftCommonTargets"] = string.Empty,
        ["CustomAfterMicrosoftCommonTargets"] = string.Empty
    };

    var outer = Evaluate(projectPath, baseGlobals, $"{repository}/{relativePath}/outer", collection, scratchRoot, evaluatedInputs);
    var frameworks = SplitFrameworks(outer.TargetFrameworks, outer.TargetFramework);
    var frameworkIdentities = new List<FrameworkIdentity>(frameworks.Count);
    foreach (var framework in frameworks)
    {
        var frameworkGlobals = new Dictionary<string, string>(baseGlobals, StringComparer.OrdinalIgnoreCase)
        {
            ["TargetFramework"] = framework
        };
        var inner = Evaluate(projectPath, frameworkGlobals, $"{repository}/{relativePath}/{framework}", collection, scratchRoot, evaluatedInputs);
        frameworkIdentities.Add(new FrameworkIdentity(framework, inner));
    }

    return new ProjectIdentity(outer, frameworkIdentities);
}

static IdentityProperties Evaluate(string projectPath, Dictionary<string, string> globals, string scratchKey, ProjectCollection collection, string scratchRoot, Dictionary<string, string> evaluatedInputs)
{
    var scratchPath = Path.Combine(scratchRoot, $"{Guid.NewGuid():N}-{SafePath(scratchKey)}") + Path.DirectorySeparatorChar;
    Directory.CreateDirectory(scratchPath);
    Require(!Directory.EnumerateFileSystemEntries(scratchPath).Any(), $"MSBuildProjectExtensionsPath was not empty before evaluating {scratchKey}.");
    globals["MSBuildProjectExtensionsPath"] = scratchPath;
    Project? project = null;
    try
    {
        project = new Project(projectPath, globals, null, collection);
        foreach (var import in project.Imports)
        {
            var importedProject = import.ImportedProject;
            if (importedProject is null || string.IsNullOrWhiteSpace(importedProject.FullPath))
            {
                throw new InvalidOperationException($"An evaluated import for '{projectPath}' did not resolve to a file.");
            }
            evaluatedInputs.TryAdd(ResolvePhysicalPath(importedProject.FullPath), "msbuild-import");
        }
        return new IdentityProperties(
            Get(project, "PackageId"),
            Get(project, "AssemblyName"),
            Get(project, "RootNamespace"),
            Get(project, "TargetFrameworks"),
            Get(project, "TargetFramework"),
            Get(project, "Version"),
            Get(project, "PackageVersion"),
            Get(project, "IsPackable"));
    }
    finally
    {
        if (project is not null)
        {
            collection.UnloadProject(project);
        }
    }
}

static string Get(Project project, string name) => project.GetPropertyValue(name);

static List<string> SplitFrameworks(string targetFrameworks, string targetFramework)
{
    var values = targetFrameworks.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    if (values.Length == 0 && !string.IsNullOrWhiteSpace(targetFramework))
    {
        values = [targetFramework.Trim()];
    }
    return values.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
}

static ProjectComparison Compare(Mapping mapping, string consolidatedPath, ProjectIdentity original, ProjectIdentity consolidated)
{
    var differences = new List<IdentityDifference>();
    CompareProperties("outer", original.Outer, consolidated.Outer, differences);

    var originalFrameworks = original.Frameworks.ToDictionary(item => item.TargetFramework, item => item.Identity, StringComparer.OrdinalIgnoreCase);
    var consolidatedFrameworks = consolidated.Frameworks.ToDictionary(item => item.TargetFramework, item => item.Identity, StringComparer.OrdinalIgnoreCase);
    foreach (var framework in originalFrameworks.Keys.Union(consolidatedFrameworks.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
    {
        originalFrameworks.TryGetValue(framework, out var originalProperties);
        consolidatedFrameworks.TryGetValue(framework, out var consolidatedProperties);
        if (originalProperties is null || consolidatedProperties is null)
        {
            differences.Add(new IdentityDifference(framework, "DeclaredTargetFramework", originalProperties is null ? "<absent>" : "<present>", consolidatedProperties is null ? "<absent>" : "<present>"));
            continue;
        }
        CompareProperties(framework, originalProperties, consolidatedProperties, differences);
    }

    return new ProjectComparison(
        mapping.Repository,
        mapping.Source,
        mapping.Destination,
        mapping.Blob,
        FileSha256(consolidatedPath),
        original,
        consolidated,
        differences);
}

static void CompareProperties(string context, IdentityProperties original, IdentityProperties consolidated, List<IdentityDifference> differences)
{
    CompareProperty(context, "PackageId", original.PackageId, consolidated.PackageId, differences);
    CompareProperty(context, "AssemblyName", original.AssemblyName, consolidated.AssemblyName, differences);
    CompareProperty(context, "RootNamespace", original.RootNamespace, consolidated.RootNamespace, differences);
    CompareProperty(context, "TargetFrameworks", original.TargetFrameworks, consolidated.TargetFrameworks, differences);
    CompareProperty(context, "TargetFramework", original.TargetFramework, consolidated.TargetFramework, differences);
    CompareProperty(context, "Version", original.Version, consolidated.Version, differences);
    CompareProperty(context, "PackageVersion", original.PackageVersion, consolidated.PackageVersion, differences);
    CompareProperty(context, "IsPackable", original.IsPackable, consolidated.IsPackable, differences);
}

static void CompareProperty(string context, string name, string original, string consolidated, List<IdentityDifference> differences)
{
    if (!string.Equals(original, consolidated, StringComparison.Ordinal))
    {
        differences.Add(new IdentityDifference(context, name, original, consolidated));
    }
}

static void VerifyInventory(JsonElement inventory, IReadOnlyCollection<Mapping> mappings)
{
    var projectInventory = inventory.GetProperty("project_inventory");
    foreach (var repository in new[] { "extensions", "studio" })
    {
        var inventoryName = repository == "extensions" ? "elsa-extensions" : "elsa-studio";
        var inventoryPaths = projectInventory.GetProperty(inventoryName)
            .EnumerateArray()
            .Select(row => row.GetProperty("path").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);
        var mappedPaths = mappings
            .Where(mapping => string.Equals(mapping.Repository, repository, StringComparison.OrdinalIgnoreCase))
            .Select(mapping => mapping.Source)
            .ToHashSet(StringComparer.Ordinal);
        Require(inventoryPaths.SetEquals(mappedPaths),
            $"Inventory project paths for {repository} differ from imported project mappings (inventory={inventoryPaths.Count}, mappings={mappedPaths.Count}).");
    }
}

static void VerifySourceBlobs(IReadOnlyDictionary<string, string> roots, IReadOnlyDictionary<string, string> pins, IReadOnlyCollection<Mapping> mappings)
{
    foreach (var repository in new[] { "extensions", "studio" })
    {
        var repositoryMappings = mappings.Where(mapping => string.Equals(mapping.Repository, repository, StringComparison.OrdinalIgnoreCase)).ToArray();
        var arguments = new List<string> { "rev-parse" };
        arguments.AddRange(repositoryMappings.Select(mapping => $"{pins[repository]}:{mapping.Source}"));
        var blobs = Git(roots[repository], arguments.ToArray()).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Require(blobs.Length == repositoryMappings.Length,
            $"Git returned {blobs.Length} source blobs for {repository}; expected {repositoryMappings.Length}.");
        for (var index = 0; index < repositoryMappings.Length; index++)
        {
            Require(string.Equals(blobs[index], repositoryMappings[index].Blob, StringComparison.OrdinalIgnoreCase),
                $"Import blob mismatch for {repositoryMappings[index].Source}: receipt {repositoryMappings[index].Blob}, pinned commit {blobs[index]}.");
        }
    }
}

static void VerifyProjectFilesAreAtRecordedHeads(IReadOnlyDictionary<string, string> roots, string consolidatedRoot, IReadOnlyCollection<Mapping> mappings)
{
    foreach (var repository in new[] { "extensions", "studio" })
    {
        var relativePaths = mappings
            .Where(mapping => string.Equals(mapping.Repository, repository, StringComparison.OrdinalIgnoreCase))
            .Select(mapping => mapping.Source)
            .ToArray();
        VerifyPathsAtHead(roots[repository], relativePaths, repository);
    }

    VerifyPathsAtHead(consolidatedRoot, mappings.Select(mapping => mapping.Destination).ToArray(), "consolidated");
}

static void VerifyBuildReceiptFiles(string consolidatedRoot, IReadOnlyCollection<ReceiptFile> files)
{
    foreach (var file in files)
    {
        var path = ResolveInside(consolidatedRoot, file.Path);
        Require(string.Equals(FileSha256(path), file.Sha256, StringComparison.OrdinalIgnoreCase),
            $"Consolidation preparation receipt hash mismatch for {file.Path}.");
    }
}

static void VerifyCleanBuildInputs(IEnumerable<KeyValuePair<string, string>> roots)
{
    foreach (var (name, root) in roots)
    {
        var trackedChanges = Run("git", root, ["diff", "--quiet", "HEAD", "--"]);
        Require(trackedChanges.ExitCode == 0, $"Tracked files in {name} are modified; identity comparison requires a clean pinned input tree.");

        var untrackedResult = Run("git", root, ["ls-files", "--others", "--exclude-standard", "-z"]);
        Require(untrackedResult.ExitCode == 0, $"Could not enumerate untracked inputs for {name}: {untrackedResult.StandardError}");
        var untrackedBuildInputs = untrackedResult.StandardOutput
            .Split('\0', StringSplitOptions.RemoveEmptyEntries)
            .Where(path => !IsBuildOutputPath(path) && IsMsBuildInputPath(path))
            .ToArray();
        Require(untrackedBuildInputs.Length == 0,
            $"Untracked MSBuild inputs in {name} could affect evaluation: {string.Join(", ", untrackedBuildInputs)}.");
    }
}

static bool IsBuildOutputPath(string path) => path.Split(['/','\\'], StringSplitOptions.RemoveEmptyEntries).Any(segment => segment is "bin" or "obj");

static bool IsMsBuildInputPath(string path)
{
    var fileName = Path.GetFileName(path);
    return fileName.Equals("global.json", StringComparison.OrdinalIgnoreCase)
        || Path.GetExtension(path).Equals(".csproj", StringComparison.OrdinalIgnoreCase)
        || Path.GetExtension(path).Equals(".props", StringComparison.OrdinalIgnoreCase)
        || Path.GetExtension(path).Equals(".targets", StringComparison.OrdinalIgnoreCase);
}

static IReadOnlyCollection<string> FindGlobalJsonInputs(IReadOnlyDictionary<string, string> sourceRoots, string consolidatedRoot, IReadOnlyCollection<Mapping> mappings)
{
    var projects = mappings.SelectMany(mapping => new[]
    {
        ResolveInside(sourceRoots[mapping.Repository], mapping.Source),
        ResolveInside(consolidatedRoot, mapping.Destination)
    });
    var results = new HashSet<string>(AuditPaths.Comparer);
    foreach (var project in projects.Append(Directory.GetCurrentDirectory()))
    {
        var directory = File.Exists(project) ? Path.GetDirectoryName(project) : Path.GetFullPath(project);
        while (!string.IsNullOrEmpty(directory))
        {
            var globalJson = Path.Combine(directory, "global.json");
            if (File.Exists(globalJson))
            {
                results.Add(ResolvePhysicalPath(globalJson));
            }

            var parent = Directory.GetParent(directory)?.FullName;
            if (parent is null || string.Equals(parent, directory, AuditPaths.Comparison))
            {
                break;
            }

            directory = parent;
        }
    }

    return results;
}

static IReadOnlyCollection<ImportedFileEvidence> VerifyAndHashImportedInputs(IReadOnlyDictionary<string, string> sourceRoots, string consolidatedRoot, string sdkToolsPath, IReadOnlyDictionary<string, string> importedPaths)
{
    var physicalSdkRoot = ResolvePhysicalPath(sdkToolsPath);
    var physicalDotnetRoot = ResolvePhysicalPath(Directory.GetParent(physicalSdkRoot)?.Parent?.FullName
        ?? throw new InvalidOperationException($"Could not determine the .NET root from selected SDK '{sdkToolsPath}'."));
    var sdkManifestRoot = ResolvePhysicalPath(Path.Combine(physicalDotnetRoot, "sdk-manifests"));
    var roots = sourceRoots
        .Select(pair => new InputRoot(pair.Key, ResolvePhysicalPath(pair.Value)))
        .Append(new InputRoot("consolidated", ResolvePhysicalPath(consolidatedRoot)))
        .OrderByDescending(root => root.Path.Length)
        .ToArray();
    var sourceInputs = roots.ToDictionary(root => root.Name, _ => new Dictionary<string, string>(StringComparer.Ordinal), StringComparer.OrdinalIgnoreCase);
    var sdkInputs = new Dictionary<string, string>(AuditPaths.Comparer);
    var unexpectedImports = new List<string>();

    foreach (var input in importedPaths)
    {
        var importedPath = ResolvePhysicalPath(input.Key);
        Require(File.Exists(importedPath), $"Evaluated import is missing: {importedPath}.");
        var sourceRoot = roots.FirstOrDefault(root => IsWithin(root.Path, importedPath));
        if (sourceRoot is not null)
        {
            sourceInputs[sourceRoot.Name].TryAdd(Path.GetRelativePath(sourceRoot.Path, importedPath), input.Value);
        }
        else if (IsWithin(physicalSdkRoot, importedPath))
        {
            sdkInputs.TryAdd(importedPath, "sdk-import");
        }
        else if (IsWithin(sdkManifestRoot, importedPath))
        {
            sdkInputs.TryAdd(importedPath, "sdk-workload-manifest");
        }
        else
        {
            unexpectedImports.Add(importedPath);
        }
    }

    Require(unexpectedImports.Count == 0,
        $"MSBuild evaluated unexpected imports outside the pinned project roots and selected SDK: {string.Join(", ", unexpectedImports)}.");

    var evidence = new List<ImportedFileEvidence>();
    foreach (var root in roots)
    {
        var paths = sourceInputs[root.Name].Keys.OrderBy(path => path, StringComparer.Ordinal).ToArray();
        VerifyPathsAtHead(root.Path, paths, root.Name);
        VerifyTrackedPaths(root.Path, paths, root.Name);
        evidence.AddRange(paths.Select(path => new ImportedFileEvidence(root.Name, path, FileSha256(Path.Combine(root.Path, path)), sourceInputs[root.Name][path])));
    }

    evidence.AddRange(sdkInputs
        .OrderBy(input => input.Key, AuditPaths.Comparer)
        .Select(input => new ImportedFileEvidence("sdk", Path.GetRelativePath(physicalSdkRoot, input.Key), FileSha256(input.Key), input.Value)));
    return evidence;
}

static void VerifyTrackedPaths(string root, IReadOnlyCollection<string> paths, string label)
{
    var arguments = new List<string> { "ls-files", "-z", "--" };
    arguments.AddRange(paths);
    var result = Run("git", root, arguments);
    Require(result.ExitCode == 0, $"Could not enumerate tracked evaluation inputs for {label}: {result.StandardError}");
    var tracked = result.StandardOutput.Split('\0', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
    var expected = paths.Select(path => path.Replace(Path.DirectorySeparatorChar, '/')).ToHashSet(StringComparer.Ordinal);
    Require(expected.SetEquals(tracked), $"One or more evaluated {label} inputs are not tracked at the recorded commit.");
}

static void VerifyPathsAtHead(string root, IReadOnlyCollection<string> paths, string label)
{
    var gitArguments = new List<string> { "diff", "--quiet", "HEAD", "--" };
    gitArguments.AddRange(paths);
    var result = Run("git", root, gitArguments);
    Require(result.ExitCode == 0, $"Mapped project files in {label} have tracked worktree changes; refuse to evaluate a mixed source state.");
}

static void RequirePin(string label, string expected, string actual)
{
    Require(string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase), $"{label} pin mismatch: expected {expected}, found {actual}.");
}

static void RequireAncestor(string repository, string ancestor, string head, string label)
{
    var result = Run("git", repository, ["merge-base", "--is-ancestor", ancestor, head]);
    Require(result.ExitCode == 0, $"{label} commit {ancestor} is not an ancestor of evaluated consolidated HEAD {head}.");
}

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static string ResolveInside(string root, string relativePath)
{
    var fullRoot = ResolvePhysicalPath(root);
    var fullPath = ResolvePhysicalPath(Path.Combine(root, relativePath));
    Require(IsWithin(fullRoot, fullPath), $"Project path '{relativePath}' escapes root '{root}'.");
    Require(File.Exists(fullPath), $"Mapped file does not exist: {fullPath}.");
    return fullPath;
}

static string ResolvePhysicalPath(string path)
{
    var fullPath = Path.GetFullPath(path);
    var root = Path.GetPathRoot(fullPath) ?? throw new InvalidOperationException($"Path has no root: {path}.");
    var remaining = fullPath[root.Length..].Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
    var current = root;
    for (var index = 0; index < remaining.Length; index++)
    {
        var candidate = Path.Combine(current, remaining[index]);
        FileSystemInfo info = Directory.Exists(candidate) ? new DirectoryInfo(candidate) : new FileInfo(candidate);
        var linkTarget = info.LinkTarget;
        if (!string.IsNullOrEmpty(linkTarget))
        {
            current = info.ResolveLinkTarget(returnFinalTarget: true)?.FullName
                ?? throw new InvalidOperationException($"Could not resolve symlink ancestor '{candidate}'.");
            continue;
        }

        if (!info.Exists)
        {
            current = Path.Combine(current, Path.Combine(remaining[index..]));
            break;
        }

        current = candidate;
    }

    return Path.GetFullPath(current);
}

static bool IsWithin(string root, string path)
{
    var physicalRoot = ResolvePhysicalPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    var physicalPath = ResolvePhysicalPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    return string.Equals(physicalRoot, physicalPath, AuditPaths.Comparison)
        || physicalPath.StartsWith(physicalRoot + Path.DirectorySeparatorChar, AuditPaths.Comparison);
}

static void RequireSamePath(string suppliedPath, string expectedPath, string label)
{
    Require(string.Equals(ResolvePhysicalPath(suppliedPath), ResolvePhysicalPath(expectedPath), AuditPaths.Comparison),
        $"The supplied {label} is not the pinned input at {expectedPath}.");
}

static string ValidateOutputPath(string output, IReadOnlyCollection<string> inputRoots, IReadOnlyCollection<string> inputFiles)
{
    var fullOutput = Path.GetFullPath(output);
    var parent = Path.GetDirectoryName(fullOutput) ?? throw new InvalidOperationException("Output path must have a parent directory.");
    Require(Directory.Exists(parent), $"Output parent directory must already exist: {parent}.");
    Require(!File.Exists(fullOutput) && !Directory.Exists(fullOutput), $"Output path already exists; choose a new filename: {fullOutput}.");
    var physicalOutput = ResolvePhysicalPath(fullOutput);
    foreach (var inputRoot in inputRoots)
    {
        Require(!IsWithin(inputRoot, physicalOutput), $"Output must be outside inspected input root '{inputRoot}'.");
    }
    foreach (var inputFile in inputFiles)
    {
        Require(!string.Equals(physicalOutput, ResolvePhysicalPath(inputFile), AuditPaths.Comparison), $"Output aliases an input file: {inputFile}.");
    }
    return physicalOutput;
}

static string FileSha256(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();

static string SafePath(string value) => string.Concat(value.Select(character => char.IsLetterOrDigit(character) ? character : '_'));

static T ReadJson<T>(string path)
{
    using var stream = File.OpenRead(path);
    return JsonSerializer.Deserialize<T>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        ?? throw new InvalidOperationException($"Could not parse JSON input: {path}.");
}

static string Git(string root, params string[] arguments)
{
    var result = Run("git", root, arguments);
    Require(result.ExitCode == 0, $"git -C {root} {string.Join(' ', arguments)} failed: {result.StandardError}");
    return result.StandardOutput.Trim();
}

static CommandResult Run(string fileName, string workingDirectory, IEnumerable<string> arguments)
{
    var startInfo = new ProcessStartInfo(fileName)
    {
        WorkingDirectory = workingDirectory,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    };
    foreach (var argument in arguments)
    {
        startInfo.ArgumentList.Add(argument);
    }
    using var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Could not start {fileName}.");
    var stdout = process.StandardOutput.ReadToEnd();
    var stderr = process.StandardError.ReadToEnd();
    process.WaitForExit();
    return new CommandResult(process.ExitCode, stdout, stderr);
}

internal sealed record Arguments(string ConsolidatedRoot, string ExtensionsRoot, string StudioRoot, string ImportReceipt, string BuildReceipt, string Inventory, string Output, string Configuration)
{
    public static Arguments Parse(string[] values)
    {
        var options = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < values.Length; index += 2)
        {
            if (index + 1 >= values.Length || !values[index].StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException("Arguments must be --name value pairs.");
            }
            options.Add(values[index], values[index + 1]);
        }

        var allowed = new HashSet<string>(["--consolidated-root", "--extensions-root", "--studio-root", "--import-receipt", "--build-receipt", "--inventory", "--output", "--configuration"], StringComparer.Ordinal);
        var unknown = options.Keys.Where(key => !allowed.Contains(key)).ToArray();
        if (unknown.Length > 0)
        {
            throw new ArgumentException($"Unknown arguments: {string.Join(", ", unknown)}.");
        }

        var parsed = new Arguments(
            GetRequired(options, "--consolidated-root"),
            GetRequired(options, "--extensions-root"),
            GetRequired(options, "--studio-root"),
            GetRequired(options, "--import-receipt"),
            GetRequired(options, "--build-receipt"),
            GetRequired(options, "--inventory"),
            GetRequired(options, "--output"),
            options.GetValueOrDefault("--configuration", "Release"));
        return parsed;
    }

    private static string GetRequired(IReadOnlyDictionary<string, string> options, string name) =>
        options.TryGetValue(name, out var value) ? value : throw new ArgumentException($"Missing required argument {name}.");
}

internal sealed record CommandResult(int ExitCode, string StandardOutput, string StandardError);
internal sealed record Mapping(string Repository, string Source, string Destination, string Mode, string Blob);
internal sealed record ImportReceipt(
    [property: JsonPropertyName("sourceCommits")] Dictionary<string, string> SourceCommits,
    [property: JsonPropertyName("rehearsalCommit")] string RehearsalCommit,
    [property: JsonPropertyName("mapping")] List<Mapping> Mapping);
internal sealed record BuildReceipt(
    [property: JsonPropertyName("sourceCommits")] Dictionary<string, string> SourceCommits,
    [property: JsonPropertyName("rehearsalCommit")] string RehearsalCommit,
    [property: JsonPropertyName("importedProjects")] int ImportedProjects,
    [property: JsonPropertyName("buildCompatibilityVerified")] bool BuildCompatibilityVerified,
    [property: JsonPropertyName("publicationAuthorized")] bool PublicationAuthorized,
    [property: JsonPropertyName("files")] List<ReceiptFile> Files);
internal sealed record ReceiptFile(string Path, string Sha256);
internal sealed record IdentityProperties(string PackageId, string AssemblyName, string RootNamespace, string TargetFrameworks, string TargetFramework, string Version, string PackageVersion, string IsPackable);
internal sealed record FrameworkIdentity(string TargetFramework, IdentityProperties Identity);
internal sealed record ProjectIdentity(IdentityProperties Outer, IReadOnlyCollection<FrameworkIdentity> Frameworks);
internal sealed record IdentityDifference(string Context, string Property, string Original, string Consolidated);
internal sealed record ProjectComparison(string Repository, string SourceProject, string ConsolidatedProject, string SourceBlob, string ConsolidatedProjectSha256, ProjectIdentity Original, ProjectIdentity Consolidated, IReadOnlyCollection<IdentityDifference> Differences);
internal sealed record QuarantinedProject(string Repository, string SourceProject, string Destination, string SourceBlob);
internal sealed record EvaluationFailure(string Repository, string SourceProject, string ConsolidatedProject, string Error);
internal sealed record InputRoot(string Name, string Path);
internal sealed record ImportedFileEvidence(string Root, string Path, string Sha256, string Role);
internal sealed record AuditMethod(string Engine, string MsBuildAssemblyVersion, string SdkToolsPath, string DotnetRoot, string Configuration, string EnvironmentPolicy, string ExecutionBoundary, string RestoreIsolation, string FrameworkCoverage);
internal sealed record AuditPins(Dictionary<string, string> SourceCommits, Dictionary<string, string> EvaluatedSourceHeads, string ConsolidatedHead, string ImportReceiptRehearsalCommit, string BuildReceiptRehearsalCommit, string InventorySnapshotDate);
internal sealed record AuditSummary(int ActiveProjectCount, int EvaluatedProjectCount, int ProjectsWithDifferences, int PropertyDifferenceCount, int PublicIdentityDifferenceCount, int ProjectsWithPublicIdentityDifferences, int ReleaseMetadataDifferenceCount, int ProjectsWithReleaseMetadataDifferences, int QuarantinedProjectCount, int EvaluationFailureCount, int EvaluationWarningCount, int EvaluationErrorCount, int VerifiedInputFileCount);
internal sealed record AuditReport(DateTimeOffset GeneratedAtUtc, AuditMethod Method, AuditPins Pins, AuditSummary Summary, IReadOnlyCollection<ProjectComparison> Projects, IReadOnlyCollection<QuarantinedProject> QuarantinedProjects, IReadOnlyCollection<ImportedFileEvidence> ImportedInputs, IReadOnlyCollection<EvaluationFailure> EvaluationFailures, IReadOnlyCollection<string> EvaluationWarnings, IReadOnlyCollection<string> EvaluationErrors, bool CompatibilityVerifiedInBuildReceipt, bool PublicationAuthorizedInBuildReceipt, string Limitation);

internal static class AuditConstants
{
    public const string ExpectedConsolidatedHead = "926c91327efaecce286d50b31f19db376239f05e";
}

internal static class AuditPaths
{
    public static StringComparison Comparison => OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    public static StringComparer Comparer => OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
}

internal sealed class EvaluationScratch : IDisposable
{
    public EvaluationScratch()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"elsa-consolidation-identity-eval-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        RootPath = System.IO.Path.GetFullPath(path);
        if (Directory.EnumerateFileSystemEntries(RootPath).Any())
        {
            throw new InvalidOperationException($"MSBuild scratch root is not empty: {RootPath}.");
        }
    }

    public string RootPath { get; }
    public IReadOnlyCollection<string> UnexpectedEntries
    {
        get
        {
            var topLevelEntries = Directory.EnumerateFileSystemEntries(RootPath).ToArray();
            return topLevelEntries
                .Where(File.Exists)
                .Concat(topLevelEntries
                    .Where(Directory.Exists)
                    .SelectMany(path => Directory.EnumerateFileSystemEntries(path, "*", SearchOption.AllDirectories)))
                .ToArray();
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(RootPath))
        {
            Directory.Delete(RootPath, recursive: true);
        }
    }
}

internal sealed class EvaluationDiagnostics : ILogger
{
    public LoggerVerbosity Verbosity { get; set; } = LoggerVerbosity.Quiet;
    public string? Parameters { get; set; }
    public List<string> Warnings { get; } = [];
    public List<string> Errors { get; } = [];

    public void Initialize(IEventSource eventSource)
    {
        eventSource.WarningRaised += (_, warning) => Warnings.Add(Format(warning));
        eventSource.ErrorRaised += (_, error) => Errors.Add(Format(error));
    }

    public void Shutdown()
    {
    }

    private static string Format(BuildWarningEventArgs eventArgs) =>
        $"{eventArgs.File}({eventArgs.LineNumber},{eventArgs.ColumnNumber}) {eventArgs.Code}: {eventArgs.Message}";

    private static string Format(BuildErrorEventArgs eventArgs) =>
        $"{eventArgs.File}({eventArgs.LineNumber},{eventArgs.ColumnNumber}) {eventArgs.Code}: {eventArgs.Message}";
}
