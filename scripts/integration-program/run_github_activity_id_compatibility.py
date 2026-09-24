#!/usr/bin/env python3
"""Replay the bounded GitHub activity-ID source patch against pinned sources."""

from __future__ import annotations

import argparse
import base64
import hashlib
import json
import os
import shutil
import subprocess
import sys
import tarfile
import tempfile
import zipfile
from pathlib import Path, PurePosixPath
from typing import Any
from xml.etree.ElementTree import ParseError
from xml.sax.saxutils import quoteattr

from run_slack_package_proof import read_test_trx_results


HERE = Path(__file__).resolve().parent
REPOSITORY = HERE.parents[1]
PATCH_PATH = HERE / "github-activity-id-compatibility/extensions-v2.patch"
CORE_SHA = "6f493809eae0e1652ca185b901a982984f4ef799"
EXTENSIONS_SHA = "33fa0bfd28c7585240e3d4f665058c067b17e287"
EXPECTED_TESTS = 6
GENERATOR_ID = "elsa.platform.packagemanifest.generator"
GENERATOR_VERSION = "0.0.1-preview.50"
GENERATOR_NUPKG_SHA256 = "56310f3c6606c793bce875f0dee5746dc5f42721d0cbbfde5fa3c4b61e6f15aa"
GITHUB_PROJECT = Path("src/modules/devops/Elsa.DevOps.GitHub/Elsa.DevOps.GitHub.csproj")
CORE_REFERENCE = br'ProjectReference Include="..\..\..\..\..\elsa-core\src\modules\Elsa\Elsa.csproj"'
BUILD_INPUT_NAMES = {
    "Directory.Build.props",
    "Directory.Build.targets",
    "Directory.Packages.props",
    "Directory.Build.rsp",
    "MSBuild.rsp",
    "NuGet.Config",
    "nuget.config",
    "global.json",
}
class ProofError(ValueError):
    """The pinned input, patch, or test receipt is not safe to claim."""


def git_text(repository: Path, *arguments: str) -> str:
    result = subprocess.run(
        ["git", "-C", str(repository), *arguments],
        check=False,
        capture_output=True,
        text=True,
    )
    if result.returncode != 0:
        raise ProofError(result.stderr.strip() or f"git command failed: {arguments!r}")
    return result.stdout.strip()


def validate_source(repository: Path, expected_sha: str, paths: tuple[str, ...] | None) -> str:
    actual_sha = git_text(repository, "rev-parse", "HEAD")
    if actual_sha != expected_sha:
        raise ProofError(f"Expected source pin {expected_sha}, found {actual_sha} at {repository}")
    status_arguments = ["status", "--porcelain", "--untracked-files=all"]
    if paths is not None:
        status_arguments.extend(["--", *paths])
    status = git_text(repository, *status_arguments)
    if status:
        raise ProofError(f"Source build inputs are modified at {repository}:\n{status}")
    return actual_sha


def core_build_input_paths(repository: Path) -> tuple[str, ...]:
    tracked = git_text(repository, "ls-files").splitlines()
    untracked = git_text(repository, "ls-files", "--others", "--exclude-standard").splitlines()
    configuration = {
        path
        for path in (*tracked, *untracked)
    if Path(path).name in BUILD_INPUT_NAMES or Path(path).suffix in {".props", ".targets", ".rsp"}
}
    return ("src", *sorted(configuration))


def resolve_dotnet(value: str) -> Path:
    candidate = Path(value).expanduser()
    if candidate.is_absolute() or candidate.parent != Path("."):
        resolved = candidate.resolve(strict=True)
    else:
        located = shutil.which(value)
        if located is None:
            raise ProofError(f"Cannot resolve dotnet executable {value!r}")
        resolved = Path(located).resolve(strict=True)
    if not resolved.is_file():
        raise ProofError(f"dotnet executable is not a file: {resolved}")
    return resolved


def output_directory(path: Path, source_roots: tuple[Path, ...]) -> Path:
    if path.exists() or path.is_symlink():
        raise ProofError(f"Refusing to overwrite existing output directory: {path}")
    parent = path.parent.resolve(strict=True)
    result = parent / path.name
    for source_root in source_roots:
        if result == source_root or source_root in result.parents:
            raise ProofError(f"Proof output must be outside inspected source: {source_root}")
    result.mkdir()
    return result


def isolated_dotnet_environment(output: Path) -> dict[str, str]:
    environment = os.environ.copy()
    for name, relative in (
        ("DOTNET_CLI_HOME", "dotnet-home"),
        ("NUGET_PACKAGES", "nuget-packages"),
        ("NUGET_HTTP_CACHE_PATH", "nuget-http-cache"),
    ):
        location = output / relative
        location.mkdir()
        environment[name] = str(location)
    environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
    environment["DOTNET_NOLOGO"] = "1"
    return environment


def stage_required_generator(source: Path, output: Path) -> dict[str, str]:
    """Stage one exact pinned package that the historic feed no longer restores here."""
    source = source.expanduser()
    if source.is_symlink() or not source.is_dir():
        raise ProofError("Generator package cache must be an ordinary directory")
    source = source.resolve(strict=True)
    nupkg = source / f"{GENERATOR_ID}.{GENERATOR_VERSION}.nupkg"
    if not nupkg.is_file() or nupkg.is_symlink() or sha256(nupkg) != GENERATOR_NUPKG_SHA256:
        raise ProofError("Generator package cache is not the reviewed pinned nupkg")
    destination = output / "nuget-packages" / GENERATOR_ID / GENERATOR_VERSION
    destination.mkdir(parents=True, exist_ok=False)
    with zipfile.ZipFile(nupkg) as archive:
        names: set[str] = set()
        for member in archive.infolist():
            relative = PurePosixPath(member.filename)
            if relative.is_absolute() or ".." in relative.parts or member.filename in names:
                raise ProofError(f"Invalid generator package entry: {member.filename!r}")
            names.add(member.filename)
            target = destination.joinpath(*relative.parts)
            if member.is_dir():
                target.mkdir(parents=True, exist_ok=True)
            else:
                target.parent.mkdir(parents=True, exist_ok=True)
                with archive.open(member) as stream, target.open("xb") as file:
                    shutil.copyfileobj(stream, file)
    package_bytes = nupkg.read_bytes()
    (destination / nupkg.name).write_bytes(package_bytes)
    content_hash = base64.b64encode(hashlib.sha512(package_bytes).digest()).decode("ascii")
    (destination / f"{nupkg.name}.sha512").write_text(content_hash, encoding="ascii")
    (destination / ".nupkg.metadata").write_text(json.dumps({
        "version": 2,
        "contentHash": content_hash,
        "source": "explicitly supplied local nupkg",
    }), encoding="utf-8")
    return {"id": GENERATOR_ID, "version": GENERATOR_VERSION, "nupkgSha256": GENERATOR_NUPKG_SHA256,
            "source": "Explicit local artifact; NuGet feed provenance is not independently verified by this replay"}


def extract_source_archive(source: Path, commit: str, destination: Path) -> None:
    archive_path = destination.parent / "extensions-source.tar"
    with archive_path.open("wb") as archive_file:
        result = subprocess.run(
            ["git", "-C", str(source), "archive", "--format=tar", commit],
            check=False,
            stdout=archive_file,
            stderr=subprocess.PIPE,
            text=True,
        )
    if result.returncode != 0:
        raise ProofError(result.stderr.strip() or "Could not archive pinned Extensions source")

    destination.mkdir()
    base = destination.resolve()
    with tarfile.open(archive_path, mode="r:") as archive:
        for member in archive.getmembers():
            relative = PurePosixPath(member.name)
            if relative.is_absolute() or ".." in relative.parts:
                raise ProofError(f"Unsafe path in pinned source archive: {member.name!r}")
            target = destination.joinpath(*relative.parts)
            resolved_target = target.resolve(strict=False)
            if resolved_target != base and base not in resolved_target.parents:
                raise ProofError(f"Archive path escaped staging root: {member.name!r}")
            if member.isdir():
                target.mkdir(parents=True, exist_ok=True)
                continue
            if not member.isfile():
                raise ProofError(f"Unsupported link or special file in source archive: {member.name!r}")
            target.parent.mkdir(parents=True, exist_ok=True)
            extracted = archive.extractfile(member)
            if extracted is None:
                raise ProofError(f"Cannot read archived source file: {member.name!r}")
            with extracted, target.open("wb") as output:
                shutil.copyfileobj(extracted, output)
            target.chmod(member.mode & 0o777)
    archive_path.unlink()


def override_fixture_project_reference(project_file: Path, core_source: Path) -> str:
    original = project_file.read_bytes()
    occurrences = original.count(CORE_REFERENCE)
    if occurrences != 1:
        raise ProofError(
            "Expected one upstream sibling Core ProjectReference in the disposable Extensions source; "
            f"found {occurrences}"
        )
    target = (core_source / "src/modules/Elsa/Elsa.csproj").resolve(strict=True).as_posix()
    replacement = b"ProjectReference Include=" + quoteattr(target).encode("utf-8")
    rewritten = original.replace(CORE_REFERENCE, replacement)
    project_file.write_bytes(rewritten)
    return hashlib.sha256(original).hexdigest()


def read_test_counts(trx_path: Path) -> dict[str, Any]:
    try:
        parsed = read_test_trx_results(
            trx_path.parent,
            missing_results_message=f"Focused GitHub activity test command produced no TRX result in {trx_path.parent}",
        )
    except (OSError, ParseError, RuntimeError) as error:
        raise ProofError(f"Cannot read test result TRX {trx_path}: {error}") from error
    if len(parsed.files) != 1:
        raise ProofError(f"Expected one focused test TRX file, found {len(parsed.files)}")
    counts = parsed.counters
    outcomes = [test.get("outcome", "") for test in parsed.unit_results]
    passed = (
        len(parsed.unit_results) == EXPECTED_TESTS
        and counts.get("total") == EXPECTED_TESTS
        and counts.get("executed") == EXPECTED_TESTS
        and counts.get("passed") == EXPECTED_TESTS
        and all(
            value == 0
            for name, value in counts.items()
            if name not in {"total", "executed", "passed"}
        )
        and outcomes == ["Passed"] * EXPECTED_TESTS
    )
    return {
        "passed": passed,
        "counters": counts,
        "testNames": [test.get("testName", "") for test in parsed.unit_results],
        "outcomes": outcomes,
    }


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as source:
        for block in iter(lambda: source.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def run(args: argparse.Namespace) -> int:
    extensions_source = args.extensions_source.resolve(strict=True)
    core_source = args.core_source.resolve(strict=True)
    output = output_directory(args.output_dir.expanduser(), (extensions_source, core_source))
    receipt: dict[str, Any] = {
        "schemaVersion": 1,
        "scope": "Mapped Extensions source patch and focused local activity identity tests; no package publication",
        "status": "failed",
        "pins": {},
        "patch": {},
        "test": {"framework": "net10.0", "expectedCount": EXPECTED_TESTS},
        "limitations": [
            "This is a focused net10.0 mapped-source compatibility proof, not the actual history-preserving import.",
            "It does not establish full solution or net8.0/net9.0 test results, SourceLink identity, release packaging, or publisher behavior.",
            "The disposable Extensions copy receives one fixture-only absolute Core ProjectReference mapping; upstream source and patch do not contain it.",
        ],
    }
    log_path = output / "net10-dotnet-test.log"
    trx_path = output / "net10-results/github-activity-id.trx"
    receipt_path = output / "receipt.json"

    try:
        receipt["pins"] = {
            "core": validate_source(
                core_source,
                CORE_SHA,
                core_build_input_paths(core_source),
            ),
            "extensions": validate_source(extensions_source, EXTENSIONS_SHA, None),
        }
        if not PATCH_PATH.is_file():
            raise ProofError(f"Compatibility patch is missing: {PATCH_PATH}")
        patch_bytes = PATCH_PATH.read_bytes()
        receipt["patch"] = {
            "path": "scripts/integration-program/github-activity-id-compatibility/extensions-v2.patch",
            "sha256": hashlib.sha256(patch_bytes).hexdigest(),
            "bytes": len(patch_bytes),
        }
        dotnet = resolve_dotnet(args.dotnet)
        receipt["test"]["dotnetResolved"] = True

        with tempfile.TemporaryDirectory(prefix="github-activity-id-replay-", dir=output) as temporary:
            temp_root = Path(temporary)
            extensions_tree = temp_root / "extensions"
            extract_source_archive(extensions_source, EXTENSIONS_SHA, extensions_tree)
            check = subprocess.run(
                ["git", "apply", "--check", "--whitespace=error-all", str(PATCH_PATH)],
                cwd=extensions_tree,
                check=False,
                capture_output=True,
                text=True,
            )
            if check.returncode != 0:
                raise ProofError(f"Patch does not apply cleanly to the pinned Extensions source: {check.stderr.strip()}")
            applied = subprocess.run(
                ["git", "apply", "--whitespace=error-all", str(PATCH_PATH)],
                cwd=extensions_tree,
                check=False,
                capture_output=True,
                text=True,
            )
            if applied.returncode != 0:
                raise ProofError(f"Could not apply the pinned compatibility patch: {applied.stderr.strip()}")

            project_file = extensions_tree / GITHUB_PROJECT
            original_project_sha = override_fixture_project_reference(project_file, core_source)
            receipt["fixtureOnlyProjectReference"] = {
                "sourceProjectSha256": original_project_sha,
                "mapping": "Sibling Core reference rewritten only in disposable test source to the supplied pinned Core project",
            }
            test_project = extensions_tree / "test/modules/devops/Elsa.DevOps.GitHub.UnitTests/Elsa.DevOps.GitHub.UnitTests.csproj"
            if not test_project.is_file():
                raise ProofError("Patch did not create the focused GitHub activity identity test project")
            results_dir = output / "net10-results"
            results_dir.mkdir()
            command = [
                str(dotnet),
                "test",
                str(test_project),
                "--framework",
                "net10.0",
                "-p:UseProjectReferences=true",
                "--logger",
                "trx;LogFileName=github-activity-id.trx",
                "--results-directory",
                str(results_dir),
            ]
            environment = isolated_dotnet_environment(output)
            receipt["test"]["seededPackage"] = stage_required_generator(args.generator_cache, output)
            receipt["test"]["cacheIsolation"] = {
                "dotnetCliHome": "dotnet-home",
                "nugetPackages": "nuget-packages",
                "nugetHttpCache": "nuget-http-cache",
            }
            with log_path.open("wb") as log_file:
                try:
                    completed = subprocess.run(
                        command,
                        cwd=extensions_tree,
                        env=environment,
                        stdout=log_file,
                        stderr=subprocess.STDOUT,
                        check=False,
                        timeout=args.timeout_seconds,
                    )
                    exit_code = completed.returncode
                except subprocess.TimeoutExpired:
                    exit_code = 124
                    log_file.write(f"\nTimed out after {args.timeout_seconds} seconds.\n".encode())
            receipt["test"]["command"] = [
                "$DOTNET",
                "test",
                "<patched-extension>/test/modules/devops/Elsa.DevOps.GitHub.UnitTests/Elsa.DevOps.GitHub.UnitTests.csproj",
                "--framework",
                "net10.0",
                "-p:UseProjectReferences=true",
                "--logger",
                "trx;LogFileName=github-activity-id.trx",
            ]
            receipt["test"]["exitCode"] = exit_code

        if log_path.is_file():
            receipt["test"]["logSha256"] = sha256(log_path)
            receipt["test"]["logPath"] = log_path.name
        if trx_path.is_file():
            counts = read_test_counts(trx_path)
            receipt["test"].update(counts)
            receipt["test"]["trxSha256"] = sha256(trx_path)
            receipt["test"]["trxPath"] = trx_path.relative_to(output).as_posix()
        if receipt["test"].get("exitCode") == 0 and receipt["test"].get("passed") is True:
            receipt["status"] = "passed"
        else:
            receipt["status"] = "failed"
    except (OSError, ProofError, subprocess.SubprocessError, tarfile.TarError) as error:
        receipt["error"] = str(error)
        receipt["status"] = "failed"

    receipt_path.write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(json.dumps({"status": receipt["status"], "receipt": str(receipt_path)}, sort_keys=True))
    if receipt["status"] != "passed":
        if "error" in receipt:
            print(receipt["error"], file=sys.stderr)
        return 1
    return 0


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--extensions-source", type=Path, required=True, help="Clean Extensions checkout at the pinned source SHA")
    parser.add_argument("--core-source", type=Path, required=True, help="Core checkout with pinned project/build inputs")
    parser.add_argument("--output-dir", type=Path, required=True, help="New output directory outside both source trees")
    parser.add_argument("--dotnet", default="dotnet", help="dotnet executable or path")
    parser.add_argument("--generator-cache", type=Path, required=True,
                        help="Extracted Elsa.Platform.PackageManifest.Generator 0.0.1-preview.50 cache folder; exact nupkg SHA is validated")
    parser.add_argument("--timeout-seconds", type=int, default=600)
    args = parser.parse_args(argv)
    if args.timeout_seconds < 1:
        parser.error("--timeout-seconds must be positive")
    return run(args)


if __name__ == "__main__":
    raise SystemExit(main())
