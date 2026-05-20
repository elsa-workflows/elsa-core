#!/usr/bin/env python3
"""Prepare or execute Elsa-style GitHub releases from Git tags."""

from __future__ import annotations

import argparse
import re
import shutil
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path


@dataclass(frozen=True)
class Command:
    args: list[str]
    cwd: Path | None = None


def main() -> int:
    args = parse_args()
    repo_path = Path(args.repo_path).expanduser().resolve()
    ensure_tool("git")
    ensure_tool("gh")
    ensure_git_repo(repo_path)

    remote = args.remote
    github_repo = args.github_repo or infer_github_repo(repo_path, remote)

    fetch_command = ["git", "fetch", "--tags", "--prune", remote]
    print("$ " + shell_join(fetch_command), flush=True)
    run(fetch_command, cwd=repo_path, execute=True)

    source_ref = args.source_ref or "HEAD"
    source_commit = git(["rev-parse", f"{source_ref}^{{commit}}"], repo_path)
    containing_branches = remote_branches_containing(repo_path, source_commit)
    validate_containing_branches(containing_branches, args.allow_uncontained)

    tag_exists_local = ref_exists(repo_path, f"refs/tags/{args.tag}")
    tag_exists_remote = remote_tag_exists(repo_path, remote, args.tag)
    if tag_exists_local or tag_exists_remote:
        existing_commit = git(["rev-list", "-n", "1", f"{args.tag}^{{commit}}"], repo_path, check=False)
        if existing_commit != source_commit:
            fail(f"Tag {args.tag} already exists and points to {existing_commit or 'an unknown commit'}, not {source_commit}.")
        print(f"Tag {args.tag} already exists at the requested commit; the helper will reuse it.")

    title = args.title or args.tag
    commands = build_commands(
        repo_path=repo_path,
        remote=remote,
        github_repo=github_repo,
        tag=args.tag,
        source_commit=source_commit,
        title=title,
        release_kind=args.release_kind,
        notes_file=args.notes_file,
        notes_start_tag=args.notes_start_tag,
        tag_exists_local=tag_exists_local,
        tag_exists_remote=tag_exists_remote,
    )

    print_summary(
        repo_path=repo_path,
        github_repo=github_repo,
        source_ref=source_ref,
        source_commit=source_commit,
        tag=args.tag,
        release_kind=args.release_kind,
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
    parser.add_argument("--remote", default="origin", help="Git remote to fetch and push tags to.")
    parser.add_argument("--source-ref", help="Existing tag, branch, or commit for the release tag. Defaults to HEAD.")
    parser.add_argument("--tag", required=True, help="Desired release tag, e.g. 3.7.0 or 3.8.0-preview2.")
    parser.add_argument("--release-kind", required=True, choices=("stable", "preview"), help="Stable creates a normal release; preview creates a prerelease.")
    parser.add_argument("--title", help="GitHub release title. Defaults to the tag.")
    parser.add_argument("--notes-file", help="Release notes Markdown file. Defaults to GitHub generated notes.")
    parser.add_argument("--notes-start-tag", help="Starting tag for GitHub generated release notes.")
    parser.add_argument("--allow-uncontained", action="store_true", help="Allow source commits not contained in origin/main or origin/release/*.")
    parser.add_argument("--execute", action="store_true", help="Create/push the tag and publish the GitHub release.")
    return parser.parse_args()


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
) -> list[Command]:
    commands: list[Command] = []

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
        "--title",
        title,
    ]

    if notes_file:
        release.extend(["--notes-file", notes_file])
    else:
        release.append("--generate-notes")
        if notes_start_tag:
            release.extend(["--notes-start-tag", notes_start_tag])

    if release_kind == "preview":
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


def validate_containing_branches(branches: list[str], allow_uncontained: bool) -> None:
    if allow_uncontained:
        return

    for branch in branches:
        normalized = branch.strip()
        if normalized == "origin/main" or normalized.startswith("origin/release/"):
            return

    fail("Source commit is not contained in origin/main or origin/release/*. Use --allow-uncontained only after reviewing the workflow risk.")


def remote_branches_containing(repo_path: Path, commit: str) -> list[str]:
    output = git(["branch", "--remote", "--contains", commit], repo_path)
    return [line.strip().lstrip("* ").strip() for line in output.splitlines() if line.strip()]


def ref_exists(repo_path: Path, ref: str) -> bool:
    result = subprocess.run(["git", "rev-parse", "--quiet", "--verify", ref], cwd=repo_path, text=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    return result.returncode == 0


def remote_tag_exists(repo_path: Path, remote: str, tag: str) -> bool:
    result = subprocess.run(["git", "ls-remote", "--exit-code", "--tags", remote, f"refs/tags/{tag}"], cwd=repo_path, text=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
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
