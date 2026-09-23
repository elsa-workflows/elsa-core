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
    def test_current_core_pin_covers_build_inputs_and_untracked_source(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            root = Path(temporary_directory)
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
            pin = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=root, text=True).strip()

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


if __name__ == "__main__":
    unittest.main()
