#!/usr/bin/env python3
"""Restore and run a clean Elsa.Slack package consumer without publishing."""

from __future__ import annotations

import argparse
import base64
import hashlib
import json
import os
import subprocess
import tempfile
from pathlib import Path
from xml.sax.saxutils import quoteattr


FRAMEWORKS = ("net8.0", "net9.0", "net10.0")
NUGET_ORG = "https://api.nuget.org/v3/index.json"


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--artifacts", type=Path, required=True)
    parser.add_argument("--version", required=True)
    parser.add_argument("--elsa-version", required=True)
    parser.add_argument("--slacknet-version", required=True)
    args = parser.parse_args()

    if not args.version.startswith("0.0.0-proof."):
        parser.error("A non-release proof version is required")

    artifacts = args.artifacts.resolve(strict=True)
    package = artifacts / f"Elsa.Slack.{args.version}.nupkg"
    if not package.is_file():
        parser.error(f"Missing local proof package: {package}")

    with tempfile.TemporaryDirectory(prefix="elsa-slack-consumer-") as temporary:
        root = Path(temporary)
        packages = root / "packages"
        project = root / "SlackConsumer.csproj"
        project.write_text(
            f"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFrameworks>{';'.join(FRAMEWORKS)}</TargetFrameworks>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Elsa.Slack" Version={quoteattr(args.version)} />
  </ItemGroup>
</Project>
""",
            encoding="utf-8",
        )
        (root / "Program.cs").write_text(
            """using Elsa.Slack.Activities.Channels;
using Elsa.Slack.Features;

var featureAssembly = typeof(SlackFeature).Assembly;
var activityAssembly = typeof(CreateChannel).Assembly;
if (featureAssembly.GetName().Name != "Elsa.Slack" || featureAssembly != activityAssembly)
    throw new InvalidOperationException("Unexpected Slack feature or activity assembly.");
Console.WriteLine($"{System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}: {featureAssembly.GetName().Name}");
""",
            encoding="utf-8",
        )
        config = root / "NuGet.Config"
        config.write_text(
            f"""<configuration><packageSources><clear />
  <add key="local-proof" value={quoteattr(str(artifacts))} />
  <add key="nuget.org" value="{NUGET_ORG}" />
</packageSources></configuration>
""",
            encoding="utf-8",
        )

        environment = os.environ.copy()
        environment["NUGET_PACKAGES"] = str(packages)

        def run(*command: str) -> None:
            subprocess.run(command, cwd=root, env=environment, check=True)

        run("dotnet", "restore", str(project), "--configfile", str(config),
            "--packages", str(packages), "--force-evaluate", "--no-cache", "--nologo")

        selected = {
            f"Elsa.Slack/{args.version}",
            f"Elsa/{args.elsa_version}",
            f"SlackNet/{args.slacknet_version}",
        }
        assets = json.loads((root / "obj" / "project.assets.json").read_text(encoding="utf-8"))
        if set(assets["targets"]) != set(FRAMEWORKS):
            raise RuntimeError(f"Unexpected target frameworks: {sorted(assets['targets'])}")
        for framework, libraries in assets["targets"].items():
            if not selected.issubset(libraries):
                raise RuntimeError(f"{framework} did not resolve {sorted(selected)}")

        for package_id, version, source in (
            ("elsa.slack", args.version, str(artifacts)),
            ("elsa", args.elsa_version, NUGET_ORG),
            ("slacknet", args.slacknet_version, NUGET_ORG),
        ):
            metadata_path = packages / package_id / version / ".nupkg.metadata"
            metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
            if metadata.get("source") != source:
                raise RuntimeError(f"Unexpected {package_id} source: {metadata.get('source')}")

        cached_package = packages / "elsa.slack" / args.version / package.name.lower()
        cached_hash = cached_package.with_suffix(cached_package.suffix + ".sha512")
        proof_bytes = package.read_bytes()
        if cached_package.read_bytes() != proof_bytes:
            raise RuntimeError("The consumer did not cache the exact proof package")
        expected_hash = base64.b64encode(hashlib.sha512(proof_bytes).digest()).decode("ascii")
        if cached_hash.read_text(encoding="utf-8").strip() != expected_hash:
            raise RuntimeError("The consumer package cache has the wrong NuGet SHA-512")

        run("dotnet", "build", str(project), "--no-restore", "--nologo", "-v", "minimal")
        for framework in FRAMEWORKS:
            run("dotnet", "run", "--project", str(project), "--no-build", "--framework", framework)

    print(f"Clean Elsa.Slack {args.version} consumer passed on {', '.join(FRAMEWORKS)}; "
          f"package SHA-256 {hashlib.sha256(proof_bytes).hexdigest()}")


if __name__ == "__main__":
    main()
