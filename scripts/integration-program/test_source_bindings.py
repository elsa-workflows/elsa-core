import json
import subprocess
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from source_bindings import expected_bindings, prepare, source_bound_project, verify_overlay


INVENTORY = Path(__file__).resolve().parents[2] / "doc/integration-program/inventory/inventory.json"


class SourceBindingTests(unittest.TestCase):
    def test_inventory_pins_select_only_reviewed_core_owned_edges(self):
        inventory = json.loads(INVENTORY.read_text(encoding="utf-8"))
        bindings = expected_bindings(inventory)
        self.assertEqual(len(bindings), 17)
        self.assertEqual(len({package for entries in bindings.values() for package in entries}), 26)
        self.assertEqual(
            bindings["test/modules/diagnostics/Elsa.Logging.Core.IntegrationTests/Elsa.Logging.Core.IntegrationTests.csproj"],
            {
                "Elsa.Testing.Shared": "src/common/Elsa.Testing.Shared/Elsa.Testing.Shared.csproj",
                "Elsa.Testing.Shared.Integration": "src/common/Elsa.Testing.Shared.Integration/Elsa.Testing.Shared.Integration.csproj",
                "Elsa.Workflows.Core": "src/modules/Elsa.Workflows.Core/Elsa.Workflows.Core.csproj",
            },
        )
        duplicate = json.loads(INVENTORY.read_text(encoding="utf-8"))
        next(project for project in duplicate["project_inventory"]["elsa-core"]
             if project["path"] == "src/modules/Elsa/Elsa.csproj")["package_id"] = "Elsa.Workflows.Core"
        with self.assertRaisesRegex(ValueError, "ambiguous owners"):
            expected_bindings(duplicate)

    def test_transformation_preserves_conditional_group_and_external_package(self):
        original = (b'<ItemGroup Condition="\'$(UseProjectReferences)\' == \'true\'">\n'
                    b'  <PackageReference Include="Elsa.Workflows.Core" />\n'
                    b'  <PackageReference Include="ThirdParty" />\n</ItemGroup>\n')
        result = source_bound_project(original, {"Elsa.Workflows.Core": "../../core/Workflow.csproj"})
        self.assertIn(b'<ProjectReference Include="../../core/Workflow.csproj" />', result)
        self.assertIn(b'<PackageReference Include="ThirdParty" />', result)
        self.assertIn(b'Condition="\'$(UseProjectReferences)\' == \'true\'"', result)
        with self.assertRaisesRegex(ValueError, "missing"):
            source_bound_project(original, {"Missing": "../../core/Missing.csproj"})
        with self.assertRaisesRegex(ValueError, "missing"):
            source_bound_project(b'<PackageReference Include="Elsa.Core" Version="1.0" />',
                                 {"Elsa.Core": "../../core/Elsa.csproj"})
        with self.assertRaisesRegex(ValueError, "Duplicate"):
            source_bound_project(b'<PackageReference Include="Elsa" /><PackageReference Include="Elsa" />',
                                 {"Elsa": "../../core/Elsa.csproj"})

    def test_disposable_overlay_keeps_pins_clean_and_detects_tampering(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            core, pristine, overlay = (root / name for name in ("elsa-core", "elsa-extensions", "source-bound-extensions"))
            for path in (core, pristine):
                path.mkdir()
                subprocess.run(["git", "-C", str(path), "init", "-q"], check=True)
                subprocess.run(["git", "-C", str(path), "config", "user.email", "test@example.invalid"], check=True)
                subprocess.run(["git", "-C", str(path), "config", "user.name", "Test"], check=True)
            owner = core / "src/Core.csproj"
            owner.parent.mkdir(parents=True)
            owner.write_text('<Project Sdk="Microsoft.NET.Sdk" />\n')
            consumer = pristine / "src/Consumer.csproj"
            consumer.parent.mkdir(parents=True)
            consumer.write_text(
                '<Project Sdk="Microsoft.NET.Sdk"><ItemGroup><PackageReference Include="Elsa.Core" />'
                '<ProjectReference Include="../cshells/src/CShells.Abstractions/CShells.Abstractions.csproj" />'
                '</ItemGroup></Project>\n')
            (pristine / "Directory.Packages.props").write_text(
                '<Project><ItemGroup><PackageVersion Include="CShells.Abstractions" Version="0.0.28" /></ItemGroup></Project>\n')
            for path in (core, pristine):
                subprocess.run(["git", "-C", str(path), "add", "."], check=True)
                subprocess.run(["git", "-C", str(path), "commit", "-qm", "fixture"], check=True)
            def head(path):
                return subprocess.check_output(["git", "-C", str(path), "rev-parse", "HEAD"], text=True).strip()
            inventory = {"repositories": {
                "elsa-core": {"commit": head(core)}, "elsa-extensions": {"commit": head(pristine)},
            }}
            with patch("source_bindings.expected_bindings", return_value={"src/Consumer.csproj": {"Elsa.Core": "src/Core.csproj"}}), \
                 patch("source_bindings.CSHELLS_CONSUMERS", {"src/Consumer.csproj"}):
                receipt = prepare(inventory, core, pristine, overlay)
                verify_overlay(inventory, core, pristine, overlay, receipt)
                self.assertEqual(len(receipt["files"]), 1)
                self.assertEqual(receipt["files"][0]["external_package_boundary"],
                                 {"package_id": "CShells.Abstractions", "version": "0.0.28"})
                self.assertIn('PackageReference Include="CShells.Abstractions"',
                              (overlay / "src/Consumer.csproj").read_text())
                self.assertFalse(subprocess.check_output(["git", "-C", str(pristine), "status", "--porcelain"]))
                (overlay / "src/Consumer.csproj").write_text("tampered")
                with self.assertRaisesRegex(ValueError, "changed since receipt"):
                    verify_overlay(inventory, core, pristine, overlay, receipt)
                subprocess.run(["git", "-C", str(pristine), "worktree", "remove", "--force", str(overlay)], check=True)
                with patch("source_bindings.expected_files", side_effect=ValueError("invalid mapping")):
                    with self.assertRaisesRegex(ValueError, "invalid mapping"):
                        prepare(inventory, core, pristine, overlay)
                self.assertFalse(overlay.exists())
                self.assertNotIn(str(overlay), subprocess.check_output(
                    ["git", "-C", str(pristine), "worktree", "list"], text=True))
                self.assertFalse(subprocess.check_output(["git", "-C", str(pristine), "status", "--porcelain"]))


if __name__ == "__main__":
    unittest.main()
