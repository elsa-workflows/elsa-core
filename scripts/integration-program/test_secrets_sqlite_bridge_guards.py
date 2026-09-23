"""Tests for the source and target safety guards in the synthetic bridge runner."""
import subprocess
import tempfile
from pathlib import Path
import sys
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).parent))
try:
    import run_secrets_sqlite_bridge_contract as bridge_runner
finally:
    sys.path.pop(0)


class SecretsSqliteBridgeGuardTests(unittest.TestCase):
    @staticmethod
    def create_pinned_repo(root):
        for relative in (
            "src/Secrets.cs",
            "Directory.Build.props",
            "Directory.Build.targets",
            "Directory.Packages.props",
            "NuGet.Config",
            "nuget.config",
        ):
            path = root / relative
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("pinned", encoding="utf-8")

        subprocess.run(["git", "init", "-q"], cwd=root, check=True)
        subprocess.run(["git", "config", "user.name", "Bridge Guard Test"], cwd=root, check=True)
        subprocess.run(["git", "config", "user.email", "bridge-guard@example.invalid"], cwd=root, check=True)
        subprocess.run(["git", "add", "."], cwd=root, check=True)
        subprocess.run(
            ["git", "-c", "commit.gpgsign=false", "commit", "-m", "pinned Core build inputs"],
            cwd=root,
            check=True,
            capture_output=True,
        )
        return subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=root, text=True).strip()

    def test_current_core_pin_covers_build_inputs_and_untracked_source(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            root = Path(temporary_directory)
            pin = self.create_pinned_repo(root)

            with patch.object(bridge_runner, "ROOT", root), patch.object(
                bridge_runner, "PINNED_TARGET_CORE_SOURCE_COMMIT", pin
            ):
                bridge_runner.verify_current_core_source_pin()

                for relative in (
                    "src/Secrets.cs",
                    "Directory.Build.props",
                    "Directory.Build.targets",
                    "Directory.Packages.props",
                    "NuGet.Config",
                    "nuget.config",
                ):
                    with self.subTest(changed_input=relative):
                        path = root / relative
                        path.write_text("changed", encoding="utf-8")
                        with self.assertRaisesRegex(ValueError, "source/build inputs differ"):
                            bridge_runner.verify_current_core_source_pin()
                        path.write_text("pinned", encoding="utf-8")

                (root / "global.json").write_text('{"sdk":{"version":"0.0.1"}}', encoding="utf-8")
                with self.assertRaisesRegex(ValueError, "Untracked Core source/build inputs"):
                    bridge_runner.verify_current_core_source_pin()
                (root / "global.json").unlink()

                (root / "src/Untracked.cs").write_text("untracked source", encoding="utf-8")
                with self.assertRaisesRegex(ValueError, "Untracked Core source/build inputs"):
                    bridge_runner.verify_current_core_source_pin()

    def test_pinned_checkout_ignores_future_source_drift_in_tools_checkout(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            temp_root = Path(temporary_directory)
            root = temp_root / "repo"
            root.mkdir()
            pin = self.create_pinned_repo(root)
            (root / "src/Secrets.cs").write_text("future working-tree source", encoding="utf-8")

            runner_source = root / "scripts/integration-program/secrets-sqlite-bridge-contract/current-core"
            runner_source.mkdir(parents=True)
            (runner_source / "CurrentCoreBridgeRunner.csproj").write_text("<Project />", encoding="utf-8")
            (runner_source / "Program.cs").write_text("synthetic runner", encoding="utf-8")
            (runner_source / "obj").mkdir()
            (runner_source / "obj/project.assets.json").write_text("stale generated state", encoding="utf-8")
            destination = temp_root / "pinned-core"

            with patch.object(bridge_runner, "ROOT", root), patch.object(
                bridge_runner, "CURRENT_PROJECT", runner_source / "CurrentCoreBridgeRunner.csproj"
            ), patch.object(bridge_runner, "PINNED_TARGET_CORE_SOURCE_COMMIT", pin):
                isolated_project = bridge_runner.prepare_current_core_checkout(destination)

            self.assertEqual(isolated_project, destination / Path("scripts/integration-program/secrets-sqlite-bridge-contract/current-core/CurrentCoreBridgeRunner.csproj"))
            self.assertEqual(
                subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=destination, text=True).strip(), pin
            )
            self.assertEqual((destination / "src/Secrets.cs").read_text(encoding="utf-8"), "pinned")
            self.assertEqual((isolated_project.parent / "Program.cs").read_text(encoding="utf-8"), "synthetic runner")
            self.assertFalse((isolated_project.parent / "obj").exists())


if __name__ == "__main__":
    unittest.main()
