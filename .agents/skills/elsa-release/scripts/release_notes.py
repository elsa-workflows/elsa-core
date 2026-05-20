#!/usr/bin/env python3
"""Generate a categorized Markdown scaffold for Elsa release notes."""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path


@dataclass(frozen=True)
class Commit:
    sha: str
    subject: str

    @property
    def short_sha(self) -> str:
        return self.sha[:10]

    @property
    def reference(self) -> str:
        pr = extract_pr_number(self.subject)
        return f"(#{pr})" if pr else f"({self.short_sha})"


SECTIONS: tuple[tuple[str, str], ...] = (
    ("breaking", "## ⚠️ Breaking changes / upgrade notes"),
    ("features", "## ✨ New features"),
    ("improvements", "## 🔧 Improvements"),
    ("fixes", "## 🐛 Fixes"),
    ("security", "## 🔒 Security"),
    ("developer", "## 🧩 Developer-facing changes"),
    ("tests", "## 🧪 Tests"),
    ("ci", "## 🔁 CI / Build"),
    ("dependencies", "## 📦 Dependencies"),
    ("docs", "## Documentation"),
    ("maintenance", "## Maintenance"),
)


def main() -> int:
    args = parse_args()
    repo_path = Path(args.repo_path).expanduser().resolve()
    commits = get_commits(repo_path, args.from_ref, args.to_ref)
    notes = render_notes(args.version, args.from_ref, args.to_ref, commits)

    if args.output:
        output = Path(args.output).expanduser()
        if not output.is_absolute():
            output = repo_path / output
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(notes, encoding="utf-8")
        print(output)
    else:
        print(notes)

    return 0


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo-path", default=".", help="Local repository path.")
    parser.add_argument("--from-ref", required=True, help="Previous tag/ref for the compare range.")
    parser.add_argument("--to-ref", required=True, help="Release tag/ref for the compare range.")
    parser.add_argument("--version", required=True, help="Release version for the title.")
    parser.add_argument("--output", help="Optional Markdown output path.")
    return parser.parse_args()


def get_commits(repo_path: Path, from_ref: str, to_ref: str) -> list[Commit]:
    output = git(
        [
            "log",
            "--reverse",
            "--no-merges",
            "--pretty=format:%H%x1f%s",
            f"{from_ref}..{to_ref}",
        ],
        repo_path,
    )
    commits: list[Commit] = []
    for line in output.splitlines():
        if not line.strip():
            continue
        sha, subject = line.split("\x1f", 1)
        commits.append(Commit(sha=sha, subject=subject))
    return commits


def render_notes(version: str, from_ref: str, to_ref: str, commits: list[Commit]) -> str:
    categorized: dict[str, list[Commit]] = {key: [] for key, _ in SECTIONS}
    for commit in commits:
        categorized[categorize(commit.subject)].append(commit)

    lines: list[str] = [f"Compare: `{from_ref}...{to_ref}`", "", "---", "", "## 🌟 Highlights", ""]

    for commit in select_highlights(commits):
        lines.append(f"- {format_subject(commit.subject)} {commit.reference}")
    if not commits:
        lines.append("- No commits found in the selected range.")

    for key, heading in SECTIONS:
        section_commits = categorized[key]
        if not section_commits:
            continue
        lines.extend(["", "---", "", heading, ""])
        for commit in section_commits:
            lines.append(f"- {format_subject(commit.subject)} {commit.reference}")

    lines.extend(["", "---", "", "## 📦 Full changelog (short)", ""])
    for commit in commits:
        lines.append(f"- {commit.subject} ({commit.short_sha})")

    lines.extend(
        [
            "",
            "<!--",
            "Review before publishing:",
            "- Rewrite bullets so they explain developer impact, not only commit wording.",
            "- Promote the most important user-facing items into Highlights.",
            "- Add migration notes for breaking changes.",
            "- Remove empty or low-value sections.",
            "- Verify PR numbers and do not invent missing context.",
            "-->",
            "",
        ]
    )
    return "\n".join(lines)


def select_highlights(commits: list[Commit]) -> list[Commit]:
    priority = {"breaking": 0, "features": 1, "fixes": 2, "improvements": 3, "security": 4, "developer": 5}
    ranked = sorted(
        commits,
        key=lambda commit: (
            priority.get(categorize(commit.subject), 99),
            is_noise(commit.subject),
            commit.subject.lower(),
        ),
    )
    return ranked[:6]


def categorize(subject: str) -> str:
    lower = subject.lower()
    conventional = lower.split(":", 1)[0]

    if "breaking change" in lower or re.search(r"^[a-z]+(?:\([^)]+\))?!:", lower):
        return "breaking"
    if "security" in lower or "cve-" in lower or "vulnerab" in lower:
        return "security"
    if conventional.startswith("feat") or re.search(r"^(add|added|introduce|introduced|new)\b", lower):
        return "features"
    if conventional.startswith("fix") or contains_any(lower, "fix ", "fixed ", "bug", "issue"):
        return "fixes"
    if conventional.startswith("test") or " test" in lower or lower.startswith("test"):
        return "tests"
    if conventional.startswith("docs") or lower.startswith("doc") or "readme" in lower:
        return "docs"
    if contains_any(lower, "package", "dependency", "dependencies", "nuget", "props"):
        return "dependencies"
    if is_ci_or_build_change(lower):
        return "ci"
    if contains_any(lower, "api", "contract", "extension", "attribute", "options"):
        return "developer"
    if contains_any(lower, "improve", "enhance", "refactor", "optimiz", "cleanup"):
        return "improvements"
    return "maintenance"


def contains_any(value: str, *needles: str) -> bool:
    return any(needle in value for needle in needles)


def is_ci_or_build_change(value: str) -> bool:
    return bool(
        re.search(r"\b(ci|build|pack|versioning)\b", value)
        or "github action" in value
        or "github workflow" in value
        or ".github/workflows" in value
    )


def is_noise(subject: str) -> bool:
    return categorize(subject) in {"tests", "ci", "docs", "maintenance"}


def format_subject(subject: str) -> str:
    value = re.sub(r"\s+\(#\d+\)$", "", subject).strip()
    value = re.sub(r"^[a-z]+(?:\([^)]+\))?!?:\s*", "", value, flags=re.IGNORECASE)
    if not value:
        return subject
    return value[0].upper() + value[1:]


def extract_pr_number(subject: str) -> str | None:
    match = re.search(r"\(#(\d+)\)\s*$", subject)
    if match:
        return match.group(1)
    match = re.search(r"merge pull request #(\d+)", subject, flags=re.IGNORECASE)
    if match:
        return match.group(1)
    return None


def git(args: list[str], cwd: Path) -> str:
    result = subprocess.run(["git", *args], cwd=cwd, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    if result.returncode != 0:
        print(result.stderr.strip() or f"git {' '.join(args)} failed", file=sys.stderr)
        raise SystemExit(result.returncode)
    return result.stdout.strip()


if __name__ == "__main__":
    raise SystemExit(main())
