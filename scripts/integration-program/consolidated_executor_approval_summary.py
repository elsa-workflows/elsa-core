#!/usr/bin/env python3
"""Render validated, non-authoritative same-run evidence for native reviewers."""
from __future__ import annotations

import argparse
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import re
import sys

import consolidated_package_executor as executor
import consolidated_package_recovery as recovery
import consolidated_recovery_inputs as inputs
from prepare_consolidated_release_candidate import inventory_identity


class SummaryError(ValueError):
    pass


class SafeParser(argparse.ArgumentParser):
    def error(self, message):
        raise SummaryError("summary_arguments_invalid")


def require(condition, category):
    if not condition:
        raise SummaryError(category)


def digest(value):
    return isinstance(value, str) and executor.SHA256.fullmatch(value) is not None


def timestamp(value):
    require(isinstance(value, str), "invalid_timestamp")
    try:
        result = datetime.fromisoformat(value.replace("Z", "+00:00"))
    except ValueError:
        raise SummaryError("invalid_timestamp") from None
    require(result.tzinfo is not None and result <= datetime.now(timezone.utc), "invalid_timestamp")
    return result


def counts(rows):
    return {name: sum(row["classification"] == name for row in rows)
            for name in ("matching", "missing", "conflicting", "unverifiable")}


def render_summary(admission, verification, *, artifact_id, artifact_digest, admission_hash, verification_hash):
    """Inputs are job evidence, never an approval or a replacement for admission."""
    source, run, attempt = (os.environ.get(name, "") for name in ("GITHUB_SHA", "GITHUB_RUN_ID", "GITHUB_RUN_ATTEMPT"))
    require(os.environ.get("GITHUB_ACTIONS") == "true" and os.environ.get("GITHUB_REPOSITORY") == executor.REPOSITORY
            and os.environ.get("GITHUB_REF") == "refs/heads/main" and os.environ.get("GITHUB_EVENT_NAME") == "workflow_dispatch"
            and re.fullmatch(r"[0-9a-f]{40}", source) and re.fullmatch(r"[1-9][0-9]*", run) and attempt == "1",
            "runtime_identity_invalid")
    require(type(artifact_id) is int and artifact_id > 0 and isinstance(artifact_digest, str)
            and re.fullmatch(r"sha256:[0-9a-f]{64}", artifact_digest), "artifact_identity_invalid")
    require(digest(admission_hash) and digest(verification_hash), "receipt_digest_invalid")
    runtime = {"source_commit": source, "run_id": int(run), "run_attempt": 1}
    require(type(admission.get("schema")) is int and admission["schema"] == 1 and admission.get("result") == "eligible_for_native_approval"
            and admission.get("failure_category") is None and admission.get("publication_performed") is False
            and admission.get("publication_ready") is False, "schedule_receipt_invalid")
    admitted = admission.get("admission", {})
    require(admitted.get("status") == "eligible_for_native_approval" and admitted.get("repository") == executor.REPOSITORY
            and admitted.get("ref") == "refs/heads/main" and all(admitted.get(key) == value for key, value in runtime.items())
            and admitted.get("executor_source") == source and admitted.get("environment_name") == executor.ENVIRONMENT
            and admitted.get("native_approval_verified") is False and admitted.get("credential_provenance_verified") is False
            and admitted.get("credential_isolation") == "pending_authenticated_execution_check", "schedule_identity_invalid")
    require(type(admitted.get("environment_id")) is int and admitted["environment_id"] > 0
            and digest(admitted.get("operational_packet_sha256")) and digest(admitted.get("policy_sha256")), "schedule_policy_invalid")
    branches, reviewers = admitted.get("branch_policies"), admitted.get("reviewer_ids")
    require(isinstance(branches, list) and len(branches) == 1 and branches[0].get("type") == "branch"
            and branches[0].get("name") == "main" and type(branches[0].get("id")) is int and branches[0]["id"] > 0
            and isinstance(reviewers, list) and bool(reviewers) and all(type(item) is int and item > 0 for item in reviewers)
            and len(set(reviewers)) == len(reviewers), "schedule_policy_invalid")
    candidate = {**executor.candidate_input.PRODUCER, "archive_sha256": executor.candidate_input.ARCHIVE_SHA256,
                 "preupload_manifest_sha256": executor.candidate_input.MANIFEST_SHA256, "expires_at": executor.EXPIRY,
                 "package_count": 225, "exclusion_count": 124, "excluded_id_count": 123}
    require(admitted.get("candidate") == candidate, "candidate_identity_invalid")
    implementation = executor.sha(Path(executor.__file__).read_bytes())
    require(admitted.get("implementation_sha256") == implementation
            and verification.get("implementation_sha256") == implementation, "implementation_identity_invalid")
    require(type(verification.get("schema")) is int and verification["schema"] == 1 and verification.get("mode") == "verify"
            and verification.get("scope") == "production" and verification.get("result") == "verified"
            and verification.get("failure_category") is None and verification.get("publication_performed") is False
            and verification.get("upload_attempted") is False and verification.get("publication_ready") is False
            and verification.get("remote_snupkg_archive_verified") is False and verification.get("admission") is None
            and verification.get("after") is None and verification.get("feed") == recovery.FEED_INDEX
            and verification.get("version") == recovery.VERSION, "verification_receipt_invalid")
    provenance = recovery._provenance(verification["original_provenance"])
    require(provenance["planner"] == runtime and provenance["archive_sha256"] == executor.candidate_input.ARCHIVE_SHA256
            and provenance["original_envelope_sha256"] == executor.candidate_input.ENVELOPE_SHA256
            and provenance["preupload_manifest_sha256"] == executor.candidate_input.MANIFEST_SHA256,
            "original_identity_invalid")
    baseline, _ = inputs._read_bounded_json(Path(executor.__file__).with_name("consolidated-candidate-inventory-baseline.json"), "Reviewed inventory")
    inventory_hash = executor.sha(json.dumps(inventory_identity(baseline), sort_keys=True, separators=(",", ":")).encode())
    require(provenance["inventory_sha256"] == inventory_hash, "inventory_identity_invalid")
    package_ids = {row["id"] for row in baseline["packages"]}
    require(len(package_ids) == 225 and len(baseline["exclusions"]) == 124
            and len({row["id"] for row in baseline["exclusions"]}) == 123, "inventory_identity_invalid")
    start, finish, scheduled = (timestamp(value) for value in
                               (verification.get("started_at"), verification.get("finished_at"), admitted.get("observed_at")))
    require(timestamp(provenance["observed_at"]) <= start <= finish <= scheduled
            and scheduled < datetime.fromisoformat(executor.EXPIRY.replace("Z", "+00:00")), "receipt_timing_invalid")
    require(digest(verification.get("feed_index_sha256")) and digest(verification.get("inspector_sha256"))
            and verification.get("inspector_source_sha256") == executor.sha(Path(executor.__file__).with_name("VerifyPackageSymbolPair").joinpath("Program.cs").read_bytes()),
            "verifier_identity_invalid")
    observed = verification["before"]
    package_proof = observed["packages"]
    require(observed.get("blocked") is False and package_proof.get("original_provenance") == provenance
            and package_proof.get("feed") == recovery.FEED_INDEX and package_proof.get("publication_performed") is False
            and package_proof.get("publication_ready") is False and package_proof.get("remote_symbols_verified") is False
            and package_proof.get("access_mode") == "anonymous" and package_proof.get("absence_scope") == "declared_access_visibility_only"
            and package_proof.get("classification_complete") is True and package_proof.get("reconciliation_blocked") is False
            and package_proof.get("package_count") == 225 and package_proof.get("exclusion_count") == 124
            and digest(package_proof.get("exclusions_sha256")), "package_observation_invalid")
    feed = package_proof["feed_observation"]
    require(feed.get("status") == 200 and feed.get("complete") is True and feed.get("failure_category") is None
            and digest(feed.get("archive_sha256")), "feed_observation_invalid")
    packages = package_proof["packages"]
    require(isinstance(packages, list) and len(packages) == 225 and {row["id"] for row in packages} == package_ids,
            "package_coverage_invalid")
    pins = {}
    for row in packages:
        require(row["version"] == recovery.VERSION and row.get("classification") in {"missing", "matching"}
                and row.get("failure_category") is None and all(digest(row.get(key)) for key in
                ("local_nupkg_sha256", "local_snupkg_sha256", "local_payload_sha256")), "package_observation_invalid")
        remote = row["remote"]
        require(remote.get("complete") is True and remote.get("status") == (404 if row["classification"] == "missing" else 200),
                "package_observation_invalid")
        if row["classification"] == "matching":
            require(digest(remote.get("archive_sha256")) and remote.get("payload_sha256") == row["local_payload_sha256"]
                    and row.get("comparison") == {key: True for key in ("identity", "source", "dependencies", "payload")},
                    "package_observation_invalid")
        for kind in ("nupkg", "snupkg"):
            pins[(row["id"], kind)] = row["local_"+kind+"_sha256"]
    package_counts = counts(packages)
    require(package_proof.get("counts") == package_counts and package_proof.get("content_converged") is (package_counts["matching"] == 225),
            "package_counts_invalid")
    operations = verification["operations"]
    require(isinstance(operations, list) and len(operations) == 450
            and {(row["id"], row["kind"]) for row in operations} == set(pins), "operation_coverage_invalid")
    require(all(row.get("state") == "not_attempted" and row.get("status") is None and row.get("failure_category") is None
                and row.get("archive_sha256") == pins[(row["id"], row["kind"])] for row in operations), "operation_receipt_invalid")
    associations = verification["associations"]
    require(isinstance(associations, list) and len(associations) == 225 and {row["id"] for row in associations} == package_ids,
            "association_coverage_invalid")
    keys, association_count, assembly_free = {}, 0, 0
    for row in associations:
        records = row["associations"]
        require(isinstance(records, list) and type(row.get("assembly_free")) is bool
                and row["assembly_free"] is (len(records) == 0) and len({item["framework"] for item in records}) == len(records),
                "association_coverage_invalid")
        assembly_free += row["assembly_free"]
        for item in records:
            key = item.get("key")
            require(isinstance(key, str) and executor.KEY.fullmatch(key) and digest(item.get("pdb_sha256"))
                    and item.get("source_evidence_preserved") is True and type(item.get("document_count")) is int
                    and item["document_count"] >= 0 and type(item.get("pdb_size")) is int and item["pdb_size"] > 0,
                    "association_identity_invalid")
            require(key not in keys or keys[key] == item["pdb_sha256"], "association_identity_invalid")
            keys[key] = item["pdb_sha256"]
            association_count += 1
    require(0 < association_count <= 2000, "association_coverage_invalid")
    symbols = observed["symbols"]
    require(isinstance(symbols, list) and len(symbols) == len(keys) and {row["key"] for row in symbols} == set(keys),
            "symbol_coverage_invalid")
    for row in symbols:
        require(row.get("classification") in {"matching", "missing"} and row.get("complete") is True
                and row.get("failure_category") is None and row.get("status") == (200 if row["classification"] == "matching" else 404)
                and (row["classification"] == "missing" or row.get("pdb_sha256") == keys[row["key"]]), "symbol_observation_invalid")
    symbol_counts = counts(symbols)
    converged = package_counts["matching"] == 225 and symbol_counts["matching"] == len(keys)
    require(observed.get("content_verified") is converged and verification.get("content_verified") is converged,
            "content_receipt_invalid")
    pending = verification.get("pending_gates")
    require(isinstance(pending, list) and set(pending) == set(executor.PENDING) and len(pending) == len(executor.PENDING),
            "pending_gates_invalid")
    artifact_url = f"https://github.com/{executor.REPOSITORY}/actions/runs/{run}/artifacts/{artifact_id}"
    return f"""## Consolidated original candidate: evidence before native approval

This is a completed, nonpublishing observation from this run. It grants no native approval or publication authority. Missing content is absence within anonymous reader visibility, not release acceptance. Credential isolation must still pass authenticated checks after native approval and before publisher-key use.

| Binding | Evidence |
| --- | --- |
| Executor commit | `{source}` |
| Executor implementation SHA-256 | `{implementation}` |
| Executor run / attempt | `{run}` / `1` |
| Verification result | `verified`; uploads attempted: **0** |
| Verification artifact | [{artifact_id}]({artifact_url}) |
| Artifact digest reported by GitHub | `{artifact_digest}` |
| Verification receipt SHA-256 | `{verification_hash}` |
| Scheduling receipt SHA-256 | `{admission_hash}` |
| Original producer commit | `{executor.candidate_input.SOURCE}` |
| Original producer run / attempt / artifact | `{executor.candidate_input.RUN}` / `{executor.candidate_input.ATTEMPT}` / `{executor.candidate_input.ARTIFACT}` |
| Original archive SHA-256 | `{executor.candidate_input.ARCHIVE_SHA256}` |
| Original envelope SHA-256 | `{executor.candidate_input.ENVELOPE_SHA256}` |
| Original pre-upload manifest SHA-256 | `{executor.candidate_input.MANIFEST_SHA256}` |
| Verified package manifest SHA-256 | `{provenance['verified_artifacts_sha256']}` |
| Inventory SHA-256 | `{inventory_hash}` |
| Feed | {recovery.FEED_INDEX} |
| Feed resource index SHA-256 | `{verification['feed_index_sha256']}` |
| Package readback index SHA-256 | `{feed['archive_sha256']}` |
| Inspector executable / source SHA-256 | `{verification['inspector_sha256']}` / `{verification['inspector_source_sha256']}` |
| Candidate expires | `{executor.EXPIRY}` |
| Native environment | `{executor.ENVIRONMENT}`; ID `{admitted['environment_id']}`; exact branch `main` (policy ID `{branches[0]['id']}`) |
| Reviewed GitHub reviewer IDs | {', '.join('`'+str(item)+'`' for item in sorted(reviewers))} |
| Reviewed policy / operational packet SHA-256 | `{admitted['policy_sha256']}` / `{admitted['operational_packet_sha256']}` |

Complete scope: **225 package pairs / 450 original archives**, **124 exclusions / 123 excluded IDs**, **{association_count} DLL/PDB associations**, **{len(keys)} distinct PDB keys**, **{assembly_free} assembly-free pairs**. Package observations: **{package_counts['matching']} matching, {package_counts['missing']} missing, 0 conflicting, 0 unknown**. PDB observations: **{symbol_counts['matching']} matching, {symbol_counts['missing']} missing, 0 conflicting, 0 unknown**. The completed verification records preserved SourceLink and original source evidence for every recorded association. These counts come from that verification receipt; this formatter checks receipt coverage and consistency and does not independently reinspect the original manifest or DLL/PDB bytes.

The artifact ID and digest above come from the same-run GitHub upload outputs; this helper hashes the extracted receipt and does not independently reconstruct or validate the uploaded ZIP digest. Workflow admission separately requires the verification job to have succeeded and downloads that exact artifact ID.

Publication readiness remains **false**. Native approval is **pending**. Original `.snupkg` archive readback, approved live cutover, overwrite controls, full compatibility and private reader-rights acceptance remain separate gates.
"""


def main(argv=None):
    parser = SafeParser(description=__doc__)
    parser.add_argument("--admission", type=Path, required=True)
    parser.add_argument("--verification", type=Path, required=True)
    parser.add_argument("--artifact-id", type=int, required=True)
    parser.add_argument("--artifact-digest", required=True)
    try:
        args = parser.parse_args(argv)
        admission, admission_hash = inputs._read_bounded_json(args.admission, "Scheduling receipt", 32*1024*1024)
        verification, verification_hash = inputs._read_bounded_json(args.verification, "Verification receipt", 32*1024*1024)
        markdown = render_summary(admission, verification, artifact_id=args.artifact_id, artifact_digest=args.artifact_digest,
                                  admission_hash=admission_hash, verification_hash=verification_hash)
    except SummaryError as error:
        print(str(error), file=sys.stderr)
        return 1
    except (OSError, ValueError, KeyError, TypeError, AttributeError, RecursionError):
        print("summary_evidence_invalid", file=sys.stderr)
        return 1
    print(markdown, end="")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
