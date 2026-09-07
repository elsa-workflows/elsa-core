"""Shared version parsing and release-kind support for the Elsa release helper."""

from __future__ import annotations

import re
from dataclasses import dataclass


_SEMVER = re.compile(
    r"^(?P<major>0|[1-9][0-9]*)\."
    r"(?P<minor>0|[1-9][0-9]*)\."
    r"(?P<patch>0|[1-9][0-9]*)"
    r"(?:-(?P<prerelease>[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?$"
)
_NUMERIC_IDENTIFIER = re.compile(r"0|[1-9][0-9]*$")
_RC_TOKEN = re.compile(r"rc(?:0|[1-9][0-9]*)$")
_RC_DOTTED_TOKEN = re.compile(r"rc\.(?:0|[1-9][0-9]*)$")
_PREVIEW_TOKEN = re.compile(r"preview(?:0|[1-9][0-9]*)$")
_PREVIEW_DOTTED_TOKEN = re.compile(r"preview\.(?:0|[1-9][0-9]*)$")


@dataclass(frozen=True)
class ReleaseVersion:
    """A validated release version and its Elsa release channel."""

    version: str
    base: str
    kind: str

    @property
    def prerelease(self) -> bool:
        return self.kind != "stable"

    @property
    def npm_tag(self) -> str:
        return "next" if self.prerelease else "latest"


def parse_version(value: str, kind: str | None = None) -> ReleaseVersion:
    """Parse a strict SemVer release tag and infer or validate its kind."""

    if not isinstance(value, str) or not value:
        raise ValueError("release version must be a non-empty string")
    if "+" in value:
        raise ValueError(f"build metadata is not supported in release version: {value}")

    match = _SEMVER.fullmatch(value)
    if match is None:
        raise ValueError(f"invalid SemVer release version: {value}")

    requested_kind = kind.lower() if isinstance(kind, str) else None
    if requested_kind is not None and requested_kind not in {"stable", "rc", "preview"}:
        raise ValueError(f"invalid release kind: {kind}")

    prerelease = match.group("prerelease")
    base = ".".join(match.group(name) for name in ("major", "minor", "patch"))
    if prerelease is None:
        inferred_kind = "stable"
    else:
        _validate_prerelease_identifiers(prerelease)
        inferred_kind = _infer_prerelease_kind(prerelease)

    if requested_kind is not None and requested_kind != inferred_kind:
        raise ValueError(
            f"release version {value} has kind {inferred_kind}, not requested {requested_kind}"
        )

    return ReleaseVersion(version=value, base=base, kind=requested_kind or inferred_kind)


def _validate_prerelease_identifiers(prerelease: str) -> None:
    for identifier in prerelease.split("."):
        if identifier.isdigit() and _NUMERIC_IDENTIFIER.fullmatch(identifier) is None:
            raise ValueError(f"invalid SemVer prerelease identifier: {prerelease}")


def _infer_prerelease_kind(prerelease: str) -> str:
    if prerelease in {"rc", "preview"}:
        raise ValueError("choose an explicit unused RC or preview number before release")
    if _RC_TOKEN.fullmatch(prerelease) or _RC_DOTTED_TOKEN.fullmatch(prerelease):
        return "rc"
    if _PREVIEW_TOKEN.fullmatch(prerelease) or _PREVIEW_DOTTED_TOKEN.fullmatch(prerelease):
        return "preview"
    return "preview"
