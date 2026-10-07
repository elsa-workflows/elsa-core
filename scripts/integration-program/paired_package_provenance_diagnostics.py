"""Bounded diagnostics for failures in owned project provenance checks."""
from __future__ import annotations

from collections.abc import Callable
from pathlib import Path
from typing import Any


PROJECTS = frozenset({"backend", "server", "wasm", "hosted-wasm", "custom-elements", "unknown"})
PROJECTS_BY_HOST = {
    "server": frozenset({"backend", "server"}),
    "wasm": frozenset({"backend", "wasm"}),
    "hosted-wasm": frozenset({"backend", "wasm", "hosted-wasm"}),
    "custom-elements": frozenset({"backend", "custom-elements"}),
}
UNKNOWN = "unknown"

RESOURCE_STAGES = frozenset({"static", "managed", "bootstrap", "host_policy", UNKNOWN})
RESOURCE_HOSTS = frozenset({"server", "wasm", "hosted-wasm", "custom-elements"})
RESOURCE_MESSAGE_CODES = {
    "Missing actual static asset inventory": "static_asset_inventory_missing",
    "Baseline browser archive differs from approved source digest": "static_archive_digest_mismatch",
    "Baseline browser archive source identity differs": "static_archive_identity_mismatch",
    "Missing mandatory package runtime subset": "managed_runtime_subset_missing",
    "WASM manifests changed during derivation": "managed_manifest_changed",
    "Unreviewed standalone WASM bootstrap framework/SDK/host": "bootstrap_cell_mismatch",
    "Invalid host-specific browser request policy": "host_policy_invalid",
}
RESOURCE_CODES = frozenset(RESOURCE_MESSAGE_CODES.values()) | {UNKNOWN}

# Only exact, stable validator messages are classified. Any changed, dynamic or
# otherwise unrecognized message becomes `unknown`; raw text never enters a receipt.
MESSAGE_CODES = {
    "Symlink in package proof input": "project_input_symlink",
    "Missing package proof input": "project_input_missing",
    "Materialized framework mismatch": "project_framework_mismatch",
    "Fixture must be nonpackable": "fixture_packability_mismatch",
    "Unexpected restored fixture targets": "restored_target_mismatch",
    "Unexpected source reference without explicit fixture": "unapproved_project_reference",
    "Only the owned HostedWasm fixture may reference client glue": "fixture_reference_owner_mismatch",
    "Unexpected fixture source edge": "fixture_reference_mismatch",
    "Invalid referenced fixture glue": "fixture_reference_invalid",
    "Malformed restored target": "restored_target_malformed",
    "Unexpected project/source fallback": "unapproved_project_fallback",
    "Source fallback is not the owned fixture": "fixture_reference_identity_mismatch",
    "Restored fixture path differs from owned source edge": "fixture_reference_path_mismatch",
    "Inconsistent fixture identity": "fixture_reference_identity_mismatch",
    "Unclassified source library record": "unclassified_source_library",
    "Baseline package cache must be isolated from the shared user cache": "package_cache_not_isolated",
    "Baseline NuGet.Config must contain only one isolated packageSources section": "nuget_config_shape_mismatch",
    "Baseline NuGet.Config must clear inherited sources and add only nuget.org": "nuget_sources_mismatch",
    "Baseline NuGet.Config contains a non-NuGet.org package source": "nuget_source_mismatch",
    "Baseline restore must use NuGet.org as its only effective source": "restore_source_mismatch",
    "Baseline restore did not use only its project-local NuGet.Config": "restore_config_mismatch",
    "Baseline restore did not use the requested isolated package cache": "restore_cache_mismatch",
    "Baseline restore must not use fallback package folders": "restore_fallback_present",
    "Baseline assets contain an unexpected package or fallback folder": "restore_package_folder_mismatch",
    "Effective restore sources leaked": "restore_source_mismatch",
    "Effective package cache/fallback folders leaked": "restore_package_folder_mismatch",
    "Parent NuGet config leaked": "restore_config_mismatch",
    "Raw project.assets.json changed while package provenance was being read": "project_assets_changed",
    "Candidate manifest differs from verified input": "candidate_manifest_mismatch",
    "Candidate project producer mismatch": "candidate_project_producer_mismatch",
    "Project is not owned by this cell": "unowned_project",
    "Verified candidate manifest changed": "candidate_manifest_changed",
    "Direct Elsa package roots missing": "direct_package_roots_missing",
}
CODES = frozenset(MESSAGE_CODES.values()) | {UNKNOWN}


def failure_receipt(project: str, error: BaseException) -> dict[str, str]:
    """Return only a project enum and stable code; never serialize exception text."""
    selected_project = project if isinstance(project, str) and project in PROJECTS else UNKNOWN
    code = UNKNOWN
    if type(error) in (ValueError, RuntimeError) and len(error.args) == 1 and isinstance(error.args[0], str):
        code = MESSAGE_CODES.get(error.args[0], UNKNOWN)
    return {"project": selected_project, "code": code}


def validate_project_with_diagnostic(
    project_name: str,
    project: Path,
    validator: Callable[[Path], dict[str, Any]],
    evidence: dict[str, Any],
) -> dict[str, Any]:
    """Capture a bounded failure code at the project boundary and re-raise unchanged."""
    try:
        return validator(project)
    except Exception as error:
        evidence["project_provenance_failure"] = failure_receipt(project_name, error)
        raise


def wrap_project_validator(
    project_paths: dict[str, Path],
    validator: Callable[[Path], dict[str, Any]],
    evidence: dict[str, Any],
) -> Callable[[Path], dict[str, Any]]:
    """Bind repeat validation calls to their fixed project enum and failure stage."""
    project_names = {Path(path).resolve(): name for name, path in project_paths.items()}

    def validate(project: Path) -> dict[str, Any]:
        previous_stage = evidence.get("stage")
        evidence["stage"] = "project_provenance"
        project_name = UNKNOWN
        try:
            project_path = Path(project)
            project_name = project_names.get(project_path.resolve(), UNKNOWN)
        except Exception as error:
            evidence["project_provenance_failure"] = failure_receipt(project_name, error)
            raise
        result = validate_project_with_diagnostic(project_name, project_path, validator, evidence)
        # Only restore the surrounding stage after successful validation.
        if previous_stage is None:
            evidence.pop("stage", None)
        else:
            evidence["stage"] = previous_stage
        return result

    return validate


def validate_failure_evidence(value: Any, *, host: Any, stage: Any, result: Any, failure_category: Any) -> None:
    """Validate the narrow failed-execution diagnostic before retaining it."""
    if (not isinstance(value, dict) or set(value) != {"project", "code"}
            or not isinstance(value.get("project"), str) or value["project"] not in PROJECTS
            or not isinstance(value.get("code"), str) or value["code"] not in CODES
            or not isinstance(host, str) or host not in PROJECTS_BY_HOST
            or stage != "project_provenance" or result != "failed"
            or failure_category != "execution_or_evidence_failed"):
        raise ValueError("Invalid retained project provenance diagnostic")
    project = value["project"]
    if project != UNKNOWN and project not in PROJECTS_BY_HOST[host]:
        raise ValueError("Project provenance diagnostic is outside its host")


def resource_failure_receipt(stage: str, error: BaseException) -> dict[str, str]:
    """Return only a resource boundary enum and exact-message code."""
    selected_stage = stage if isinstance(stage, str) and stage in RESOURCE_STAGES else UNKNOWN
    code = UNKNOWN
    if type(error) in (ValueError, RuntimeError) and len(error.args) == 1 and isinstance(error.args[0], str):
        code = RESOURCE_MESSAGE_CODES.get(error.args[0], UNKNOWN)
    return {"stage": selected_stage, "code": code}


def run_resource_boundary(stage: str, operation: Callable[[], Any], evidence: dict[str, Any]) -> Any:
    """Record a safe boundary code and re-raise the original failure unchanged."""
    try:
        return operation()
    except Exception as error:
        evidence.setdefault("resource_provenance_failure", resource_failure_receipt(stage, error))
        raise


def validate_resource_failure_evidence(
    value: Any, *, host: Any, stage: Any, result: Any, failure_category: Any,
) -> None:
    """Reject raw or unbound resource diagnostics in retained execution receipts."""
    if (not isinstance(value, dict) or set(value) != {"stage", "code"}
            or not isinstance(value.get("stage"), str) or value["stage"] not in RESOURCE_STAGES
            or not isinstance(value.get("code"), str) or value["code"] not in RESOURCE_CODES
            or not isinstance(host, str) or host not in RESOURCE_HOSTS
            or stage != "resource_provenance" or result != "failed"
            or failure_category != "execution_or_evidence_failed"):
        raise ValueError("Invalid retained resource provenance diagnostic")
