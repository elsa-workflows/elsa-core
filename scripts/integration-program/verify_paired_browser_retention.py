"""Reject files outside the browser proof's portable receipt inventory."""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import stat

from paired_package_provenance import regular_file
from run_paired_package_browser_matrix import MATRIX, REACT_PHASE_HOSTS, check_cell
import paired_package_released_documents as documents
import paired_package_react_phase as react_phase
import paired_package_provenance_diagnostics as provenance_diagnostics
from paired_package_secrets_endpoints import validate_secrets_endpoint_evidence
from verify_browser_package_resources import verify_browser_resources


def _verify_react_phase(root, key, execution, matrix):
    cell = root / "cells" / "-".join(key)
    path = cell / "react-phase.json"
    if not isinstance(matrix.get("cells", []), list):
        raise ValueError("Invalid React matrix cell inventory")
    rows = [row for row in matrix.get("cells", []) if isinstance(row, dict) and
            tuple(row.get(name) for name in ("version", "framework", "host")) == key]
    if len(rows) > 1:
        raise ValueError("Duplicate React matrix cell")
    combined = rows[0] if rows else {}
    if not isinstance(combined.get("proof", {}), dict) or not isinstance(combined.get("assertions", []), list):
        raise ValueError("Invalid React matrix claim")
    claim = combined.get("proof", {}).get("reactflow")
    passed = any(isinstance(item, dict) and item.get("name") == "reactflow_edit_save" and item.get("passed") is True
                 for item in combined.get("assertions", []))
    if not path.exists():
        if claim is not None or passed or "react_phase" in execution or execution.get("result") == "passed":
            raise ValueError("Candidate full-shell evidence is missing its React phase")
        return
    if tuple(execution.get(name) for name in ("version", "framework", "host")) != key:
        raise ValueError("React phase is missing matching execution evidence")
    original_path = regular_file(cell / "browser.json")
    original = json.loads(original_path.read_text())
    phase = json.loads(regular_file(path).read_text())
    inventory = execution.get("resource_inventory", {}).get("assets")
    request = dict(zip(("version", "framework", "host"), key), designer_mode="react-flow",
                   route_prefix=execution.get("route_prefix", ""))
    react_phase.validate_react_phase_receipt(phase, request, original, inventory)
    summary = react_phase.summarize_react_phase(phase)
    if (hashlib.sha256(original_path.read_bytes()).hexdigest() != summary["source_browser_sha256"] or
            hashlib.sha256(path.read_bytes()).hexdigest() != summary["phase_receipt_sha256"] or
            execution.get("react_phase") != summary or execution.get("react_runtime_continuity") is not True):
        raise ValueError("React phase retained bytes or execution binding differ")
    if claim is not None or passed or execution.get("result") == "passed":
        if not combined:
            raise ValueError("Complete React execution is missing its matrix cell")
        check_cell(combined)
        if (claim != summary or combined.get("resources") != original["resources"] + phase["resources"] or
                execution.get("result") != "passed" or execution.get("stage") != "complete" or
                execution.get("owned_process_cleanup") is not True or "react_loaded_assemblies" not in execution):
            raise ValueError("React matrix claim lacks complete matching execution")
        if {name: value for name, value in combined["proof"].items() if name != "reactflow"} != original["proof"]:
            raise ValueError("Combined React evidence changed the original browser proof")
        original_assertions = {item["name"]: item["passed"] for item in original["assertions"]}
        if any(item["passed"] != original_assertions[item["name"]] for item in combined["assertions"]
               if item["name"] not in {"package_provenance", "browser_resources", "reactflow_edit_save"}):
            raise ValueError("Combined React evidence changed an unverified browser assertion")
        prefix = "/" + request["route_prefix"] if request["route_prefix"] else ""
        verified = verify_browser_resources(inventory, combined["resources"], require_all=False, route_prefix=prefix)
        if verified != execution.get("browser_resources") or passed is not (phase["result"] == "passed"):
            raise ValueError("React combined resource or result binding differs")


def verify_retained_inventory(root: Path) -> list[str]:
    # Validate ancestors before traversal; never follow an evidence-root link.
    root = root.absolute()
    regular_file(root / "matrix.json")
    allowed = {"matrix.json", "inputs/original-envelope.json", "inputs/live-retrieval.json", "inputs/provenance.json"}
    for key in MATRIX:
        allowed.update(f"cells/{'-'.join(key)}/{name}.json" for name in ("execution", "browser"))
        if key[0] in documents.TOOL_VERSIONS:
            allowed.add(f"cells/{'-'.join(key)}/released-document.json")
        if key[0] == "3.10.0" and key[2] in REACT_PHASE_HOSTS:
            allowed.add(f"cells/{'-'.join(key)}/react-phase.json")
    directories = {str(parent) for name in allowed for parent in Path(name).parents if str(parent) != "."}
    found = []
    for path in root.rglob("*"):
        name = path.relative_to(root).as_posix()
        mode = path.lstat().st_mode
        if stat.S_ISLNK(mode):
            raise ValueError("Linked browser evidence cannot be retained")
        if stat.S_ISDIR(mode):
            if name not in directories:
                raise ValueError("Unexpected browser evidence directory")
            continue
        if not stat.S_ISREG(mode) or name not in allowed:
            raise ValueError("Unexpected browser evidence file")
        regular_file(path)
        if not 0 < path.stat().st_size <= 8 * 1024 * 1024:
            raise ValueError("Unbounded or empty browser evidence file")
        if not isinstance(json.loads(path.read_text(encoding="utf-8")), dict):
            raise ValueError("Browser evidence must be a JSON object")
        found.append(name)
    # A native document may be retained only with matching complete source
    # evidence. Partial/failed cells remain useful but never publish a document.
    matrix = json.loads((root / "matrix.json").read_text())
    for key in MATRIX:
        cell = root / "cells" / "-".join(key)
        path = cell / "released-document.json"
        execution_path = cell / "execution.json"
        execution = json.loads(execution_path.read_text()) if execution_path.is_file() else {}
        if "project_provenance_failure" in execution:
            provenance_diagnostics.validate_failure_evidence(
                execution["project_provenance_failure"],
                host=execution.get("host"), stage=execution.get("stage"), result=execution.get("result"),
                failure_category=execution.get("failure_category"))
        if "resource_provenance_failure" in execution:
            provenance_diagnostics.validate_resource_failure_evidence(
                execution["resource_provenance_failure"],
                host=execution.get("host"), stage=execution.get("stage"), result=execution.get("result"),
                failure_category=execution.get("failure_category"))
        if key[0] == "3.10.0":
            ownership = execution.get("secrets_endpoint_ownership")
            if ownership is None and execution.get("result") == "passed":
                raise ValueError("Passing candidate lacks canonical Secrets endpoint ownership")
            if ownership is not None:
                assemblies = execution.get("loaded_assemblies", {}).get("backend", {}).get("package_assemblies", [])
                validate_secrets_endpoint_evidence(ownership, assemblies)
        if key[0] == "3.10.0" and key[2] in REACT_PHASE_HOSTS:
            _verify_react_phase(root, key, execution, matrix)
        binding = execution.get("released_document")
        if not path.exists():
            if execution.get("result") == "passed" and key[0] in documents.TOOL_VERSIONS:
                raise ValueError("Passing released cell is missing its document")
            continue
        if execution.get("result") != "passed" or execution.get("stage") != "complete":
            raise ValueError("Released document requires complete source evidence")
        receipt = json.loads(regular_file(cell / "browser.json").read_text())
        identity = json.loads(regular_file(root / "inputs/provenance.json").read_text()).get("browser_execution")
        if binding != documents.bind_released_document(path, key, receipt, execution, identity):
            raise ValueError("Released document source binding differs")
    return sorted(found)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("root", type=Path)
    arguments = parser.parse_args()
    try:
        inventory = verify_retained_inventory(arguments.root)
    except (OSError, ValueError, RuntimeError):
        raise SystemExit("Browser evidence inventory rejected") from None
    print(f"Validated {len(inventory)} portable browser receipt files")
