"""Owned child transport for separate optional-feature browser observations."""
from dataclasses import asdict
import json
import os
import re

from paired_package_optional_control import OptionalProbeControl
from paired_package_optional_features import validate_optional_feature_profile
import run_paired_package_browser_matrix as browser


def validate_envelope(value, request, scenario):
    cell, _ = validate_optional_feature_profile(request, scenario)
    fields = {"schema", "phase", "cell", "probe", "browser_version", "cleanup_verified",
              "browser_alive_after_stop", "failure_category"}
    browser.require(isinstance(value, dict) and set(value) == fields and type(value["schema"]) is int
                    and value["schema"] == 1 and value["phase"] == "optional-feature-probe"
                    and value["cell"] == cell, "Invalid optional browser envelope")
    browser.require(value["probe"] is None or isinstance(value["probe"], dict), "Invalid optional browser probe")
    browser.require(value["browser_version"] is None or isinstance(value["browser_version"], str)
                    and re.fullmatch(r"[0-9]{1,4}(?:\.[0-9]{1,8}){3}", value["browser_version"]) is not None,
                    "Unsafe optional browser version")
    browser.require(type(value["cleanup_verified"]) is bool, "Invalid optional browser cleanup")
    browser.require(value["browser_alive_after_stop"] is None if scenario != "disconnect" else
                    value["browser_alive_after_stop"] is None or type(value["browser_alive_after_stop"]) is bool,
                    "Invalid optional browser liveness")
    browser.require(value["failure_category"] in (None, "optional_probe_execution_failed"),
                    "Unsafe optional browser failure")
    if value["failure_category"] is None:
        browser.require(value["probe"] is not None and value["browser_version"] is not None
                        and value["cleanup_verified"], "Successful optional transport lacks its observation or cleanup")
    # Full receipt semantics and parent-owned endpoint/process proof are checked
    # by the caller; this envelope alone cannot establish optional acceptance.
    return value


def run_optional_browser(handle, request, scenario, *, on_event, timeout=240):
    """Keep credentials on stdin and control on a private inherited Unix socket."""
    validate_optional_feature_profile(request, scenario)
    control = OptionalProbeControl(scenario, on_event, timeout=timeout)
    try:
        descriptor = control.descriptor()
        payload = {"request": asdict(request), "studio_url": handle.studio_url,
                   "backend_url": handle.backend_url, "username": handle.username, "password": handle.password,
                   "safe_ids": handle.safe_ids, "resources": [], "phase": "optional-feature-probe",
                   "optional_probe": {"scenario": scenario, "control": descriptor}}
        # npm/tsx CLI process spawning need not inherit arbitrary descriptors.
        # The locked loader runs inside this exact Node process instead.
        result = browser._run_browser_process(["node", "--import", "tsx", str(browser.JOURNEY)],
            cwd=browser.JOURNEY.parent, input=json.dumps(payload), timeout=timeout,
            env={key: value for key, value in os.environ.items() if key in {"PATH", "HOME", "TMPDIR", "SystemRoot"}},
            pass_fds=(descriptor["fd"],), control=control.session)
        browser.require(control.complete and not control.failure and
                        tuple(control.completed_events) == control.events, "Optional action control did not complete")
        browser.require(len(result.stdout.encode("utf-8")) <= 256 * 1024, "Optional browser receipt exceeded bound")
        record = validate_envelope(json.loads(result.stdout), request, scenario)
        browser.require(result.returncode == (1 if record["failure_category"] else 0),
                        "Optional browser exit status differs")
        return record
    finally:
        control.close()
