#!/usr/bin/env python3
"""Build the existing paired Blazor probe against this checkout's imported source."""

from __future__ import annotations

import argparse
import hashlib
import json
import subprocess
from pathlib import Path
from shutil import copy2
from xml.sax.saxutils import escape

from prepare_paired_blazor_host import AMBIENT_CONFIG


ROOT = Path(__file__).resolve().parents[2]
FIXTURE = ROOT / "scripts/integration-program/paired-blazor-host"
CONTRACT = ROOT / "scripts/integration-program/paired-source-probe/HttpProbe.cs"
SOURCE_FILES = ("Program.cs", "App.razor", "Routes.razor", "Probe.razor", "_Imports.razor")
REFERENCES = {
    "../elsa-core/src/modules/Elsa/Elsa.csproj": "src/modules/Elsa/Elsa.csproj",
    "../elsa-extensions/src/modules/workflows/Elsa.WorkflowContexts/Elsa.WorkflowContexts.csproj":
        "src/extensions/workflows/Elsa.WorkflowContexts/Elsa.WorkflowContexts.csproj",
    "../elsa-extensions/src/modules/workflows/Elsa.Studio.WorkflowContexts/Elsa.Studio.WorkflowContexts.csproj":
        "src/extensions/workflows/Elsa.Studio.WorkflowContexts/Elsa.Studio.WorkflowContexts.csproj",
}


def materialize(output: Path) -> dict[str, object]:
    if not output.is_absolute():
        raise ValueError("Output must be an absolute path")
    output = output.resolve()
    if output.is_relative_to(ROOT):
        raise ValueError("Output must be outside the source checkout")
    if output.exists():
        raise FileExistsError(f"Refusing to overwrite existing output: {output}")
    for directory in output.parents:
        if not directory.exists():
            continue
        for entry in directory.iterdir():
            if entry.name.lower() in AMBIENT_CONFIG:
                raise ValueError(f"Ambient build configuration is not permitted: {entry}")

    host = output / "UiProbe"
    contract = output / "ContractProbe"
    host.mkdir(parents=True)
    contract.mkdir()
    for name in SOURCE_FILES:
        copy2(FIXTURE / name, host / name)
    copy2(CONTRACT, contract / CONTRACT.name)

    project = (FIXTURE / "UiProbe.csproj").read_text(encoding="utf-8")
    for original, mapped in REFERENCES.items():
        if project.count(original) != 1:
            raise ValueError(f"Expected exactly one pinned project reference: {original}")
        target = ROOT / mapped
        if not target.is_file():
            raise FileNotFoundError(target)
        project = project.replace(original, escape(str(target), {'"': "&quot;"}))
    (host / "UiProbe.csproj").write_text(project, encoding="utf-8")

    source_head = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip()
    source_status = subprocess.check_output(["git", "status", "--porcelain"], cwd=ROOT, text=True).strip()
    return {
        "sourceHead": source_head,
        "sourceDirty": bool(source_status),
        "hostProject": str(host / "UiProbe.csproj"),
        "fixtureSha256": {
            name: hashlib.sha256((FIXTURE / name).read_bytes()).hexdigest()
            for name in SOURCE_FILES
        },
        "contractSha256": hashlib.sha256(CONTRACT.read_bytes()).hexdigest(),
        "published": False,
        "browserVerified": False,
        "debuggerVerified": False,
    }


def build_host(output: Path, receipt: dict[str, object]) -> int:
    command = [
        "dotnet", "build", receipt["hostProject"], "--configuration", "Debug", "--framework", "net10.0",
        "-p:UseProjectReferences=true", "-p:IsPackable=false", "-p:GeneratePackageOnBuild=false",
        "--verbosity", "quiet",
    ]
    receipt["buildCommand"] = command
    try:
        with (output / "build.log").open("w", encoding="utf-8") as log:
            result = subprocess.run(command, cwd=output / "UiProbe", stdout=log, stderr=subprocess.STDOUT,
                                    check=False, timeout=900)
        receipt["buildExitCode"] = result.returncode
    except (OSError, subprocess.TimeoutExpired) as error:
        receipt["buildExitCode"] = None
        receipt["buildFailure"] = type(error).__name__
        (output / "evidence.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
        raise RuntimeError("Host build did not finish; inspect build.log and evidence.json before removing the disposable output or choosing a new path") from error
    (output / "evidence.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    return result.returncode


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True, help="New disposable directory outside this checkout")
    args = parser.parse_args()
    receipt = materialize(args.output)
    output = args.output.resolve()
    result = build_host(output, receipt)
    print(json.dumps(receipt, indent=2))
    if result:
        raise SystemExit(result)


if __name__ == "__main__":
    main()
