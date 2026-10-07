"""Bind released Studio static assets to source-approved NuGet archives."""
from contextlib import ExitStack
import json
from pathlib import Path
from zipfile import ZipFile

import paired_package_baseline_provenance as baseline
from verify_browser_package_resources import _derive_package_resources, require, sha256


# Baselines prove the default editor smoke, not the candidate's complete interop journey.
# Both releases contain all six common materialized assets; only these startup assets
# must actually be requested by the baseline smoke. Candidate requirements are unchanged.
BASELINE_REQUESTED_ASSETS = {
    "/_content/Elsa.Studio.Workflows.Designer/designer.entry.js",
    "/_content/Elsa.Studio.Workflows.Designer/designer.css",
    "/_content/Elsa.Studio.DomInterop/dom.entry.js",
}


def derive_baseline_resources(build_manifest: Path, cache: Path, version: str, *,
                              policy=baseline.POLICY_PATH, route_prefix: str = "", hosted_layout=None) -> dict:
    selected = baseline._read_policy(policy)
    require(version in baseline.RELEASE_COMMITS["elsa-core"], "Unsupported baseline version")
    by_id = {record["id"].casefold(): record for record in selected["packages"] if record["version"] == version}
    archive_receipts = {}
    with ExitStack() as stack:
        archives = {}

        def package_member(package, member, mandatory):
            package_id = package["id"]
            if package_id not in archives:
                archive, digest, sha512 = baseline._cache_package(cache, package_id, version)
                require(digest == package["archive_sha256"], "Baseline browser archive differs from approved source digest")
                nuspec = baseline._read_package_nuspec(archive, package_id, version)
                require(nuspec == {"id": package_id, "version": version, **package["repository"]},
                        "Baseline browser archive source identity differs")
                archives[package_id] = stack.enter_context(ZipFile(archive))
                archive_receipts[package_id] = {
                    "id": package_id, "version": version, "archive_sha256": digest,
                    "archive_sha512": sha512, "owner": package["owner"],
                    "repository": package["repository"],
                }
            return archives[package_id].read(member)

        receipt = _derive_package_resources(build_manifest, cache, version, by_id, package_member,
                                             route_prefix=route_prefix, hosted_layout=hosted_layout)
    required = {route_prefix + path for path in BASELINE_REQUESTED_ASSETS}
    for asset in receipt["assets"]:
        asset["required"] = asset["path"] in required
    return {**receipt, "version": version,
            "source_policy_sha256": sha256(json.dumps(selected, sort_keys=True, separators=(",", ":")).encode()),
            "packages": [archive_receipts[key] for key in sorted(archive_receipts)]}
