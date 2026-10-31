"""Check the evaluated MSBuild release boundary without building the solution."""

import json
from pathlib import Path
import subprocess
import unittest


ROOT = Path(__file__).resolve().parents[1]


class PackageVersionPolicyTests(unittest.TestCase):
    def test_existing_release_stream_excludes_react_and_preview_stream_includes_it(self):
        for project in ("Elsa.ReactStudio", "Elsa.ReactStudio.Host", "modules/Elsa.ReactStudio.Labels"):
            path = ROOT / "dotnet" / project / f"{Path(project).name}.csproj"
            for version, expected in (("3.10.0", "false"), ("3.10.12", "false"),
                                      ("3.10.0-proof.8859.1", "false"),
                                      ("3.11.0-preview.1", "true"), ("3.11.0", "true"), ("3.12.0", "true")):
                with self.subTest(project=project, version=version):
                    output = subprocess.check_output([
                        "dotnet", "msbuild", str(path), "-getProperty:IsPackable", f"-p:Version={version}"
                    ], text=True)
                    self.assertEqual(expected, output.strip())

    def test_package_version_override_cannot_sneak_into_an_older_release(self):
        project = ROOT / "dotnet/Elsa.ReactStudio/Elsa.ReactStudio.csproj"
        output = subprocess.check_output([
            "dotnet", "msbuild", str(project), "-getProperty:Version,PackageVersion,IsPackable",
            "-p:Version=3.11.0", "-p:PackageVersion=3.10.0"
        ], text=True)
        self.assertEqual("false", json.loads(output)["Properties"]["IsPackable"])

    def test_pack_rejects_different_package_and_assembly_versions(self):
        project = ROOT / "dotnet/Elsa.ReactStudio/Elsa.ReactStudio.csproj"
        result = subprocess.run([
            "dotnet", "msbuild", str(project), "-target:RequireReactStudioVersionConsistency",
            "-p:Version=3.11.0", "-p:PackageVersion=3.12.0"
        ], text=True, capture_output=True)
        self.assertNotEqual(0, result.returncode)
        self.assertIn("lockstep package and assembly versions", result.stdout + result.stderr)


if __name__ == "__main__":
    unittest.main()
