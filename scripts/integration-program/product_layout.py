"""Resolve historical repository paths into the current physical product layout.

Only current checkout consumers should call this helper. Pinned upstream Git paths,
original import manifests and historical receipts retain their original identities.
"""
from __future__ import annotations

import hashlib
import json
import subprocess
from functools import lru_cache
from pathlib import Path


LAYOUT = json.loads(Path(__file__).with_name("product-layout.json").read_text(encoding="utf-8"))


def map_path(path: str) -> str:
    """Apply exact overrides, then the first matching prefix; current paths are unchanged."""
    if type(path) is not str:
        raise ValueError("Product path must be a string")
    if path in LAYOUT["exactMoves"]:
        return LAYOUT["exactMoves"][path]
    for old, new in LAYOUT["prefixMoves"]:
        if path.startswith(old):
            return new + path[len(old):]
    return path


def current_path(root: Path, path: str) -> Path:
    """Resolve current monorepo paths without changing separate historical checkouts."""
    if (root / "scripts/integration-program/product-layout.json").is_file():
        path = map_path(path)
    return root / path


# Finite, reviewed path-only dispositions for shared tooling whose source equivalence
# was recorded before relocation. These do not permit arbitrary edits to current files.
_SPEC_PATHS = (
    ".specify/extensions/git/README.md",
    ".specify/extensions/git/commands/speckit.git.initialize.md",
    ".specify/extensions/git/commands/speckit.git.validate.md",
    ".specify/templates/plan-template.md",
    ".specify/templates/tasks-template.md",
    ".agents/skills/speckit-git-initialize/SKILL.md",
    ".agents/skills/speckit-git-validate/SKILL.md",
    ".agents/skills/speckit-specify/SKILL.md",
)
_PATH_TRANSFORMS = {path: ((b"specs/", b"core/docs/specs/"),) for path in _SPEC_PATHS}
_PATH_TRANSFORMS.update({path: (
    (b"$repo_root/specs", b"$repo_root/core/docs/specs"),
    (b"$REPO_ROOT/specs", b"$REPO_ROOT/core/docs/specs"),
    (b"Join-Path $repoRoot 'specs'", b"Join-Path $repoRoot 'core/docs/specs'"),
) for path in (
    ".specify/scripts/bash/common.sh", ".specify/scripts/bash/create-new-feature.sh",
    ".specify/extensions/git/scripts/bash/create-new-feature.sh",
    ".specify/extensions/git/scripts/powershell/create-new-feature.ps1",
)})
_PATH_TRANSFORMS[".specify/memory/constitution.md"] = tuple(
    (prefix.encode(), ("core/" + prefix).encode()) for prefix in
    ("src/modules/", "src/common/", "src/apps/", "test/unit/", "test/integration/", "test/component/"))
_PATH_TRANSFORMS[".github/dependabot.yml"] = (
    (b"src/extensions", b"extensions/src"), (b"src/studio", b"studio/src"))

_PATH_TRANSFORMS["src/studio/AGENTS.md"] = (
    (b"These instructions apply to Studio modules, hosts, frameworks, and tests under\n"
     b"`src/studio/`. Follow the repository-root `AGENTS.md` for shared build, review,\n"
     b"testing, package and ADR\nrules. This guidance carries the Studio-specific rules from the pinned\n",
     b"These instructions apply to Studio modules, hosts, frameworks and colocated\n"
     b".NET tests under `studio/src/`, and browser fixtures under `studio/test/`.\n"
     b"Follow the repository-root `AGENTS.md` for shared build, review, testing,\n"
     b"package and ADR rules. This guidance carries the Studio-specific rules from the pinned\n"),
    (b"src/studio/", b"studio/src/"),
    (b"../../doc/studio/README.md", b"../docs/README.md"),
    (b"- Keep Studio feature-specific requirements, plans, and tasks under that\n"
     b"  feature's `specs/` directory. Do not change the repository-root\n",
     b"- Keep Studio feature-specific requirements, plans, and tasks under\n"
     b"  `studio/docs/specs/<feature>/`. Do not change the repository-root\n"),
)


@lru_cache(maxsize=32)
def relocation_baseline(git_root: Path, path: str) -> tuple[bytes, str]:
    before = subprocess.check_output(["git", "show", f"{LAYOUT['baselineCommit']}:{path}"], cwd=git_root)
    mode = subprocess.check_output(["git", "ls-tree", LAYOUT["baselineCommit"], "--", path],
                                   cwd=git_root, text=True).split()[0]
    return before, mode


def verified_relocation_identity(git_root: Path, path: str, current: Path) -> tuple[str, str] | None:
    """Return original identity only when current bytes/mode equal a reviewed path transform."""
    if path not in _PATH_TRANSFORMS or current.is_symlink() or not current.is_file():
        return None
    before, mode = relocation_baseline(git_root, path)
    after = before
    for old, new in _PATH_TRANSFORMS[path]:
        after = after.replace(old, new)
    actual_mode = "100755" if current.stat().st_mode & 0o111 else "100644"
    if before == after or current.read_bytes() != after or actual_mode != mode:
        return None
    return hashlib.sha1(f"blob {len(before)}\0".encode() + before).hexdigest(), mode
