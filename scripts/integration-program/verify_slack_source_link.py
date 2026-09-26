#!/usr/bin/env python3
"""Verify the remote source links in an unpublished Elsa.Slack symbol package."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import shutil
import subprocess
import tempfile
from pathlib import Path
from zipfile import ZipFile

from run_mapped_slack_package_proof import TFMS, verify_imported_source_link
from run_slack_package_proof import require_sourcelink_tool


ROOT = Path(__file__).resolve().parents[2]
PAIR_PROJECT = ROOT / "scripts/integration-program/VerifyPackageSymbolPair/VerifyPackageSymbolPair.csproj"


def verify(artifacts: Path, version: str, commit: str, sourcelink_tool: Path) -> dict[str, object]:
    manifest = json.loads((ROOT / "doc/integration-program/release-units.json").read_text(encoding="utf-8"))
    units = [unit for unit in manifest["release_units"] if unit["package_id"] == "Elsa.Slack"]
    if len(units) != 1:
        raise ValueError(f"Expected one Elsa.Slack release unit, got {len(units)}")
    proof_base = units[0]["versioning"]["local_proof_version"]
    if not re.fullmatch(rf"{re.escape(proof_base)}\.[0-9]+\.[0-9]+", version):
        raise ValueError(f"Expected a {proof_base}.<run-id>.<attempt> proof version")
    if not re.fullmatch(r"[0-9a-f]{40}", commit):
        raise ValueError("Expected a full lowercase source commit SHA")
    if not artifacts.is_dir() or artifacts.is_symlink():
        raise ValueError("Artifact directory must exist and must not be a symlink")

    package = artifacts / f"Elsa.Slack.{version}.nupkg"
    symbols = artifacts / f"Elsa.Slack.{version}.snupkg"
    actual = {path.name for path in artifacts.iterdir() if path.is_file()}
    if actual != {package.name, symbols.name}:
        raise ValueError(f"Expected only the Elsa.Slack package and symbols; got {sorted(actual)}")
    source_link_assembly, tool_version, tool_sha256 = require_sourcelink_tool(sourcelink_tool)
    dotnet = shutil.which("dotnet")
    if dotnet is None:
        raise ValueError("dotnet is required for SourceLink verification")

    with tempfile.TemporaryDirectory(prefix="elsa-slack-source-link-") as temporary:
        output = Path(temporary)
        (output / "pdb").mkdir()
        (output / "logs").mkdir()
        pair_build = output / "pair-verifier"
        subprocess.run(
            [dotnet, "build", str(PAIR_PROJECT), "--configuration", "Release", "--output", str(pair_build),
             "--nologo", "--verbosity", "quiet"],
            check=True,
            capture_output=True,
            text=True,
        )
        pair_verifier = pair_build / "VerifyPackageSymbolPair.dll"
        with ZipFile(package) as package_archive, ZipFile(symbols) as symbol_archive:
            for framework in TFMS:
                member = f"lib/{framework}/Elsa.Slack.pdb"
                pdb_bytes = symbol_archive.read(member)
                (output / "pdb" / f"Elsa.Slack.{framework}.pdb").write_bytes(pdb_bytes)
                pair = output / "pairs" / framework
                pair.mkdir(parents=True)
                packaged_assembly = pair / "Elsa.Slack.dll"
                pdb = pair / "Elsa.Slack.pdb"
                packaged_assembly.write_bytes(package_archive.read(f"lib/{framework}/Elsa.Slack.dll"))
                pdb.write_bytes(pdb_bytes)
                pairing = subprocess.run(
                    [dotnet, str(pair_verifier), str(packaged_assembly), str(pdb)],
                    capture_output=True,
                    text=True,
                )
                if pairing.returncode != 0:
                    raise ValueError(f"Packaged assembly and external symbols do not match for {framework}: {pairing.stderr.strip()}")
        results = verify_imported_source_link(output, commit, source_link_assembly, Path(dotnet), os.environ.copy())
        for result in results:
            result["assemblySymbolPair"] = "passed"

    return {
        "packageId": "Elsa.Slack",
        "version": version,
        "sourceCommit": commit,
        "frameworks": results,
        "nupkgSha256": hashlib.sha256(package.read_bytes()).hexdigest(),
        "snupkgSha256": hashlib.sha256(symbols.read_bytes()).hexdigest(),
        "sourceLinkToolVersion": tool_version,
        "sourceLinkToolSha256": tool_sha256,
        "published": False,
    }


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--artifacts", type=Path, required=True)
    parser.add_argument("--version", required=True)
    parser.add_argument("--commit", required=True)
    parser.add_argument("--sourcelink-tool", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    result = verify(args.artifacts, args.version, args.commit, args.sourcelink_tool)
    args.output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(f"Verified Elsa.Slack SourceLink on {', '.join(TFMS)} at {args.commit}")


if __name__ == "__main__":
    main()
