#!/usr/bin/env python3
"""Fail closed before preparing/publishing a React Studio release (#8857/#8858)."""

import os
import re


def validate(version: str, publish: str, enabled: str, ref: str, repository: str) -> None:
    match = re.fullmatch(r"3\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-preview\.(0|[1-9][0-9]*))?", version)
    if not match or int(match[1]) < 11:
        raise ValueError("React Studio starts at 3.11.0; use X.Y.Z or X.Y.Z-preview.N with no v prefix")
    if publish not in ("true", "false"):
        raise ValueError("PUBLISH must be true or false")
    if publish == "true":
        if repository != "elsa-workflows/elsa-core" or enabled != "true":
            raise ValueError("Release must explicitly enable REACT_STUDIO_PUBLISH_ENABLED after cutover approval")
        if ref != f"refs/tags/{version}":
            raise ValueError("Publish only from the matching approved release tag")


if __name__ == "__main__":
    version = os.environ["VERSION"]
    validate(version, os.environ["PUBLISH"], os.getenv("PUBLISH_ENABLED", ""),
             os.environ["GITHUB_REF"], os.environ["GITHUB_REPOSITORY"])
    with open(os.environ["GITHUB_OUTPUT"], "a") as output:
        output.write(f"version={version}\n")
