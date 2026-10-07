"""Hosted regression coverage for the real compiler-produced Portable PDB path.

Set ELSA_SYMBOL_INSPECTOR_DLL to the compiled helper. Without that explicit
opt-in, discovery skips SDK work so the cheap local contracts stay lightweight.
"""
import hashlib
import json
import os
from pathlib import Path
import struct
import subprocess
import tempfile
import unittest
import uuid


@unittest.skipUnless(os.environ.get("ELSA_SYMBOL_INSPECTOR_DLL"), "requires the compiled symbol helper")
class RealSymbolInspectorTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.helper = Path(os.environ["ELSA_SYMBOL_INSPECTOR_DLL"]).resolve(strict=True)
        temporary = tempfile.TemporaryDirectory(prefix="elsa-symbol-fixture-")
        cls.addClassCleanup(temporary.cleanup)
        cls.root = Path(temporary.name).resolve()
        cls.source = b"public static class Fixture { public static int Increment(int value) => value + 1; }\n"
        (cls.root / "Fixture.cs").write_bytes(cls.source)
        (cls.root / "sourcelink.json").write_text(json.dumps({
            "documents": {"/_/*": "https://example.invalid/fixture/*"}}))
        project = cls.root / "Fixture.csproj"
        project.write_text("""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <DebugType>portable</DebugType>
    <Deterministic>true</Deterministic>
    <ChecksumAlgorithm>SHA256</ChecksumAlgorithm>
    <EmbedAllSources>true</EmbedAllSources>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <GenerateTargetFrameworkAttribute>false</GenerateTargetFrameworkAttribute>
    <EnableSourceControlManagerQueries>false</EnableSourceControlManagerQueries>
    <EnableSourceLink>false</EnableSourceLink>
    <SourceLink>$(MSBuildProjectDirectory)/sourcelink.json</SourceLink>
    <PathMap>$(MSBuildProjectDirectory)=/_</PathMap>
  </PropertyGroup>
</Project>
""")
        cls.environment = {key: value for key, value in os.environ.items()
                           if key in {"PATH", "DOTNET_ROOT", "HOME", "TMPDIR", "DOTNET_CLI_TELEMETRY_OPTOUT"}}
        subprocess.run(["dotnet", "build", str(project), "--configuration", "Release",
                        "--output", str(cls.root / "output"), "--nologo"], cwd=cls.root,
                       env=cls.environment, check=True, capture_output=True, timeout=120)
        cls.assembly = cls.root / "output/Fixture.dll"
        cls.symbols = cls.root / "output/Fixture.pdb"
        cls.pdb = cls.symbols.read_bytes()
        # Read the compiler's metadata ID as an independent field oracle.
        version_size = struct.unpack_from("<I", cls.pdb, 12)[0]
        position = (16 + version_size + 3) & ~3
        stream_count = struct.unpack_from("<H", cls.pdb, position + 2)[0]
        position += 4
        offsets = []
        for _ in range(stream_count):
            offset, _ = struct.unpack_from("<II", cls.pdb, position)
            start = position + 8
            end = cls.pdb.index(b"\0", start)
            if cls.pdb[start:end] == b"#Pdb":
                offsets.append(offset)
            position = (end + 4) & ~3
        if len(offsets) != 1:
            raise AssertionError("Compiler fixture must contain one Portable PDB ID stream")
        cls.id_offset = offsets[0]

    def inspect(self, mode="--inspect-symbols", symbols=None):
        return subprocess.run(["dotnet", str(self.helper), str(self.assembly), str(symbols or self.symbols),
                               *([mode] if mode else [])], cwd=self.root, env=self.environment,
                              capture_output=True, text=True, timeout=30)

    def test_real_pair_reports_compiler_identity_checksum_key_and_source_documents(self):
        result = self.inspect()
        self.assertEqual(result.returncode, 0, result.stderr)
        value = json.loads(result.stdout)
        self.assertEqual(value["schema"], 1)
        symbol = value["symbol"]
        guid = uuid.UUID(bytes_le=self.pdb[self.id_offset:self.id_offset + 16])
        stamp = struct.unpack_from("<I", self.pdb, self.id_offset + 16)[0]
        normalized = bytearray(self.pdb)
        normalized[self.id_offset:self.id_offset + 20] = bytes(20)
        checksum = hashlib.sha256(normalized).hexdigest()
        self.assertEqual(symbol, {
            "key": f"fixture.pdb/{guid.hex}FFFFFFFF/fixture.pdb", "pdb_name": "fixture.pdb",
            "guid": str(guid), "stamp": stamp, "checksum_algorithm": "SHA256",
            "declared_checksum": checksum, "normalized_checksum": checksum,
            "pdb_sha256": hashlib.sha256(self.pdb).hexdigest(), "pdb_size": len(self.pdb)})
        self.assertIn(bytes.fromhex(checksum), self.assembly.read_bytes())
        self.assertNotEqual(symbol["pdb_sha256"], checksum)
        details = value["details"]
        self.assertEqual(details["source_link"], {"documents": {"/_/*": "https://example.invalid/fixture/*"}})
        document = next(row for row in details["documents"] if row["path"] == "/_/Fixture.cs")
        self.assertEqual(document["checksum"], hashlib.sha256(self.source).hexdigest())
        self.assertEqual(document["embedded_checksum"], document["checksum"])
        legacy = self.inspect("--inspect-documents")
        self.assertEqual(legacy.returncode, 0, legacy.stderr)
        self.assertEqual(json.loads(legacy.stdout), details)
        self.assertEqual(self.inspect(mode=None).returncode, 0)

    def test_altered_pdb_is_rejected_by_real_checksum_verification(self):
        directory = self.root / "altered"
        directory.mkdir()
        changed = directory / "Fixture.pdb"
        changed.write_bytes(self.pdb + b"altered-after-compilation")
        result = self.inspect(symbols=changed)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("Portable PDB checksum differs from the assembly", result.stderr)
        self.assertEqual(result.stdout, "")

    def test_different_pdb_identity_is_rejected(self):
        directory = self.root / "wrong-identity"
        directory.mkdir()
        changed = directory / "Fixture.pdb"
        data = bytearray(self.pdb)
        data[self.id_offset] ^= 1
        changed.write_bytes(data)
        self.assertNotEqual(self.inspect(symbols=changed).returncode, 0)


if __name__ == "__main__":
    unittest.main()
