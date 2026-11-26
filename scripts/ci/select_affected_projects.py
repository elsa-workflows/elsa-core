#!/usr/bin/env python3
"""Decide whether PR checks build everything, only the affected projects, or nothing.

  base    prints base=<sha> (or base=) for GITHUB_OUTPUT: the commit to compare with.
  decide  prints build=all|affected|none for GITHUB_OUTPUT. For affected it also writes
          affected.slnx, because Nuke can't load the .slnf that dotnet-affected writes.

See https://stenbrinke.nl/blog/build-only-what-changed-in-your-dotnet-monorepo/
"""
import json
import os
from pathlib import Path
import subprocess
import sys
import urllib.parse
import urllib.request

from select_pr_suites import pattern_regex

WORKFLOW = "pr.yml"
AFFECTED_JOB = "Select affected projects"
NOTHING_AFFECTED = 166  # dotnet-affected's exit code when no project is affected

# Files the project graph can't see, or that change how everything builds.
ALWAYS_BUILD = [pattern_regex(glob) for glob in (
    ".github/workflows/*",
    "global.json",
    "**/[Nn]u[Gg]et.[Cc]onfig",
    "**/.editorconfig",
    "**/Directory.Build.*",
    "**/*.props",
    "**/*.targets",
    "Elsa.sln",
    ".config/dotnet-tools.json",
    "build/**",
    ".nuke/**",
)]


def pick_base(event, payload, last_verified_push):
    """The commit to compare with, or None to build everything."""
    if event == "pull_request":
        return payload["pull_request"]["base"]["sha"]
    if event == "merge_group":
        # Not reachable yet: pr.yml has no merge_group trigger. When it gets one,
        # select_pr_suites.py needs merge_group support too (it reads payload["before"]).
        return payload["merge_group"]["base_sha"]
    if event == "push":
        return last_verified_push()
    return None  # workflow_dispatch and anything else


def last_verified_push(get_json, repository, branch):
    """Head of the newest successful push run on this branch whose affected job succeeded.

    A green run where compile wasn't selected (docs only, say) proves nothing about the
    code, so it doesn't count. Returns None when no run qualifies or the lookup fails.
    """
    try:
        runs = get_json(f"repos/{repository}/actions/workflows/{WORKFLOW}/runs"
                        f"?branch={urllib.parse.quote(branch)}&event=push&status=success&per_page=30")["workflow_runs"]
        for run in runs:
            jobs = get_json(f"repos/{repository}/actions/runs/{run['id']}/jobs?per_page=100")["jobs"]
            if any(job["name"] == AFFECTED_JOB and job["conclusion"] == "success" for job in jobs):
                return run["head_sha"]
    except Exception as error:  # any lookup problem means: build everything
        print(f"::warning::Couldn't find the last verified push run: {error}")
    return None


def decide(changes, run_affected):
    """changes: (status, path) pairs from git diff --name-status. Returns (build, reason)."""
    hits = [path for _, path in changes if any(glob.fullmatch(path) for glob in ALWAYS_BUILD)]
    if hits:
        return "all", "a file on the always-build list changed: " + " ".join(hits)
    # dotnet-affected can't attribute a deleted file that another project linked in
    # (for example core/test/unit/Shared/FastEndpoints/*.cs), so any deletion builds everything.
    deleted = [path for status, path in changes if status == "D"]
    if deleted:
        return "all", "files were deleted: " + " ".join(deleted)
    rc = run_affected()
    if rc == 0:
        return "affected", "dotnet-affected found affected projects"
    if rc == NOTHING_AFFECTED:
        return "none", "no project in Elsa.sln is affected"
    raise SystemExit(f"dotnet-affected failed with exit code {rc}")


def git(*args, check=True):
    return subprocess.run(["git", *args], check=check, capture_output=True, text=True)


def github_get_json(path):
    request = urllib.request.Request(f"https://api.github.com/{path}", headers={
        "Authorization": f"Bearer {os.environ['GH_TOKEN']}",
        "Accept": "application/vnd.github+json",
    })
    with urllib.request.urlopen(request, timeout=30) as response:
        return json.load(response)


def main_base():
    event = os.environ["GITHUB_EVENT_NAME"]
    payload = json.loads(Path(os.environ["GITHUB_EVENT_PATH"]).read_bytes())
    base = pick_base(event, payload, lambda: last_verified_push(
        github_get_json, os.environ["GITHUB_REPOSITORY"], os.environ["GITHUB_REF_NAME"]))
    print(f"Base for {event}: {base or 'none'}", file=sys.stderr)
    print(f"base={base or ''}")


def main_decide():
    base = os.environ.get("BASE", "")
    if not base:
        return "all", "no base commit to compare with"
    if git("fetch", "--no-tags", "--depth=1", "origin", base, check=False).returncode:
        return "all", f"base commit {base} can't be fetched"
    print(f"Comparing {base}..HEAD", file=sys.stderr)
    diff = git("diff", "--no-renames", "--name-status", "-z", base, "HEAD").stdout.split("\0")
    changes = list(zip(diff[0::2], diff[1::2]))

    def run_affected():
        return subprocess.run(["dotnet", "affected", "--from", base, "--uncommitted", "none",
                               "--filter-file-path", "Elsa.sln", "--format", "text",
                               "--output-name", "affected"], stdout=sys.stderr).returncode

    build, reason = decide(changes, run_affected)
    if build == "affected":
        write_solution(Path("affected.txt"), Path("affected.slnx"))
    return build, reason


def write_solution(affected_txt, slnx):
    root = os.getcwd() + "/"
    projects = [line.removeprefix(root) for line in affected_txt.read_text().splitlines() if line]
    subprocess.run(["dotnet", "new", "sln", "--name", slnx.stem, "--format", "slnx"], check=True, stdout=sys.stderr)
    # No --in-root: two projects share a name (Elsa.Server.Web), so they need their folders.
    subprocess.run(["dotnet", "sln", str(slnx), "add", "--include-references", "false", *projects], check=True, stdout=sys.stderr)
    added = slnx.read_text().count("<Project ")
    if added != len(projects):
        raise SystemExit(f"{slnx} has {added} projects, expected {len(projects)}")
    print(f"Affected projects ({len(projects)}):", *projects, sep="\n  ", file=sys.stderr)


if __name__ == "__main__":
    if sys.argv[1:] == ["base"]:
        main_base()
    elif sys.argv[1:] == ["decide"]:
        build, reason = main_decide()
        print(f"Build {build}: {reason}", file=sys.stderr)
        print(f"build={build}")
    else:
        raise SystemExit("usage: select_affected_projects.py base|decide")
