#!/usr/bin/env python3
"""Select CI suites using the complete Git diff and the former workflow path filters."""
import json
import os
from pathlib import Path
import re
import subprocess


ROUTES = Path(__file__).with_name("pr-paths.json")
CI_PATHS = (".github/workflows/pr.yml", "scripts/ci/**")
GLOB_TOKENS = re.compile(r"\*\*/|\*\*|\*|\[[^\]]+\]|.", re.DOTALL)
GLOB_REGEX = {"**/": "(?:.*/)?", "**": ".*", "*": "[^/]*"}


def pattern_regex(pattern):
    # In Actions, * stops at a slash and **/ also matches zero directories.
    return re.compile("".join(
        GLOB_REGEX[token] if token in GLOB_REGEX else
        token if token.startswith("[") and token.endswith("]") else re.escape(token)
        for token in GLOB_TOKENS.findall(pattern)
    ), re.DOTALL)


def matches_path(path, patterns):
    matched = False
    for include, pattern in patterns:
        if pattern.fullmatch(path):
            matched = include
    return matched


def select_suites(routes, event, branch, paths=(), suite=None):
    if event == "workflow_dispatch":
        if suite not in routes or not routes[suite]["manual"]:
            raise ValueError(f"Unknown manual CI suite: {suite}")
        return {key: key == suite for key in routes}

    ci_patterns = [pattern_regex(glob) for glob in CI_PATHS]
    ci_changed = paths is None or any(pattern.fullmatch(path) for path in paths for pattern in ci_patterns)
    selected = {}
    for key, route in routes.items():
        patterns = [(not glob.startswith("!"), pattern_regex(glob.removeprefix("!"))) for glob in route["paths"]]
        eligible = (
            route.get("push", False) if event == "push"
            else not route.get("branches") or any(pattern_regex(glob).fullmatch(branch) for glob in route["branches"])
        )
        selected[key] = bool(eligible and (ci_changed or any(matches_path(path, patterns) for path in paths)))
    return selected


def changed_paths(event, payload):
    # GitHub checks out the synthetic merge commit for PRs. Diff its first
    # parent to include deletions and both sides of renames, even on large PRs.
    base = "HEAD^1" if event == "pull_request" else payload["before"]
    if event == "push":
        if base == "0" * 40:
            return None
        exists = subprocess.run(["git", "cat-file", "-e", f"{base}^{{commit}}"], capture_output=True)
        if exists.returncode:
            fetched = subprocess.run(["git", "fetch", "--no-tags", "--depth=1", "origin", base])
            if fetched.returncode:
                print("::warning::Previous push commit is unavailable. Running all push suites.")
                return None
    diff = subprocess.check_output(["git", "diff", "--no-renames", "--name-only", "-z", base, "HEAD", "--"])
    return [os.fsdecode(path) for path in diff.split(b"\0") if path]


def main():
    event = os.environ["GITHUB_EVENT_NAME"]
    payload = json.loads(Path(os.environ["GITHUB_EVENT_PATH"]).read_bytes())
    branch = payload["pull_request"]["base"]["ref"] if event == "pull_request" else os.environ["GITHUB_REF_NAME"]
    paths = () if event == "workflow_dispatch" else changed_paths(event, payload)
    routes = json.loads(ROUTES.read_bytes())
    suites = select_suites(routes, event, branch, paths, os.environ.get("CI_SUITE"))
    print("Selected CI suites:", ", ".join(key for key, enabled in suites.items() if enabled) or "none")
    with Path(os.environ["GITHUB_OUTPUT"]).open("a", encoding="utf-8") as output:
        print("suites=" + json.dumps(suites, separators=(",", ":")), file=output)


if __name__ == "__main__":
    main()
