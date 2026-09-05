#!/usr/bin/env python3
"""Prepare or execute Elsa-style GitHub releases from Git tags."""

from __future__ import annotations

import argparse
import json
import re
import shutil
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path

try:
    from release_support import ReleaseVersion, parse_version
except ImportError:  # pragma: no cover - supports importing as a package in tests
    from .release_support import ReleaseVersion, parse_version


@dataclass(frozen=True)
class Command:
    args: list[str]
    cwd: Path | None = None


@dataclass(frozen=True)
class ExistingRelease:
    tag_name: str
    target_commitish: str
    is_draft: bool
    is_prerelease: bool


def main() -> int:
    args = parse_args()
    release_version = parse_release_version(args.tag, args.release_kind)
    repo_path = Path(args.repo_path).expanduser().resolve()

    ensure_tool("git")
    ensure_tool("gh")
    ensure_git_repo(repo_path)

    remote = args.remote
    github_repo = args.github_repo or infer_github_repo(repo_path, remote)
    source_ref = args.source_ref or intended_source_ref(release_version, remote)
    validate_source_override(source_ref, release_version)
    notes_file = validate_notes_file(repo_path, args.notes_file, release_version.version)

    # All checks below are read-only. Mutating commands are built and run only
    # after the source, tag, release, and containment checks succeed.
    source_commit = resolve_source_commit(repo_path, source_ref, remote)
    containing_branches = remote_branches_containing(repo_path, source_commit)
    if not containing_branches and is_remote_branch_ref(source_ref, remote):
        containing_branches = [source_ref]
    validate_containing_branches(containing_branches, args.allow_uncontained, remote=remote)

    local_tag_commit = local_tag_target(repo_path, args.tag)
    remote_tag_commit = remote_tag_target(repo_path, remote, args.tag)
    validate_existing_tag(args.tag, source_commit, local_tag_commit, remote_tag_commit)

    repository_visible = verify_github_repo(repo_path, github_repo)
    existing_release = inspect_release(repo_path, github_repo, args.tag, repository_visible=repository_visible)
    if existing_release is not None:
        if remote_tag_commit is None:
            fail(f"GitHub release {args.tag} exists but its remote tag is missing; refusing to recreate or move the tag.")
        validate_existing_release(existing_release, args.tag, source_commit, release_version, remote_tag_commit)
        print(f"GitHub release {args.tag} already exists at the requested commit; the helper will reuse it.")

    tag_exists_local = local_tag_commit is not None
    tag_exists_remote = remote_tag_commit is not None
    title = args.title or args.tag
    commands = build_commands(
        repo_path=repo_path,
        remote=remote,
        github_repo=github_repo,
        tag=args.tag,
        source_commit=source_commit,
        title=title,
        release_kind=release_version.kind,
        notes_file=str(notes_file) if notes_file is not None else None,
        notes_start_tag=args.notes_start_tag,
        tag_exists_local=tag_exists_local,
        tag_exists_remote=tag_exists_remote,
        release_exists=existing_release is not None,
        source_ref=source_ref,
    )

    print_summary(
        repo_path=repo_path,
        github_repo=github_repo,
        source_ref=source_ref,
        source_commit=source_commit,
        tag=args.tag,
        release_kind=release_version.kind,
        containing_branches=containing_branches,
        execute=args.execute,
    )

    for command in commands:
        print("$ " + shell_join(command.args), flush=True)
        if args.execute:
            run(command.args, cwd=command.cwd, execute=True)

    if not args.execute:
        print("\nDry run only. Re-run with --execute after explicit release approval.")

    return 0


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo-path", default=".", help="Local repository path.")
    parser.add_argument("--github-repo", help="GitHub repository as owner/name. Inferred from the git remote when omitted.")
    parser.add_argument("--remote", default="origin", help="Git remote to inspect and push tags to.")
    parser.add_argument("--source-ref", help="Existing tag, branch, or commit for the release tag. Defaults to origin/release/{base}.")
    parser.add_argument("--tag", required=True, help="Desired release tag, e.g. 3.7.0 or 3.8.0-preview2.")
    parser.add_argument("--release-kind", choices=("stable", "rc", "preview"), help="Release kind. When omitted, infer it from the SemVer tag.")
    parser.add_argument("--title", help="GitHub release title. Defaults to the tag.")
    parser.add_argument("--notes-file", help="Release notes Markdown file. Defaults to GitHub generated notes.")
    parser.add_argument("--notes-start-tag", help="Starting tag for GitHub generated release notes.")
    parser.add_argument("--allow-uncontained", action="store_true", help="Allow source commits not contained in origin/main or origin/release/*.")
    parser.add_argument("--execute", action="store_true", help="Create/push the tag and publish the GitHub release.")
    return parser.parse_args()


def parse_release_version(tag: str, kind: str | None) -> ReleaseVersion:
    try:
        return parse_version(tag, kind)
    except ValueError as error:
        fail(str(error))


def intended_source_ref(version: ReleaseVersion, remote: str = "origin") -> str:
    return f"{remote}/release/{version.base}"


def validate_source_override(source_ref: str, target: ReleaseVersion) -> None:
    """Reject an RC source for a stable tag when its base version differs."""

    candidate = source_ref.removeprefix("refs/tags/").removesuffix("^{}")
    candidate = candidate.rsplit("/", 1)[-1]
    try:
        source_version = parse_version(candidate)
    except ValueError:
        return

    if target.kind == "stable" and source_version.kind == "rc" and source_version.base != target.base:
        fail(
            f"RC source {source_ref} has base {source_version.base}, but stable release {target.version} has base {target.base}."
        )


def validate_notes_file(repo_path: Path, notes_file: str | None, requested_version: str | None = None) -> Path | None:
    if notes_file is None:
        return None
    path = Path(notes_file).expanduser()
    if not path.is_absolute():
        path = repo_path / path
    if not path.is_file():
        fail(f"Release notes file does not exist: {path}")
    content = path.read_text(encoding="utf-8")
    if not content.strip():
        fail(f"Release notes file is empty: {path}")
    version_metadata = re.search(r"<!--\s*elsa-release-version:\s*([^\s]+)\s*-->", content)
    if version_metadata is not None and requested_version is not None and version_metadata.group(1) != requested_version:
        fail(
            f"Release notes version metadata is {version_metadata.group(1)}, not requested version {requested_version}: {path}"
        )
    if "Review before publishing:" in content or "Rewrite bullets so they explain developer impact" in content:
        fail(f"Release notes still contain the generated scaffold review marker: {path}")
    return path


def build_commands(
    *,
    repo_path: Path,
    remote: str,
    github_repo: str,
    tag: str,
    source_commit: str,
    title: str,
    release_kind: str,
    notes_file: str | None,
    notes_start_tag: str | None,
    tag_exists_local: bool,
    tag_exists_remote: bool,
    release_exists: bool = False,
    source_ref: str | None = None,
) -> list[Command]:
    commands: list[Command] = []

    # A remote-only source may not have a local object. Fetching is planned
    # only after all validation has passed and is skipped for release reuse.
    if source_ref and not ref_exists(repo_path, f"{source_ref}^{{commit}}") and not release_exists:
        fetch_remote = source_ref.split("/", 1)[0] if "/" in source_ref else remote
        if fetch_remote == remote and is_remote_branch_ref(source_ref, remote):
            commands.append(Command(["git", "fetch", "--tags", "--prune", remote], repo_path))

    if not release_exists:
        if not tag_exists_local:
            commands.append(Command(["git", "tag", "-a", tag, source_commit, "-m", f"Release {tag}"], repo_path))

        if not tag_exists_remote:
            commands.append(Command(["git", "push", remote, f"refs/tags/{tag}"], repo_path))

        release = [
            "gh",
            "release",
            "create",
            tag,
            "--repo",
            github_repo,
            "--verify-tag",
            "--target",
            source_commit,
            "--title",
            title,
        ]

        if notes_file:
            release.extend(["--notes-file", notes_file])
        else:
            release.append("--generate-notes")
            if notes_start_tag:
                release.extend(["--notes-start-tag", notes_start_tag])

        if release_kind in {"preview", "rc"}:
            release.extend(["--prerelease", "--latest=false"])
        else:
            release.append("--latest")

        commands.append(Command(release, repo_path))
    return commands


def print_summary(
    *,
    repo_path: Path,
    github_repo: str,
    source_ref: str,
    source_commit: str,
    tag: str,
    release_kind: str,
    containing_branches: list[str],
    execute: bool,
) -> None:
    mode = "EXECUTE" if execute else "DRY RUN"
    print(f"Mode: {mode}")
    print(f"Repository path: {repo_path}")
    print(f"GitHub repository: {github_repo}")
    print(f"Source ref: {source_ref}")
    print(f"Source commit: {source_commit}")
    print(f"Release tag: {tag}")
    print(f"Release kind: {release_kind}")
    print("Containing remote branches:")
    for branch in containing_branches:
        print(f"  - {branch}")
    print()


def validate_containing_branches(branches: list[str], allow_uncontained: bool, *, remote: str = "origin") -> None:
    if allow_uncontained:
        return

    accepted_remotes = {"origin", remote}
    for branch in branches:
        normalized = branch.strip()
        prefix, separator, suffix = normalized.partition("/")
        if separator and prefix in accepted_remotes and (suffix == "main" or suffix.startswith("release/")):
            return

    fail(
        f"Source commit is not contained in origin/main, origin/release/*, {remote}/main, or {remote}/release/*. "
        "Use --allow-uncontained only after reviewing the workflow risk."
    )


def remote_branches_containing(repo_path: Path, commit: str) -> list[str]:
    result = subprocess.run(
        ["git", "branch", "--remote", "--contains", commit],
        cwd=repo_path,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    if result.returncode != 0:
        if re.search(r"malformed object|not a valid object|unknown revision|bad object", result.stderr, re.IGNORECASE):
            return []
        fail(result.stderr.strip() or f"git branch --remote --contains {commit} failed")
    return [line.strip().lstrip("* ").strip() for line in result.stdout.splitlines() if line.strip()]


def resolve_source_commit(repo_path: Path, source_ref: str, remote: str) -> str:
    if is_remote_branch_ref(source_ref, remote):
        branch = source_ref[len(remote) + 1 :]
        output = git_remote(repo_path, remote, [f"refs/heads/{branch}"])
        remote_commit = None
        for line in output.splitlines():
            parts = line.split()
            if len(parts) >= 2 and parts[1] == f"refs/heads/{branch}":
                remote_commit = parts[0]
                break
        if remote_commit is None:
            fail(f"Source ref does not exist on remote {remote}: {source_ref}")

        local = git(["rev-parse", "--verify", f"{source_ref}^{{commit}}"], repo_path, check=False)
        if local and local != remote_commit:
            fail(
                f"Local source ref {source_ref} points to stale commit {local}; remote points to {remote_commit}. "
                "Refresh the remote-tracking ref before releasing."
            )
        return remote_commit

    local = git(["rev-parse", "--verify", f"{source_ref}^{{commit}}"], repo_path, check=False)
    if local:
        return local

    fail(f"Source ref does not resolve to a commit: {source_ref}")


def is_remote_branch_ref(source_ref: str, remote: str) -> bool:
    return source_ref.startswith(f"{remote}/") and not source_ref.startswith(f"{remote}/tags/")


def local_tag_target(repo_path: Path, tag: str) -> str | None:
    value = git(["rev-list", "-n", "1", f"refs/tags/{tag}^{{commit}}"], repo_path, check=False)
    return value or None


def remote_tag_target(repo_path: Path, remote: str, tag: str) -> str | None:
    output = git_remote(repo_path, remote, [f"refs/tags/{tag}", f"refs/tags/{tag}^{{}}"])
    if not output:
        return None
    peeled = None
    direct = None
    for line in output.splitlines():
        parts = line.split()
        if len(parts) != 2:
            continue
        if parts[1] == f"refs/tags/{tag}^{{}}":
            peeled = parts[0]
        elif parts[1] == f"refs/tags/{tag}":
            direct = parts[0]
    return peeled or direct


def validate_existing_tag(tag: str, source_commit: str, local_commit: str | None, remote_commit: str | None) -> None:
    for location, existing_commit in (("local", local_commit), ("remote", remote_commit)):
        if existing_commit is not None and existing_commit != source_commit:
            fail(f"Tag {tag} already exists {location} and points to {existing_commit}, not {source_commit}.")
    if local_commit is not None and remote_commit is not None and local_commit != remote_commit:
        fail(f"Tag {tag} differs between the local repository ({local_commit}) and remote ({remote_commit}).")


def verify_github_repo(repo_path: Path, github_repo: str) -> bool:
    result = subprocess.run(
        ["gh", "repo", "view", github_repo, "--json", "nameWithOwner"],
        cwd=repo_path,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    if result.returncode != 0:
        fail(result.stderr.strip() or result.stdout.strip() or f"Could not access GitHub repository {github_repo}.")
    try:
        payload = json.loads(result.stdout)
        actual = str(payload["nameWithOwner"])
    except (KeyError, TypeError, ValueError, json.JSONDecodeError) as error:
        fail(f"GitHub returned invalid repository metadata for {github_repo}: {error}")
    if actual.lower() != github_repo.lower():
        fail(f"GitHub repository lookup returned {actual}, not requested repository {github_repo}.")
    return True


def inspect_release(
    repo_path: Path,
    github_repo: str,
    tag: str,
    *,
    repository_visible: bool = False,
) -> ExistingRelease | None:
    result = subprocess.run(
        ["gh", "release", "view", tag, "--repo", github_repo, "--json", "tagName,targetCommitish,isDraft,isPrerelease"],
        cwd=repo_path,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    if result.returncode != 0:
        if is_github_not_found(result.stdout, result.stderr, repository_visible=repository_visible):
            return None
        fail(result.stderr.strip() or result.stdout.strip() or "Could not inspect the GitHub release.")

    try:
        payload = json.loads(result.stdout)
        return ExistingRelease(
            tag_name=str(payload["tagName"]),
            target_commitish=str(payload["targetCommitish"]),
            is_draft=bool(payload["isDraft"]),
            is_prerelease=bool(payload["isPrerelease"]),
        )
    except (KeyError, TypeError, ValueError, json.JSONDecodeError) as error:
        fail(f"GitHub returned invalid release metadata for {tag}: {error}")


def is_github_not_found(stdout: str, stderr: str, *, repository_visible: bool = False) -> bool:
    text = f"{stdout}\n{stderr}".lower()
    if any(marker in text for marker in ("401", "403", "unauthorized", "forbidden", "rate limit", "timed out", "could not resolve")):
        return False
    if re.search(r"\bhttp(?: error)?\s*404\b|\bstatus(?: code)?\s*[:=]?\s*404\b|\b404\s+not found\b", text):
        return True
    return repository_visible and "release not found" in text


def validate_existing_release(
    release: ExistingRelease,
    tag: str,
    source_commit: str,
    expected: ReleaseVersion,
    remote_tag_commit: str | None = None,
) -> None:
    if release.tag_name != tag:
        fail(f"GitHub returned release {release.tag_name} while looking up {tag}.")
    if remote_tag_commit is not None and remote_tag_commit != source_commit:
        fail(
            f"Remote tag {tag} resolves to {remote_tag_commit}, not the requested commit {source_commit}."
        )
    if _looks_like_commit(release.target_commitish) and not source_commit.lower().startswith(release.target_commitish.lower()):
        fail(
            f"GitHub release {tag} targets {release.target_commitish}, not the requested commit {source_commit}."
        )
    if release.is_prerelease != expected.prerelease:
        actual_kind = "preview/rc" if release.is_prerelease else "stable"
        fail(f"GitHub release {tag} has kind {actual_kind}, not {expected.kind}.")
    if release.is_draft:
        fail(f"GitHub release {tag} is a draft; refusing to reuse or edit it.")


def _looks_like_commit(value: str) -> bool:
    return bool(re.fullmatch(r"[0-9a-fA-F]{7,40}", value))


def git_remote(repo_path: Path, remote: str, refs: list[str]) -> str:
    command = ["git", "ls-remote"]
    if all(ref.startswith("refs/tags/") for ref in refs):
        command.append("--tags")
    command.extend([remote, *refs])
    result = subprocess.run(command, cwd=repo_path, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    if result.returncode != 0:
        fail(result.stderr.strip() or f"Could not inspect remote {remote}.")
    return result.stdout.strip()


def remote_tag_exists(repo_path: Path, remote: str, tag: str) -> bool:
    return remote_tag_target(repo_path, remote, tag) is not None


def ref_exists(repo_path: Path, ref: str) -> bool:
    result = subprocess.run(["git", "rev-parse", "--quiet", "--verify", ref], cwd=repo_path, text=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    return result.returncode == 0


def infer_github_repo(repo_path: Path, remote: str) -> str:
    url = git(["remote", "get-url", remote], repo_path)
    match = re.search(r"github\.com[:/]([^/]+)/(.+?)(?:\.git)?$", url)
    if match:
        return f"{match.group(1)}/{match.group(2)}"
    fail(f"Could not infer GitHub repository from remote URL: {url}. Pass --github-repo owner/name.")


def ensure_tool(name: str) -> None:
    if shutil.which(name) is None:
        fail(f"Required tool not found on PATH: {name}")


def ensure_git_repo(path: Path) -> None:
    if not path.exists():
        fail(f"Repository path does not exist: {path}")
    git(["rev-parse", "--git-dir"], path)


def git(args: list[str], cwd: Path, *, check: bool = True) -> str:
    result = subprocess.run(["git", *args], cwd=cwd, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    if check and result.returncode != 0:
        fail(result.stderr.strip() or f"git {' '.join(args)} failed")
    return result.stdout.strip()


def run(args: list[str], cwd: Path | None, *, execute: bool) -> None:
    if not execute:
        print("$ " + shell_join(args))
        return
    result = subprocess.run(args, cwd=cwd)
    if result.returncode != 0:
        fail(f"Command failed with exit code {result.returncode}: {shell_join(args)}")


def shell_join(args: list[str]) -> str:
    return " ".join(quote(arg) for arg in args)


def quote(value: str) -> str:
    if re.fullmatch(r"[A-Za-z0-9_./:=@%+-]+", value):
        return value
    return "'" + value.replace("'", "'\"'\"'") + "'"


def fail(message: str) -> None:
    print(f"error: {message}", file=sys.stderr)
    raise SystemExit(1)


if __name__ == "__main__":
    raise SystemExit(main())
