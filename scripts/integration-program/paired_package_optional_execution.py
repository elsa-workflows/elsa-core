"""Run one optional profile in a fresh owned runtime and preserve bounded facts."""
from dataclasses import replace
import copy
import threading

import materialize_paired_package_hosts as hosts
import paired_package_optional_features as features
import paired_package_optional_transport as transport


def profile_request(request, scenario):
    profile = features.optional_feature_profile(scenario)
    result = replace(request, backend_features=tuple(profile["backend_features"]),
                     permission_profile=profile["permission_profile"], designer_mode="x6")
    features.validate_optional_feature_profile(result, scenario)
    return result


def endpoint_assemblies(verified, request):
    """Reduce verified package metadata to the canonical endpoint owner identities."""
    names = {"secrets": "Elsa.Secrets", "workflow-contexts": "Elsa.WorkflowContexts"}
    selected = []
    for feature in request.backend_features:
        matches = [row for row in verified["backend"]["package_assemblies"] if row["name"] == names[feature]]
        features.require(len(matches) == 1, "Optional endpoint assembly is missing or ambiguous")
        selected.append({field: matches[0][field] for field in ("name", "fullName", "sha256")})
    return selected


def validate_optional_execution(record, request, scenario, *, canonical_definition_id_sha256,
                                canonical_assemblies, validate_ready):
    """Recompute profile assessment from the retained facts, including failed runs."""
    from paired_package_optional_endpoints import validate_optional_endpoint_snapshot, derive_backend_native_rows

    request = profile_request(request, scenario)
    cell, profile = features.validate_optional_feature_profile(request, scenario)
    features._sha(canonical_definition_id_sha256)
    features.require(canonical_definition_id_sha256 != features.EMPTY_BODY_SHA256,
                     "Optional execution lacks its canonical workflow identity")
    required = {"schema", "cell", "scenario", "profile", "stage", "result", "owned_process_cleanup", "failure_category"}
    optional = {"runtime_readiness", "endpoint_assemblies", "endpoint_boundary", "endpoint_final",
                "parent_disconnect", "browser", "assessment"}
    features.require(isinstance(record, dict) and required <= set(record) <= required | optional,
                     "Unsafe optional execution fields")
    features.require(type(record["schema"]) is int and record["schema"] == 1 and record["cell"] == cell
                     and record["scenario"] == scenario and record["profile"] == profile,
                     "Optional execution identity differs")
    features.require(isinstance(record["stage"], str) and isinstance(record["result"], str)
                     and record["stage"] in {"fresh_runtime", "runtime_readiness", "browser_execution",
                     "disconnect_boundary", "native_action", "native_evidence", "owned_cleanup", "complete"}
                     and record["result"] in {"passed", "failed"}
                     and type(record["owned_process_cleanup"]) is bool,
                     "Invalid optional execution state")
    if "runtime_readiness" in record:
        validate_ready(record["runtime_readiness"], request)
    if "endpoint_assemblies" in record:
        expected = endpoint_assemblies({"backend": {"package_assemblies": canonical_assemblies}}, request)
        features.require(record["endpoint_assemblies"] == expected, "Optional runtime endpoint binaries differ")
    for name in ("endpoint_boundary", "endpoint_final"):
        if name in record:
            features.require(request.host == "server" and "endpoint_assemblies" in record,
                             "Optional endpoint snapshot lacks its native server owner")
            validate_optional_endpoint_snapshot(record[name], record["endpoint_assemblies"])
    parent_rows = []
    if "endpoint_final" in record:
        features.require("endpoint_boundary" in record, "Optional endpoint snapshot lacks action boundary")
        parent_rows = derive_backend_native_rows(record["endpoint_boundary"], record["endpoint_final"],
                                                record["endpoint_assemblies"], scenario=scenario)
        if scenario == "disconnect":
            features.require(record["endpoint_final"] == record["endpoint_boundary"],
                             "Stopped backend cannot supply post-stop endpoint metadata")
    disconnect = record.get("parent_disconnect")
    if disconnect is not None:
        features.require(scenario == "disconnect" and isinstance(disconnect, dict)
                         and set(disconnect) == features.DISCONNECT_CHECKS
                         and all(type(flag) is bool for flag in disconnect.values())
                         and disconnect["child_ready_observed"] and disconnect["owned_backend_stopped"]
                         and disconnect["studio_alive_after_stop"], "Invalid owned optional disconnect")
    if "browser" in record:
        child = transport.validate_envelope(record["browser"], request, scenario)
        features.require(child["probe"] is not None, "Optional execution lacks a native observation")
        assessment = features.validate_optional_feature_receipt(child["probe"], request, scenario,
            parent_observations=parent_rows, parent_disconnect=disconnect,
            canonical_definition_id_sha256=canonical_definition_id_sha256)
        if disconnect is not None:
            features.require(disconnect["browser_alive_after_stop"] is (child["browser_alive_after_stop"] is True)
                             and (not disconnect["cleanup_verified"] or
                                  record["owned_process_cleanup"] and child["cleanup_verified"]),
                             "Optional disconnect browser or cleanup binding differs")
        if "assessment" in record:
            features.require(record["assessment"] == assessment, "Optional assessment differs from observations")
    else:
        features.require("assessment" not in record, "Optional assessment lacks browser observation")
    if record["stage"] == "complete":
        features.require({"runtime_readiness", "endpoint_assemblies", "browser", "assessment"} <= set(record)
                         and record["owned_process_cleanup"], "Incomplete optional execution claimed completion")
        features.require(request.host != "server" or "endpoint_final" in record,
                         "Optional Server execution lacks canonical endpoint evidence")
        accepted = (record["assessment"]["acceptance"] and child["cleanup_verified"]
                    and child["failure_category"] is None)
        features.require(record["result"] == ("passed" if accepted else "failed")
                         and record["failure_category"] == (None if accepted else "optional_acceptance_failed"),
                         "Optional acceptance claim differs from observations")
    else:
        features.require(record["result"] == "failed" and record["failure_category"] == "optional_execution_failed",
                         "Partial optional execution claimed success")
    return record


def run_optional_profile(layout, scenario, *, validate_project, observe_ready, observe_assemblies,
                         observe_endpoints, canonical_definition_id_sha256):
    """Callbacks return already verified metadata from this exact owned runtime.

    A complete defect observation is returned as failed acceptance. An execution
    failure retains only facts validated before its boundary, never raw errors.
    """
    from paired_package_optional_endpoints import validate_optional_endpoint_snapshot, derive_backend_native_rows

    request = profile_request(layout.request, scenario)
    cell, profile = features.validate_optional_feature_profile(request, scenario)
    record = {"schema": 1, "cell": cell, "scenario": scenario, "profile": profile,
              "stage": "fresh_runtime", "result": "failed", "owned_process_cleanup": False,
              "failure_category": "optional_execution_failed"}
    record_lock = threading.Lock()
    sealed = threading.Event()

    def commit_callback(facts):
        with record_lock:
            features.require(not sealed.is_set(), "Optional evidence was already sealed")
            record.update(facts)

    runtime_root = layout.group_root / "optional-runtime" / request.host / scenario
    try:
        runtime_root.mkdir(parents=True, mode=0o700, exist_ok=False)
        variant = replace(layout, request=request, runtime_root=runtime_root)
        with hosts.start_optional_feature_probe(variant, validate_project=validate_project) as runtime:
            handle = runtime.handle
            record["stage"] = "runtime_readiness"
            record["runtime_readiness"] = observe_ready(handle, request)

            def capture_assemblies():
                return endpoint_assemblies(observe_assemblies(handle, variant), request)

            def snapshot(assemblies):
                value = observe_endpoints(handle)
                validate_optional_endpoint_snapshot(value, assemblies)
                return value

            def on_event(event):
                # The runtime owner checks its captured process identities and
                # deadline again immediately before any bounded observation/stop.
                runtime.check_live_studio()
                facts = {}
                if event == "disconnect-ready":
                    facts["stage"] = "disconnect_boundary"
                    facts["endpoint_assemblies"] = capture_assemblies()
                    if request.host == "server":
                        facts["endpoint_boundary"] = snapshot(facts["endpoint_assemblies"])
                    stopped = runtime.disconnect_backend()
                    facts["parent_disconnect"] = {
                        "child_ready_observed": True, **stopped,
                        "browser_alive_after_stop": False, "cleanup_verified": False}
                elif event == "begin-native-action":
                    facts["stage"] = "native_action"
                    if scenario == "disconnect":
                        features.require(runtime.disconnected and "parent_disconnect" in record,
                                         "Optional native action preceded owned stop")
                    elif request.host == "server":
                        facts["endpoint_assemblies"] = capture_assemblies()
                        facts["endpoint_boundary"] = snapshot(facts["endpoint_assemblies"])
                else:
                    raise ValueError("Unexpected optional control callback")
                runtime.check_live_studio()
                commit_callback(facts)

            record["stage"] = "browser_execution"
            child = transport.run_optional_browser(handle, request, scenario, on_event=on_event)
            # Do not retain arbitrary nested child fields until the full native
            # observation validator has checked them below.
            record["stage"] = "native_evidence"
            if scenario != "disconnect":
                record["endpoint_assemblies"] = capture_assemblies()
            parent_rows = []
            if request.host == "server":
                # A stopped backend cannot supply a post-stop HTTP snapshot.
                final = record["endpoint_boundary"] if scenario == "disconnect" else snapshot(record["endpoint_assemblies"])
                parent_rows = derive_backend_native_rows(record["endpoint_boundary"], final,
                                                        record["endpoint_assemblies"], scenario=scenario)
                record["endpoint_final"] = final
            disconnect = record.get("parent_disconnect")
            if disconnect is not None:
                disconnect["browser_alive_after_stop"] = child["browser_alive_after_stop"] is True
            features.require(child["probe"] is not None, "Optional browser observation absent")
            features.validate_optional_feature_receipt(child["probe"], request, scenario,
                parent_observations=parent_rows, parent_disconnect=disconnect,
                canonical_definition_id_sha256=canonical_definition_id_sha256)
            record["browser"] = child
            runtime.check_live_studio()
            record["stage"] = "owned_cleanup"
        record["owned_process_cleanup"] = True
        if disconnect is not None:
            disconnect["cleanup_verified"] = child["cleanup_verified"]
        record["assessment"] = features.validate_optional_feature_receipt(child["probe"], request, scenario,
            parent_observations=parent_rows, parent_disconnect=disconnect,
            canonical_definition_id_sha256=canonical_definition_id_sha256)
        accepted = (record["assessment"]["acceptance"] and child["cleanup_verified"]
                    and child["failure_category"] is None)
        record.update(stage="complete", result="passed" if accepted else "failed",
                      failure_category=None if accepted else "optional_acceptance_failed")
    except Exception:
        # Context managers still attempt owned cleanup; absent confirmation cannot pass.
        pass
    with record_lock:
        sealed.set()
        # A timed-out metadata callback may still unwind in its daemon thread.
        # It cannot change either the sealed source or this detached receipt.
        return copy.deepcopy(record)
