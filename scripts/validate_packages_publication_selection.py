#!/usr/bin/env python3
"""Reject a manual Packages run whose publication inputs do not fit the dispatched ref.

The validate_publication_selection job in .github/workflows/packages.yml runs this before the package
build and every publisher. The ref and inputs arrive as environment variables, never as script text:

    REF                    github.ref, e.g. refs/heads/main or refs/tags/3.10.0
    PUBLISH_PREVIEW_FEEDZ  inputs.publish_preview_feedz ("true" or "false")
    PUBLISH_NUGET          inputs.publish_nuget
    PUBLISH_NPM            inputs.publish_npm
    DEPLOY_COVERAGE        inputs.deploy_coverage
"""

from __future__ import annotations

import os
import re
import sys

from validate_packages_id_set import STABLE_310_PATTERN, is_stable_310

# Release tags carry no "v" prefix: 3.8.4, 3.8.0-rc2, 3.8.0-preview1.
RELEASE_TAG = re.compile(r"[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z]+(\.[0-9A-Za-z]+)*)?")
# Python source of the 3.10 patch is STABLE_310_PATTERN; this only adds the optional prerelease suffix.
CANONICAL_310 = re.compile(STABLE_310_PATTERN + r"(-[0-9A-Za-z]+(\.[0-9A-Za-z]+)*)?")
# Only these branches upload a Pages artifact that deploy_coverage can deploy.
COVERAGE_REFS = ("refs/heads/main", "refs/heads/release/3.6.1")


def selection_errors(ref: str, *, feedz: bool = False, nuget: bool = False, npm: bool = False, coverage: bool = False) -> list[str]:
    """Return why this selection must not run; an empty list means it may."""
    errors = []
    if ref.startswith("refs/heads/"):
        if nuget:
            errors.append("NuGet.org publication requires a release tag ref, not a branch.")
        if npm:
            errors.append("Studio npm publication requires a release tag ref, not a branch.")
    elif ref.startswith("refs/tags/"):
        tag = ref[len("refs/tags/"):]
        if not RELEASE_TAG.fullmatch(tag):
            return [f"Tag {tag!r} is not a release version such as 3.8.4 or 3.8.0-rc2."]
        # 3.10.x prerelease tags may still publish to Feedz at the tag version.
        # Stable 3.10.x Feedz is owned only by publish-consolidated-original-candidate.yml.
        # nuget.org still accepts any release-version tag, including stable 3.10.x.
        major, minor = (int(part) for part in tag.split(".")[:2])
        feedz_from_tag = (major, minor) == (3, 10)
        if feedz_from_tag and not tag.startswith("3.10."):
            # NuGet would normalize 03.10.0 to 3.10.0, and the Feedz job matches the literal 3.10. prefix.
            return [f"Tag {tag!r} names a 3.10 release without the canonical 3.10. prefix."]
        if feedz_from_tag and not CANONICAL_310.fullmatch(tag):
            return [f"Tag {tag!r} names a 3.10 release without a canonical patch number."]
        if feedz and is_stable_310(tag):
            errors.append("Feedz publication for stable 3.10.x is owned by publish-consolidated-original-candidate.yml.")
        elif feedz and not feedz_from_tag:
            errors.append("Feedz publication from a tag is limited to 3.10.x prerelease tags; publish previews from a branch.")
    else:
        return [f"Ref {ref!r} is neither a branch nor a tag."]
    if coverage and ref not in COVERAGE_REFS:
        errors.append(f"Coverage deployment requires {' or '.join(COVERAGE_REFS)}.")
    return errors


def _flag(name: str) -> bool:
    value = os.environ.get(name, "")
    if value not in ("true", "false"):
        raise SystemExit(f"{name} must be 'true' or 'false', got {value!r}.")
    return value == "true"


def main() -> int:
    ref = os.environ.get("REF", "")
    errors = selection_errors(
        ref,
        feedz=_flag("PUBLISH_PREVIEW_FEEDZ"),
        nuget=_flag("PUBLISH_NUGET"),
        npm=_flag("PUBLISH_NPM"),
        coverage=_flag("DEPLOY_COVERAGE"),
    )
    for error in errors:
        print(error, file=sys.stderr)
    if errors:
        return 1
    print(f"Publication selection accepted for {ref}.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
