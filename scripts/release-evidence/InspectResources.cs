// Read manifest resource names and hashes from packaged assemblies without loading them.
using System.IO.Compression;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

using var package = ZipFile.OpenRead(args.Single());
var result = new Dictionary<string, Dictionary<string, string>>();
foreach (var entry in package.Entries.Where(entry => entry.FullName.StartsWith("lib/", StringComparison.Ordinal)
                                                     && entry.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)))
{
    using var bytes = new MemoryStream();
    using (var stream = entry.Open())
        stream.CopyTo(bytes);
    bytes.Position = 0;
    using var reader = new PEReader(bytes);
    var metadata = reader.GetMetadataReader();
    var resources = reader.PEHeaders.CorHeader!.ResourcesDirectory;
    var entries = new Dictionary<string, string>();
    foreach (var handle in metadata.ManifestResources)
    {
        var item = metadata.GetManifestResource(handle);
        if (!item.Implementation.IsNil)
            continue;
        var name = metadata.GetString(item.Name);
        if (!name.StartsWith("ReactStudio/", StringComparison.Ordinal)
            && !name.StartsWith("ReactStudio.Module/", StringComparison.Ordinal)
            && !name.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
            continue;
        var blob = reader.GetSectionData(checked(resources.RelativeVirtualAddress + (int)item.Offset)).GetReader();
        var data = blob.ReadBytes(blob.ReadInt32());
        entries.Add(name, Convert.ToHexStringLower(SHA256.HashData(data)));
    }
    result.Add(entry.FullName, entries);
}
Console.WriteLine(JsonSerializer.Serialize(result, typeof(Dictionary<string, Dictionary<string, string>>), ResourceJson.Default));

[JsonSerializable(typeof(Dictionary<string, Dictionary<string, string>>))]
internal partial class ResourceJson : JsonSerializerContext;
