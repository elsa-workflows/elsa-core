using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using NuGet.Frameworks;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Versioning;

// One bounded JSON request; no feed, credential, package creation or publisher code.
try
{
    using var input = JsonDocument.Parse(Console.In.ReadToEnd());
    var request = input.RootElement;
    object result = request.GetProperty("operation").GetString() switch
    {
        "versions" => request.GetProperty("values").EnumerateArray().Select(value => Version(value.GetString()!)).ToArray(),
        "history" => History(request),
        "ranges" => request.GetProperty("values").EnumerateArray().Select(Range).ToArray(),
        "frameworks" => request.GetProperty("values").EnumerateArray().Select(Framework).ToArray(),
        "nuspecs" => request.GetProperty("values").EnumerateArray().Select(Nuspec).ToArray(),
        "feeds" => Feeds(request),
        "identity" => new[] { typeof(NuGetVersion).Assembly, typeof(NuGetFramework).Assembly, typeof(NuspecReader).Assembly,
            typeof(PackageSourceMapping).Assembly, typeof(NuGet.Common.NullLogger).Assembly }
            .Select(assembly => new
            {
                name = assembly.GetName().Name, version = assembly.GetName().Version!.ToString(),
                file_version = FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion,
                sha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(assembly.Location)))
            }).ToArray(),
        _ => throw new InvalidDataException("Unknown semantic operation")
    };
    Console.WriteLine(JsonSerializer.Serialize(result));
}
catch (Exception)
{
    // Never print raw metadata, paths or stack traces in a public plan.
    Console.Error.WriteLine("invalid_nuget_semantics_input");
    return 1;
}
return 0;

static object Feeds(JsonElement request)
{
    var config = Path.GetFullPath(request.GetProperty("config").GetString()!);
    var settings = Settings.LoadSpecificSettings(Path.GetDirectoryName(config)!, Path.GetFileName(config));
    var sources = new PackageSourceProvider(settings).LoadPackageSources().Where(source => source.IsEnabled).ToArray();
    if (sources.Select(source => source.Name.ToLowerInvariant()).Distinct().Count() != sources.Length)
        throw new InvalidDataException("Duplicate source identity");
    var mapping = PackageSourceMapping.GetPackageSourceMapping(settings);
    return new { sources = sources.Select(source => new { name = source.Name, url = source.Source }).ToArray(),
        mapping_enabled = mapping.IsEnabled,
        packages = request.GetProperty("ids").EnumerateArray().Select(value => value.GetString()!).Select(id => new
        {
            id, sources = mapping.IsEnabled ? mapping.GetConfiguredPackageSources(id).ToArray() : sources.Select(source => source.Name).ToArray()
        }).ToArray() };
}

static object Version(string raw)
{
    var version = NuGetVersion.Parse(raw);
    return new { raw, normalized = version.ToNormalizedString(), major = version.Major, minor = version.Minor,
        prerelease = version.IsPrerelease };
}

static object History(JsonElement request)
{
    var requested = NuGetVersion.Parse(request.GetProperty("requested").GetString()!);
    var line = request.GetProperty("line").GetString()!;
    if ($"{requested.Major}.{requested.Minor}" != line)
        throw new InvalidDataException("Wrong line");
    var observed = request.GetProperty("versions").EnumerateArray().Select(value => NuGetVersion.Parse(value.GetString()!)).ToArray();
    var comparer = VersionComparer.VersionRelease;
    var duplicate = observed.Distinct(comparer).Count() != observed.Length;
    var reused = observed.Any(version => comparer.Equals(version, requested));
    var lineHistory = observed.Where(version => version.Major == requested.Major && version.Minor == requested.Minor).ToArray();
    var floor = lineHistory.OrderBy(version => version, comparer).LastOrDefault();
    return new { normalized = requested.ToNormalizedString(), duplicate, reused,
        latest_in_line = floor?.ToNormalizedString(), monotonic = floor is null || comparer.Compare(requested, floor) > 0 };
}

static object Range(JsonElement value)
{
    var range = VersionRange.Parse(value.GetProperty("range").GetString()!);
    var version = NuGetVersion.Parse(value.GetProperty("version").GetString()!);
    return new { normalized = range.ToNormalizedString(), satisfies = range.Satisfies(version),
        version = version.ToNormalizedString() };
}

static NuGetFramework ParseFramework(string value) => value == "" ? NuGetFramework.AnyFramework : NuGetFramework.Parse(value);

static object Framework(JsonElement value)
{
    var consumer = ParseFramework(value.GetProperty("consumer").GetString()!);
    var candidates = value.GetProperty("candidates").EnumerateArray().Select(item => ParseFramework(item.GetString()!)).ToArray();
    if (consumer.IsUnsupported || candidates.Any(item => item.IsUnsupported) || candidates.Distinct().Count() != candidates.Length)
        throw new InvalidDataException("Invalid or duplicate framework");
    var nearest = new FrameworkReducer().GetNearest(consumer, candidates);
    return new { consumer = consumer.GetShortFolderName(), nearest = nearest?.GetShortFolderName(),
        compatible = nearest is not null && DefaultCompatibilityProvider.Instance.IsCompatible(consumer, nearest) };
}

static object Nuspec(JsonElement value)
{
    var xml = value.GetString()!;
    using var xmlReader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings
        { DtdProcessing = DtdProcessing.Prohibit, MaxCharactersInDocument = 16 * 1024 * 1024 });
    var document = XDocument.Load(xmlReader);
    var metadata = document.Root!.Elements().Single(element => element.Name.LocalName == "metadata");
    var containers = metadata.Elements().Where(element => element.Name.LocalName == "dependencies").ToArray();
    if (containers.Length > 1)
        throw new InvalidDataException("Duplicate dependency container");
    var rawGroups = containers.SelectMany(element => element.Elements().Where(child => child.Name.LocalName == "group")).ToArray();
    var flat = containers.SelectMany(element => element.Elements().Where(child => child.Name.LocalName == "dependency")).ToArray();
    if (rawGroups.Length > 0 && flat.Length > 0 ||
        flat.Select(dependency => ((string?)dependency.Attribute("id") ?? "").ToLowerInvariant()).Distinct().Count() != flat.Length ||
        containers.Any(container => container.Elements().Any(child => child.Name.LocalName is not ("group" or "dependency"))))
        throw new InvalidDataException("Ambiguous dependency structure");
    // NuspecReader can merge groups/dependencies. Reject ambiguity before that normalization.
    if (rawGroups.Select(group => ParseFramework((string?)group.Attribute("targetFramework") ?? "")).Distinct().Count() != rawGroups.Length ||
        rawGroups.Any(group => group.Elements().Any(child => child.Name.LocalName != "dependency") ||
            group.Elements().Select(dependency => ((string?)dependency.Attribute("id") ?? "").ToLowerInvariant())
            .Distinct().Count() != group.Elements().Count()))
        throw new InvalidDataException("Duplicate dependency identity");
    using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
    var reader = new NuspecReader(stream);
    var groups = reader.GetDependencyGroups(true).ToArray();
    if (groups.Select(group => group.TargetFramework).Distinct().Count() != groups.Length ||
        groups.Any(group => group.TargetFramework.IsUnsupported ||
            group.Packages.Select(package => package.Id.ToLowerInvariant()).Distinct().Count() != group.Packages.Count()))
        throw new InvalidDataException("Duplicate dependency identity");
    return new { id = reader.GetId(), version = reader.GetVersion().ToNormalizedString(),
        groups = groups.Select(group => new { framework = group.TargetFramework.GetShortFolderName(),
            dependencies = group.Packages.Select(package => new { id = package.Id,
                range = package.VersionRange.ToNormalizedString() }).OrderBy(package => package.id, StringComparer.OrdinalIgnoreCase).ToArray() }).ToArray() };
}
