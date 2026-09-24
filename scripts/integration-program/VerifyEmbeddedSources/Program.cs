using System.IO.Compression;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text.Json;

if (args.Length != 2)
{
    throw new ArgumentException("Usage: VerifyEmbeddedSources <portable-pdb> <pinned-extensions-slack-source-directory>");
}

var pdbPath = Path.GetFullPath(args[0]);
var sourceRoot = Path.GetFullPath(args[1]);
if (!File.Exists(pdbPath) || !Directory.Exists(sourceRoot))
{
    throw new ArgumentException("Portable PDB and pinned Slack source directory must exist.");
}

const string mappedSourceMarker = "src/extensions/communication/Elsa.Slack/";
var embeddedSourceKind = new Guid("0e8a571b-6926-466e-b4ad-8ab04611f5fe");
var expectedFiles = Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
    .Select(path => Path.GetRelativePath(sourceRoot, path).Replace(Path.DirectorySeparatorChar, '/'))
    .Where(path => !path.StartsWith("bin/", StringComparison.Ordinal) && !path.StartsWith("obj/", StringComparison.Ordinal))
    .ToHashSet(StringComparer.Ordinal);
var embeddedFiles = new Dictionary<string, string>(StringComparer.Ordinal);

using var pdbStream = File.OpenRead(pdbPath);
using var provider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
var reader = provider.GetMetadataReader();
foreach (var handle in reader.CustomDebugInformation)
{
    var information = reader.GetCustomDebugInformation(handle);
    if (reader.GetGuid(information.Kind) != embeddedSourceKind || information.Parent.Kind != HandleKind.Document)
    {
        continue;
    }

    var documentHandle = MetadataTokens.DocumentHandle(MetadataTokens.GetRowNumber(information.Parent));
    var document = reader.GetDocument(documentHandle);
    var path = reader.GetString(document.Name).Replace('\\', '/');
    var markerIndex = path.LastIndexOf(mappedSourceMarker, StringComparison.Ordinal);
    if (markerIndex < 0)
    {
        continue;
    }

    var relativePath = path[(markerIndex + mappedSourceMarker.Length)..];
    if (relativePath.StartsWith("obj/", StringComparison.Ordinal) || !relativePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
    {
        continue;
    }

    if (!expectedFiles.Contains(relativePath))
    {
        throw new InvalidDataException($"PDB embeds unexpected Slack source document: {relativePath}");
    }

    var embedded = reader.GetBlobReader(information.Value);
    var uncompressedSize = embedded.ReadInt32();
    var payload = embedded.ReadBytes(embedded.RemainingBytes);
    byte[] source;
    if (uncompressedSize == 0)
    {
        source = payload;
    }
    else
    {
        using var compressed = new MemoryStream(payload);
        using var deflate = new DeflateStream(compressed, CompressionMode.Decompress);
        using var restored = new MemoryStream();
        deflate.CopyTo(restored);
        source = restored.ToArray();
        if (source.Length != uncompressedSize)
        {
            throw new InvalidDataException($"Wrong embedded source length for {relativePath}: {source.Length} != {uncompressedSize}");
        }
    }

    var sourcePath = Path.GetFullPath(Path.Combine(sourceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
    var sourceRootPrefix = sourceRoot.EndsWith(Path.DirectorySeparatorChar)
        ? sourceRoot
        : sourceRoot + Path.DirectorySeparatorChar;
    if (!sourcePath.StartsWith(sourceRootPrefix, StringComparison.Ordinal) || !File.Exists(sourcePath))
    {
        throw new InvalidDataException($"Pinned Slack source path is missing or outside its root: {relativePath}");
    }

    var embeddedHash = Convert.ToHexString(SHA256.HashData(source)).ToLowerInvariant();
    var pinnedHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(sourcePath))).ToLowerInvariant();
    if (!StringComparer.Ordinal.Equals(embeddedHash, pinnedHash))
    {
        throw new InvalidDataException($"Embedded source differs from pinned Extensions source: {relativePath}");
    }

    if (!embeddedFiles.TryAdd(relativePath, embeddedHash))
    {
        throw new InvalidDataException($"Duplicate embedded Slack source document: {relativePath}");
    }
}

var missingFiles = expectedFiles.Except(embeddedFiles.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
if (missingFiles.Length > 0)
{
    throw new InvalidDataException($"Portable PDB omits Slack source files: {string.Join(", ", missingFiles)}");
}

Console.WriteLine(JsonSerializer.Serialize(new
{
    result = "passed",
    pdb = pdbPath,
    pinnedSourceRoot = sourceRoot,
    embeddedSourceCount = embeddedFiles.Count,
    embeddedSources = embeddedFiles.OrderBy(row => row.Key, StringComparer.Ordinal)
        .Select(row => new { path = row.Key, sha256 = row.Value }),
    limitation = "The rehearsal has no remote and produces no SourceLink URLs. Embedded C# bytes are verified against the pinned Extensions source; final Core-repository SourceLink URLs remain unverified until the imported history is published to its real remote."
}, new JsonSerializerOptions { WriteIndented = true }));
