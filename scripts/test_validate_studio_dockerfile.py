import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import validate_studio_dockerfile as validator

ROOT = Path(__file__).resolve().parents[1]
DOCKERFILE = ROOT / "docker/ElsaStudio.Dockerfile"
SERVER_DOCKERFILE = ROOT / "docker/ElsaServer.Dockerfile"
COMBINED_DOCKERFILE = ROOT / "docker/ElsaServerAndStudio.Dockerfile"


class StudioDockerfileGuardTests(unittest.TestCase):
    def test_checked_in_studio_dockerfile_is_the_studio_host(self):
        text = DOCKERFILE.read_text(encoding="utf-8")
        self.assertEqual([], validator.dockerfile_errors(text))
        self.assertIn(validator.STUDIO_PROJECT, text)
        self.assertIn(validator.STUDIO_DLL, text)

    def test_server_and_combined_dockerfiles_fail_the_studio_guard(self):
        for path in (SERVER_DOCKERFILE, COMBINED_DOCKERFILE):
            with self.subTest(path=path.name):
                errors = validator.dockerfile_errors(path.read_text(encoding="utf-8"))
                self.assertTrue(errors)
                self.assertTrue(any("must not mention" in error for error in errors))

    def test_cmd_or_publish_target_pointing_at_the_server_fails(self):
        good = DOCKERFILE.read_text(encoding="utf-8")
        mutated_cmd = good.replace(
            'CMD ["dotnet", "Elsa.Studio.Host.Server.dll"]',
            'CMD ["dotnet", "Elsa.ModularServer.Web.dll"]',
        )
        errors = validator.dockerfile_errors(mutated_cmd)
        self.assertTrue(any("Elsa.ModularServer.Web.dll" in error for error in errors))
        mutated_project = good.replace(validator.STUDIO_PROJECT, "core/src/apps/Elsa.Server.Web/Elsa.Server.Web.csproj")
        mutated_project = mutated_project.replace("Elsa.Studio.Host.Server.csproj", "Elsa.Server.Web.csproj")
        errors = validator.dockerfile_errors(mutated_project)
        self.assertTrue(any(validator.STUDIO_PROJECT in error for error in errors))

    def test_script_accepts_the_checked_in_dockerfile(self):
        import subprocess
        import sys

        result = subprocess.run(
            [sys.executable, str(ROOT / "scripts/validate_studio_dockerfile.py"), str(DOCKERFILE)],
            capture_output=True,
            text=True,
        )
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertIn(validator.STUDIO_DLL, result.stdout)

    def test_docker_ca_runs_the_studio_dockerfile_guard(self):
        workflow = (ROOT / ".github/workflows/docker-ca.yml").read_text(encoding="utf-8")
        self.assertIn("python3 -B scripts/validate_studio_dockerfile.py", workflow)
        self.assertIn("test_validate_studio_dockerfile.py", workflow)
        self.assertIn("needs: validate-studio-dockerfile", workflow)
        self.assertIn('["dotnet","Elsa.Studio.Host.Server.dll"]', workflow)
        self.assertLess(
            workflow.find("validate-studio-dockerfile:"),
            workflow.find("docker build -f ${{ matrix.dockerfile }}"),
        )

    def test_script_rejects_a_server_dockerfile(self):
        import subprocess
        import sys

        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / "Dockerfile"
            path.write_text(SERVER_DOCKERFILE.read_text(encoding="utf-8"), encoding="utf-8")
            result = subprocess.run(
                [sys.executable, str(ROOT / "scripts/validate_studio_dockerfile.py"), str(path)],
                capture_output=True,
                text=True,
            )
        self.assertEqual(1, result.returncode)
        self.assertTrue(result.stderr)


if __name__ == "__main__":
    unittest.main()
