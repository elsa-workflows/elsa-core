#!/usr/bin/env python3
"""Verify the complete product relocation inventory; this does not run builds."""
from __future__ import annotations

import json
from pathlib import Path, PurePosixPath
import subprocess

from product_layout import map_path

ROOT = Path(__file__).resolve().parents[2]


def require(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(message)


def git(*arguments: str) -> bytes:
    return subprocess.check_output(["git", "-C", str(ROOT), *arguments])


def tree(revision: str) -> dict[str, tuple[str, str]]:
    result = {}
    for entry in git("ls-tree", "-rz", "--full-tree", revision).split(b"\0"):
        if not entry:
            continue
        metadata, path = entry.split(b"\t", 1)
        mode, kind, oid = metadata.decode().split()
        require(kind == "blob", "Unexpected non-file baseline entry")
        result[path.decode()] = (mode, oid)
    return result


def verify() -> dict:
    config = json.loads(Path(__file__).with_name("product-layout.json").read_text())
    baseline = config["baselineCommit"]
    relocation = config["relocationCommit"]
    for revision in (baseline, relocation):
        subprocess.run(["git", "-C", str(ROOT), "merge-base", "--is-ancestor", revision, "HEAD"], check=True)
    require(git("rev-parse", baseline + "^{tree}").decode().strip() == config["baselineTree"],
            "Baseline tree identity changed")
    original, moved = tree(baseline), tree(relocation)
    require(len(original) == config["baselineEntries"], "Baseline inventory changed")
    current = {}
    for entry in git("ls-files", "--stage", "-z").split(b"\0"):
        if not entry:
            continue
        metadata, path = entry.split(b"\t", 1)
        mode, oid, stage = metadata.decode().split()
        require(stage == "0", "Unresolved index entry")
        current[path.decode()] = (mode, oid)

    destinations = {}
    changed = []
    for old, identity in original.items():
        explicit = old in config["exactMoves"] or any(old.startswith(prefix) for prefix, _ in config["prefixMoves"])
        require(explicit or old.split("/")[0] in config["sharedRootEntries"], "Unclassified baseline path: " + old)
        new = map_path(old)
        path = PurePosixPath(new)
        require(not path.is_absolute() and ".." not in path.parts, "Unsafe destination: " + new)
        require(new.casefold() not in destinations, "Destination collision: " + new)
        destinations[new.casefold()] = new
        require(moved.get(new) == identity, "Pure move changed content or mode: " + new)
        require(new in current and current[new][0] == identity[0], "Missing destination or changed mode: " + new)
        require((ROOT / new).is_file() or (ROOT / new).is_symlink(), "Missing working file: " + new)
        require(old == new or old not in current, "Retired source remains active: " + old)
        if current[new][1] != identity[1]:
            changed.append(new)
    metadata_path = "scripts/integration-program/product-layout.json"
    require(set(moved) == set(destinations.values()) | {metadata_path},
            "Pure move contains unexpected files beyond the relocation manifest")
    require(moved[metadata_path][0] == "100644", "Relocation manifest mode changed")
    original_config = json.loads(git("show", relocation + ":" + metadata_path))
    expected_config = {key: value for key, value in config.items() if key != "relocationCommit"}
    require(original_config == expected_config, "Relocation manifest differs from its original mapping")
    for key in destinations:
        parts = key.split("/")
        require(not any("/".join(parts[:n]) in destinations for n in range(1, len(parts))),
                "File/directory collision: " + destinations[key])
    retired = ("src/", "test/", "doc/", "specs/", "samples/", "gen/", "announcements/")
    require(not any(path.startswith(retired) for path in current), "Retired active root remains")
    for product in ("core", "extensions", "studio"):
        for child in ("src", "test", "docs"):
            require(any(path.startswith(product + "/" + child + "/") for path in current),
                    "Missing product directory: " + product + "/" + child)
    return {"baselineCommit": baseline, "relocationCommit": relocation,
            "baselineFiles": len(original), "mappedFilesPresent": len(destinations),
            "pureMoveBlobsAndModesPreserved": True, "currentTrackedFiles": len(current),
            "subsequentContentChanges": sorted(changed),
            "scope": "Git index inventory and exact pure-move provenance; working content and build/runtime acceptance are separate"}


if __name__ == "__main__":
    print(json.dumps(verify(), indent=2))
