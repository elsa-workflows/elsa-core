using System.Reflection.PortableExecutable;

if (args.Length != 2)
{
    throw new ArgumentException("Usage: VerifyPackageSymbolPair <assembly.dll> <symbols.pdb>");
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
    pdbReaderProvider.GetMetadataReader();
}
Console.WriteLine("Verified matching external Portable PDB for packaged assembly.");
