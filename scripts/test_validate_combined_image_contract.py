#!/usr/bin/env python3
import tempfile
import unittest
from pathlib import Path

from validate_combined_image_contract import validate_combined_image_contract


class CombinedImageContractTests(unittest.TestCase):
    def test_current_tree_satisfies_the_server_plus_studio_contract(self):
        errors = validate_combined_image_contract()
        self.assertEqual(errors, [])

    def test_modular_server_dockerfile_is_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            dockerfile = root / "docker" / "ElsaServerAndStudio.Dockerfile"
            host = root / "studio" / "src" / "hosts" / "Elsa.ServerAndStudio.Web"
            dockerfile.parent.mkdir(parents=True)
            host.mkdir(parents=True)
            (host / "Pages").mkdir()
            dockerfile.write_text(
                "RUN dotnet publish core/src/apps/Elsa.ModularServer.Web/Elsa.ModularServer.Web.csproj\n"
                'CMD ["dotnet", "Elsa.ModularServer.Web.dll"]\n',
                encoding="utf-8",
            )
            (host / "Elsa.ServerAndStudio.Web.csproj").write_text(
                '<Project Sdk="Microsoft.NET.Sdk.Web"></Project>\n',
                encoding="utf-8",
            )
            (host / "Program.cs").write_text("app.MapHealthChecks(\"/\");\n", encoding="utf-8")
            (host / "Pages" / "_Host.cshtml").write_text("<html></html>\n", encoding="utf-8")
            (host / "appsettings.json").write_text("{}\n", encoding="utf-8")
            errors = validate_combined_image_contract(root)
            self.assertTrue(any("Elsa.ModularServer.Web" in error for error in errors))
            self.assertTrue(any("missing required marker" in error for error in errors))


if __name__ == "__main__":
    unittest.main()
