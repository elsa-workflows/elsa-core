#!/usr/bin/env python3
"""Retained provenance guard for #8840; adaptations may follow the immutable import."""

import argparse
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[2]
RECEIPT = "docs/integration-program/studio-react-source-import.json"


def git(root, *args):
    return subprocess.check_output(["git", *args], cwd=root, text=True, stderr=subprocess.PIPE).strip()


def require(condition, message):
    if not condition:
        raise ValueError(message)


def verify(root, receipt):
    source, imported = receipt["sourceCommit"], receipt["importCommit"]
    base, prefix = receipt["coreBase"], receipt["destination"]
    parents = git(root, "show", "-s", "--format=%P", imported).split()
    require(parents == [base, source], "Import must preserve Core and the original donor as its two parents")
    require(git(root, "rev-parse", source + "^{tree}") == receipt["sourceTree"]
            == git(root, "rev-parse", f"{imported}:{prefix}"), "Imported donor tree differs from the source")
    changes = git(root, "diff", "--name-only", base, imported).splitlines()
    require(changes and all(path.startswith(prefix + "/") for path in changes),
            "Import changes files outside the destination")
    git(root, "merge-base", "--is-ancestor", imported, "HEAD")
    require(int(git(root, "rev-list", "--count", source)) == receipt["sourceHistoryCommits"],
            "Source history count differs")
    snapshot = receipt["snapshotCommit"]
    git(root, "merge-base", "--is-ancestor", snapshot, source)
    subsequent = git(root, "rev-list", "--reverse", f"{snapshot}..{source}").splitlines()
    require(subsequent == receipt["postSnapshotCommits"], "Post-snapshot work is missing from the receipt")
    for ref in receipt["sourceRefs"]:
        git(root, "merge-base", "--is-ancestor", ref["commit"], imported)
    print(f"Verified original React Studio tree, {receipt['sourceHistoryCommits']} commits and post-snapshot ancestry")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=ROOT)
    args = parser.parse_args()
    verify(args.root, json.loads((args.root / RECEIPT).read_text()))
