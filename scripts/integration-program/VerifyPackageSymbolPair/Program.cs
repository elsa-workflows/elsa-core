using System.IO.Compression;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;

if (args.Length != 2 && (args.Length != 3 || args[2] != "--inspect-documents"))
{
    throw new ArgumentException("Usage: VerifyPackageSymbolPair <assembly.dll> <symbols.pdb> [--inspect-documents]");
}

var assemblyPath = Path.GetFullPath(args[0]);
var symbolsPath = Path.GetFullPath(args[1]);
if (!File.Exists(assemblyPath) || !File.Exists(symbolsPath)
    || Path.GetFileNameWithoutExtension(assemblyPath) != Path.GetFileNameWithoutExtension(symbolsPath))
{
    throw new ArgumentException("An assembly and its same-named external PDB must exist.");
}

using var assembly = File.OpenRead(assemblyPath);
using var reader = new PEReader(assembly);
var openedExternalFile = false;
var matched = reader.TryOpenAssociatedPortablePdb(
    assemblyPath,
    candidate =>
    {
        if (Path.GetFileName(candidate) != Path.GetFileName(symbolsPath))
        {
            return null;
        }

        openedExternalFile = true;
        return File.OpenRead(symbolsPath);
    },
    out var pdbReaderProvider,
    out var foundPath);

if (!matched || pdbReaderProvider is null || !openedExternalFile || foundPath is null)
{
    throw new InvalidDataException("The external Portable PDB does not match the packaged assembly.");
}

using (pdbReaderProvider)
{
    var metadata = pdbReaderProvider.GetMetadataReader();
    if (args.Length == 3)
    {
        var sourceLinkKind = new Guid("cc110556-a091-4d38-9fec-25ab9a351a6a");
        var embeddedSourceKind = new Guid("0e8a571b-6926-466e-b4ad-8ab04611f5fe");
        var sha256Kind = new Guid("8829d00f-11b8-4213-878b-770e8597ac16");
        var sha1Kind = new Guid("ff1816ec-aa5e-4d10-87f7-6f4963833460");
        var sourceLinks = metadata.CustomDebugInformation.Select(metadata.GetCustomDebugInformation)
            .Where(info => metadata.GetGuid(info.Kind) == sourceLinkKind)
            .Select(info => JsonSerializer.Deserialize<JsonElement>(metadata.GetBlobBytes(info.Value))).ToArray();
        if (sourceLinks.Length != 1)
        {
            throw new InvalidDataException("A Portable PDB must contain exactly one SourceLink map.");
        }

        var documents = metadata.Documents.Select(handle =>
        {
            var document = metadata.GetDocument(handle);
            var algorithmId = metadata.GetGuid(document.HashAlgorithm);
            var algorithm = algorithmId == sha256Kind ? "sha256" : algorithmId == sha1Kind ? "sha1" : null;
            if (algorithm is null)
            {
                throw new InvalidDataException("Unsupported Portable PDB document checksum algorithm.");
            }

            var embedded = metadata.GetCustomDebugInformation(handle).Select(metadata.GetCustomDebugInformation)
                .Where(info => metadata.GetGuid(info.Kind) == embeddedSourceKind).ToArray();
            if (embedded.Length > 1)
            {
                throw new InvalidDataException("Duplicate embedded source for a Portable PDB document.");
            }

            string? embeddedChecksum = null;
            if (embedded.Length == 1)
            {
                var blob = metadata.GetBlobReader(embedded[0].Value);
                var size = blob.ReadInt32();
                if (size < 0)
                {
                    throw new InvalidDataException("Negative embedded source size.");
                }

                var payload = blob.ReadBytes(blob.RemainingBytes);
                byte[] source;
                if (size == 0)
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
                    if (source.Length != size)
                    {
                        throw new InvalidDataException("Embedded source length does not match its header.");
                    }
                }

                embeddedChecksum = Convert.ToHexString(algorithm == "sha256" ? SHA256.HashData(source) : SHA1.HashData(source)).ToLowerInvariant();
            }

            return new
            {
                path = metadata.GetString(document.Name).Replace('\\', '/'),
                algorithm,
                checksum = Convert.ToHexString(metadata.GetBlobBytes(document.Hash)).ToLowerInvariant(),
                embedded_checksum = embeddedChecksum
            };
        }).ToArray();
        var assemblyMetadata = reader.GetMetadataReader();
        var definition = assemblyMetadata.GetAssemblyDefinition();
        string? informationalVersion = null;
        foreach (var handle in definition.GetCustomAttributes())
        {
            var attribute = assemblyMetadata.GetCustomAttribute(handle);
            if (attribute.Constructor.Kind != HandleKind.MemberReference)
            {
                continue;
            }

            var constructor = assemblyMetadata.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
            if (constructor.Parent.Kind != HandleKind.TypeReference)
            {
                continue;
            }

            var type = assemblyMetadata.GetTypeReference((TypeReferenceHandle)constructor.Parent);
            if (assemblyMetadata.GetString(type.Namespace) != "System.Reflection"
                || assemblyMetadata.GetString(type.Name) != "AssemblyInformationalVersionAttribute")
            {
                continue;
            }

            var value = assemblyMetadata.GetBlobReader(attribute.Value);
            if (value.ReadUInt16() != 1 || informationalVersion is not null)
            {
                throw new InvalidDataException("Malformed or duplicate assembly informational version.");
            }

            informationalVersion = value.ReadSerializedString();
        }

        Console.WriteLine(JsonSerializer.Serialize(new
        {
            assembly_name = assemblyMetadata.GetString(definition.Name),
            assembly_version = definition.Version.ToString(),
            informational_version = informationalVersion,
            source_link = sourceLinks[0],
            documents
        }));
    }
    else
    {
        Console.WriteLine("Verified matching external Portable PDB for packaged assembly.");
    }
}
