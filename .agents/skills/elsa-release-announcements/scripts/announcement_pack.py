#!/usr/bin/env python3
"""Create a channel-specific Elsa release announcement draft pack."""

from __future__ import annotations

import argparse
import re
from pathlib import Path


def main() -> int:
    args = parse_args()
    notes = read_notes(args.notes_file)
    highlights = extract_highlights(notes)
    links = [args.release_url, *args.package_url, *args.docs_url]

    pack = render_pack(
        product=args.product,
        version=args.version,
        release_kind=args.release_kind,
        release_url=args.release_url,
        package_urls=args.package_url,
        docs_urls=args.docs_url,
        highlights=highlights,
    )

    if args.output:
        output = Path(args.output).expanduser()
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(pack, encoding="utf-8")
        print(output)
    else:
        print(pack)

    return 0


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--product", required=True, help="Product name, e.g. Elsa Core.")
    parser.add_argument("--version", required=True, help="Release version.")
    parser.add_argument("--release-kind", required=True, choices=("stable", "preview", "rc"), help="Release kind.")
    parser.add_argument("--release-url", required=True, help="GitHub release URL.")
    parser.add_argument("--notes-file", help="Curated release notes Markdown file.")
    parser.add_argument("--package-url", action="append", default=[], help="Package/feed URL. Can be repeated.")
    parser.add_argument("--docs-url", action="append", default=[], help="Docs or migration URL. Can be repeated.")
    parser.add_argument("--output", help="Optional output Markdown path.")
    return parser.parse_args()


def read_notes(path: str | None) -> str:
    if not path:
        return ""
    return Path(path).expanduser().read_text(encoding="utf-8")


def extract_highlights(notes: str) -> list[str]:
    if not notes:
        return []

    section_match = re.search(r"^##\s+.*Highlights\s*$([\s\S]*?)(?:^---$|^##\s+)", notes, flags=re.MULTILINE)
    source = section_match.group(1) if section_match else notes
    highlights: list[str] = []
    for line in source.splitlines():
        stripped = line.strip()
        if stripped.startswith("- "):
            highlights.append(clean_markdown(stripped[2:]))
        if len(highlights) == 5:
            break
    return highlights


def clean_markdown(value: str) -> str:
    value = re.sub(r"\[[^\]]+\]\(([^)]+)\)", r"\1", value)
    value = value.replace("`", "")
    return re.sub(r"\s+", " ", value).strip()


def render_pack(
    *,
    product: str,
    version: str,
    release_kind: str,
    release_url: str,
    package_urls: list[str],
    docs_urls: list[str],
    highlights: list[str],
) -> str:
    availability = availability_text(release_kind, package_urls)
    highlight_text = "\n".join(f"- {highlight}" for highlight in highlights) if highlights else "- Add the top release highlights here after reviewing the release notes."
    link_text = "\n".join(f"- {url}" for url in [release_url, *package_urls, *docs_urls])
    stable_label = "stable " if release_kind == "stable" else f"{release_kind} "

    discord = render_discord(
        product=product,
        version=version,
        release_kind=release_kind,
        release_url=release_url,
        availability=availability,
        highlights=highlights,
    )

    linkedin = f"""\
{product} {version} is now available.

This {stable_label}release includes important improvements for developers building with Elsa. Highlights include:

{highlight_text}

Read the full release notes: {release_url}
"""

    x_single = f"""\
{product} {version} is now available.

{availability}

Highlights:
{compact_highlights(highlights)}

Release notes: {release_url}
"""

    x_thread = f"""\
1/{product} {version} is now available.

{availability}

Release notes: {release_url}

2/Highlights:
{compact_highlights(highlights)}

3/Upgrade notes, package links, and the full changelog are in the release notes.
"""

    return "\n".join(
        [
            f"# {product} {version} Announcement Pack",
            "",
            "## Facts",
            "",
            f"- Product: {product}",
            f"- Version: {version}",
            f"- Release kind: {release_kind}",
            f"- Release URL: {release_url}",
            "",
            "## Discord",
            "",
            discord.strip(),
            "",
            "## LinkedIn",
            "",
            linkedin.strip(),
            "",
            "## X single-post option",
            "",
            x_single.strip(),
            "",
            "## X thread option",
            "",
            x_thread.strip(),
            "",
            "## Links",
            "",
            link_text,
            "",
            "<!-- Review, tighten, and approve before publishing. -->",
            "",
        ]
    )


def availability_text(release_kind: str, package_urls: list[str]) -> str:
    if release_kind == "stable" and package_urls:
        return "Packages are available on the configured feeds."
    if release_kind == "stable":
        return "Packages are available once the release pipeline has completed."
    if release_kind == "preview":
        return "This is a preview release intended for early validation."
    return "This is a release candidate intended for final validation before stable release."


def render_discord(
    *,
    product: str,
    version: str,
    release_kind: str,
    release_url: str,
    availability: str,
    highlights: list[str],
) -> str:
    heading = discord_heading(product, version, release_kind)
    intro = discord_intro(product, version, release_kind)
    highlight_text = render_discord_highlights(highlights)
    upgrade_notes = render_upgrade_notes(release_kind)
    validation = render_validation_note(release_kind, version)

    return f"""\
:rocket: **{heading}**

{intro}

:point_right: Release notes: <{release_url}>

{availability}

### :sparkles: Highlights

{highlight_text}

{upgrade_notes}

{validation}
"""


def discord_heading(product: str, version: str, release_kind: str) -> str:
    if release_kind == "stable":
        return f"{product} {version} is here!"
    if release_kind == "preview":
        return f"{product} {version} preview is here!"
    return f"{product} {version} RC is here!"


def discord_intro(product: str, version: str, release_kind: str) -> str:
    if release_kind == "stable":
        return f"We've published the stable **{product} {version}** release."
    if release_kind == "preview":
        return f"We've published a preview release for **{product} {version}** for early testing and feedback."
    return f"We've published a release candidate for **{product} {version}**."


def render_discord_highlights(highlights: list[str]) -> str:
    if not highlights:
        return """:compass: **Release improvements**
Add the top release highlights here after reviewing the release notes."""

    icons = [
        ":closed_lock_with_key:",
        ":compass:",
        ":art:",
        ":jigsaw:",
        ":zap:",
        ":bug:",
    ]
    lines: list[str] = []
    for index, highlight in enumerate(highlights[:6]):
        icon = icons[index % len(icons)]
        title, _, details = highlight.partition(":")
        if details:
            lines.append(f"{icon} **{title.strip()}**\n{details.strip()}")
        else:
            lines.append(f"{icon} **{highlight}**")
    return "\n\n".join(lines)


def render_upgrade_notes(release_kind: str) -> str:
    heading = "### :tools: Upgrade notes"
    if release_kind == "stable":
        body = """Review the full release notes before upgrading, especially if you host Elsa Studio yourself, customize Studio components, use custom authentication, or integrate with secured Elsa APIs."""
    else:
        body = """There may be compatibility changes to validate before production use. Pay special attention if you host Elsa Studio yourself, customize Studio components, use custom authentication, or integrate with secured Elsa APIs."""
    return f"{heading}\n{body}"


def render_validation_note(release_kind: str, version: str) -> str:
    if release_kind == "stable":
        release_line = ".".join(version.split(".")[:2]) if "." in version else version
        return f"""### :raised_hands: Feedback welcome
Please report issues, regressions, or upgrade notes you run into so we can keep improving the {release_line} line."""

    return """### :test_tube: Please test it
This release is intended for testing and validation before the final stable release. Feedback, bug reports, and PRs are very welcome :raised_hands:"""


def compact_highlights(highlights: list[str]) -> str:
    if not highlights:
        return "- See the release notes for details."
    return "\n".join(f"- {highlight}" for highlight in highlights[:3])


if __name__ == "__main__":
    raise SystemExit(main())
