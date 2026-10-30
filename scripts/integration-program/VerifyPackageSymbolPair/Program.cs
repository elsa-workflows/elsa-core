using System.IO.Compression;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using NuGet.Packaging;
using NuGet.Packaging.Signing;

if (args.Length == 2 && args[0] == "--inspect-archive")
{
    using var package = new PackageArchiveReader(args[1]);
    try
    {
        var signature = await package.GetPrimarySignatureAsync(CancellationToken.None);
        if (signature is not null)
        {
            await package.ValidateIntegrityAsync(signature.SignatureContent, CancellationToken.None);
        }

        Console.WriteLine(JsonSerializer.Serialize(new
        {
            signed = signature is not null,
            content_hash = package.GetContentHash(CancellationToken.None),
            archive_sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[1]))).ToLowerInvariant()
        }));
    }
    catch (SignatureException error)
    {
        Console.Error.WriteLine($"{error.Code}: {error.Message}");
        Environment.ExitCode = 1;
    }
    return;
}

if (args.Length != 2 && (args.Length != 3 || args[2] != "--inspect-documents" && args[2] != "--inspect-symbols"))
{
    throw new ArgumentException("Usage: VerifyPackageSymbolPair <assembly.dll> <symbols.pdb> [--inspect-documents|--inspect-symbols] or --inspect-archive <package.nupkg>");
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
        var nonmoduleTypes = assemblyMetadata.TypeDefinitions.Select(assemblyMetadata.GetTypeDefinition)
            .Count(type => assemblyMetadata.GetString(type.Name) != "<Module>");
        var methods = assemblyMetadata.MethodDefinitions.Select(assemblyMetadata.GetMethodDefinition).ToArray();
        var executableMethodBodies = 0;
        foreach (var method in methods.Where(method => method.RelativeVirtualAddress != 0))
        {
            if (reader.GetMethodBody(method.RelativeVirtualAddress).GetILBytes() is null)
            {
                throw new InvalidDataException("A declared method body could not be decoded.");
            }
            executableMethodBodies++;
        }
        var nonabstractMethodsWithoutBody = methods.Count(method => method.RelativeVirtualAddress == 0
            && (method.Attributes & System.Reflection.MethodAttributes.Abstract) == 0);
        var nativeOrExternalMethods = methods.Count(method =>
            (method.Attributes & System.Reflection.MethodAttributes.PinvokeImpl) != 0
            || (method.ImplAttributes & System.Reflection.MethodImplAttributes.CodeTypeMask) != System.Reflection.MethodImplAttributes.IL
            || (method.ImplAttributes & (System.Reflection.MethodImplAttributes.InternalCall | System.Reflection.MethodImplAttributes.ForwardRef)) != 0);
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

        var inspection = new
        {
            assembly_name = assemblyMetadata.GetString(definition.Name),
            assembly_version = definition.Version.ToString(),
            informational_version = informationalVersion,
            executable_method_bodies = executableMethodBodies,
            nonabstract_methods_without_body = nonabstractMethodsWithoutBody,
            native_or_external_methods = nativeOrExternalMethods,
            nonmodule_types = nonmoduleTypes,
            reference_assembly = IsReferenceAssembly(assemblyMetadata, definition),
            source_link = sourceLinks[0],
            documents
        };
        Console.WriteLine(args[2] == "--inspect-symbols"
            ? JsonSerializer.Serialize(new { schema = 1, symbol = InspectSymbols(reader, metadata, symbolsPath), details = inspection })
            : JsonSerializer.Serialize(inspection));
    }
    else
    {
        Console.WriteLine("Verified matching external Portable PDB for packaged assembly.");
    }
}


// Unknown attribute type shapes remain unknown; they cannot admit a bodyless
// implementation artifact through the no-documents policy.
static bool? IsReferenceAssembly(MetadataReader metadata, AssemblyDefinition definition)
{
    var unknown = false;
    foreach (var handle in definition.GetCustomAttributes())
    {
        var attribute = metadata.GetCustomAttribute(handle);
        EntityHandle typeHandle = attribute.Constructor.Kind switch
        {
            HandleKind.MemberReference => metadata.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent,
            HandleKind.MethodDefinition => metadata.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType(),
            _ => default
        };
        (StringHandle Namespace, StringHandle Name)? identity = typeHandle.Kind switch
        {
            HandleKind.TypeReference => (metadata.GetTypeReference((TypeReferenceHandle)typeHandle).Namespace,
                                        metadata.GetTypeReference((TypeReferenceHandle)typeHandle).Name),
            HandleKind.TypeDefinition => (metadata.GetTypeDefinition((TypeDefinitionHandle)typeHandle).Namespace,
                                         metadata.GetTypeDefinition((TypeDefinitionHandle)typeHandle).Name),
            _ => null
        };
        if (identity is not { } type)
        {
            unknown = true;
            continue;
        }
        if (metadata.GetString(type.Namespace) == "System.Runtime.CompilerServices" &&
            metadata.GetString(type.Name) == "ReferenceAssemblyAttribute")
        {
            return true;
        }
    }
    return unknown ? null : false;
}


// The Portable PDB key and checksum follow dotnet/symstore and the PE-COFF
// Portable PDB checksum specification. The checksum zeroes the 20-byte #Pdb ID;
// it is deliberately distinct from the unchanged archive member's raw hash.
static object InspectSymbols(PEReader reader, MetadataReader metadata, string symbolsPath)
{
    var entries = reader.ReadDebugDirectory();
    var codeViews = entries.Where(entry => entry.Type == DebugDirectoryEntryType.CodeView).ToArray();
    var checksums = entries.Where(entry => entry.Type == DebugDirectoryEntryType.PdbChecksum).ToArray();
    if (codeViews.Length != 1 || !codeViews[0].IsPortableCodeView || checksums.Length != 1)
    {
        throw new InvalidDataException("Exactly one Portable CodeView and PDB checksum are required.");
    }

    var codeView = reader.ReadCodeViewDebugDirectoryData(codeViews[0]);
    var checksum = reader.ReadPdbChecksumDebugDirectoryData(checksums[0]);
    var id = metadata.DebugMetadataHeader?.Id.ToArray();
    if (id is null || id.Length != 20 || codeView.Age != 1)
    {
        throw new InvalidDataException("Invalid Portable PDB identity.");
    }

    var guid = new Guid(id.AsSpan(0, 16));
    var stamp = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(id.AsSpan(16, 4));
    if (guid != codeView.Guid || stamp != codeViews[0].Stamp)
    {
        throw new InvalidDataException("Portable PDB GUID/stamp differs from the assembly.");
    }

    var file = new FileInfo(symbolsPath);
    if (file.Length <= 0 || file.Length > 32 * 1024 * 1024)
    {
        throw new InvalidDataException("Portable PDB exceeds the inspection bound.");
    }

    var bytes = File.ReadAllBytes(symbolsPath);
    var originalHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    using var stream = new MemoryStream(bytes, writable: false);
    using var binary = new BinaryReader(stream);
    if (binary.ReadUInt32() != 0x424a5342)
    {
        throw new InvalidDataException("Portable PDB metadata signature is invalid.");
    }

    stream.Position = 12;
    var versionLength = binary.ReadUInt32();
    if (versionLength > bytes.Length - 16)
    {
        throw new InvalidDataException("Portable PDB version header is invalid.");
    }

    stream.Position = (16L + versionLength + 3) & ~3L;
    binary.ReadUInt16();
    var count = binary.ReadUInt16();
    if (count == 0 || count > 64)
    {
        throw new InvalidDataException("Portable PDB stream count is invalid.");
    }

    int? idOffset = null;
    for (var index = 0; index < count; index++)
    {
        var offset = binary.ReadUInt32();
        var size = binary.ReadUInt32();
        var name = new List<byte>();
        byte next;
        while ((next = binary.ReadByte()) != 0)
        {
            if (name.Count >= 32)
            {
                throw new InvalidDataException("Portable PDB stream name is invalid.");
            }
            name.Add(next);
        }
        stream.Position = (stream.Position + 3) & ~3L;
        if (offset > bytes.Length || size > bytes.Length - offset)
        {
            throw new InvalidDataException("Portable PDB stream is outside the file.");
        }
        if (System.Text.Encoding.ASCII.GetString(name.ToArray()) == "#Pdb")
        {
            if (idOffset is not null || size < 20)
            {
                throw new InvalidDataException("Portable PDB identity stream is invalid.");
            }
            idOffset = checked((int)offset);
        }
    }

    if (idOffset is null || !bytes.AsSpan(idOffset.Value, 20).SequenceEqual(id))
    {
        throw new InvalidDataException("Portable PDB identity stream differs from metadata.");
    }

    Array.Clear(bytes, idOffset.Value, 20);
    var normalizedHash = checksum.AlgorithmName switch
    {
        "SHA256" => SHA256.HashData(bytes),
        "SHA1" => SHA1.HashData(bytes),
        _ => throw new InvalidDataException("Unsupported Portable PDB checksum algorithm.")
    };
    if (!normalizedHash.AsSpan().SequenceEqual(checksum.Checksum.AsSpan()))
    {
        throw new InvalidDataException("Portable PDB checksum differs from the assembly.");
    }

    var nameLower = Path.GetFileName(symbolsPath).ToLowerInvariant();
    return new
    {
        key = $"{nameLower}/{guid:N}FFFFFFFF/{nameLower}",
        pdb_name = nameLower,
        guid = guid.ToString("D"),
        stamp,
        checksum_algorithm = checksum.AlgorithmName,
        declared_checksum = Convert.ToHexString(checksum.Checksum.AsSpan()).ToLowerInvariant(),
        normalized_checksum = Convert.ToHexString(normalizedHash).ToLowerInvariant(),
        pdb_sha256 = originalHash,
        pdb_size = bytes.Length
    };
}
