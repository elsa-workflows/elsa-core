"""Bind native Hosted root/prefix receipts to the same verified client build.

Aliases describe only the two probed delivery paths. They are not new package
authority: every observed alias must match an original canonical resource.
"""
from __future__ import annotations

from dataclasses import asdict, is_dataclass
import hashlib
import json
import os
import re
import subprocess

import paired_package_wasm_boot as boot
import paired_package_react_phase as phases
import run_paired_package_browser_matrix as browser
import verify_browser_package_resources as resources

CHECKS = {"document", "base", "platform_resources", "configuration", "managed_resources",
          "managed_callback", "cleanup", "observations", "navigation"}
ASSERTIONS = {"wasm_boot", "root_delivery", "prefixed_delivery"}
FIELDS = {"schema", "host", "framework", "version", "phase", "deliveries", "result",
          "reason_category", "browser_version", "cleanup_verified"}
DELIVERY_FIELDS = {"route_prefix", "entry_path", "document", "resources", "boot",
                   "interactive_validation_observed", "checks", "result"}
RESOURCE_FIELDS = {"path", "sha256", "bytes", "content_type", "owner", "status", "requested"}


def require(condition, message):
    if not condition:
        raise ValueError(message)


def _sha(value):
    require(isinstance(value, str) and re.fullmatch(r"[a-f0-9]{64}", value), "Unsafe Hosted proof hash")


def _request(request):
    value = asdict(request) if is_dataclass(request) else dict(request)
    key = browser.identity(value)
    require(key in browser.MATRIX and key[2] == "hosted-wasm" and value.get("route_prefix", "") == "",
            "Hosted delivery requires a canonical root cell")
    return value, key


def _normalized(rows, prefix, expected):
    require(isinstance(rows, list) and len(rows) <= 2048, "Unbounded Hosted observations")
    canonical = {row["path"]: row for row in expected}
    require(len(canonical) == len(expected), "Duplicate Hosted resource inventory")
    aliases = {path: path for path in canonical}
    if prefix:
        aliases.update({"/compat" + path: path for path in canonical})
    seen, normalized = set(), {}
    for row in rows:
        require(isinstance(row, dict) and set(row) == RESOURCE_FIELDS and
                isinstance(row["path"], str) and row["path"] in aliases and row["path"] not in seen,
                "Unbound or duplicate Hosted resource observation")
        seen.add(row["path"])
        path = aliases[row["path"]]
        original = canonical[path]
        require(type(row["bytes"]) is int and row["bytes"] > 0 and
                type(row["status"]) is int and row["status"] == 200 and row["requested"] is True and
                all(row[field] == original[field] for field in ("sha256", "bytes", "content_type", "owner")),
                "Hosted response differs from original verified resource")
        normalized[path] = dict(row, path=path)
    return list(normalized.values())


def validate_receipt(record, request, expected):
    request, key = _request(request)
    require(isinstance(expected, list) and 0 < len(expected) <= 1024 and all(
        isinstance(row, dict) and isinstance(row.get("path"), str) and
        row["path"].startswith(("/_framework/", "/_content/")) and not row["path"].startswith("/compat/")
        for row in expected), "Invalid canonical Hosted inventory")
    resources.verify_browser_resources([dict(row, required=False) for row in expected], [], require_all=False)
    require(isinstance(record, dict) and set(record) == FIELDS and type(record["schema"]) is int and
            record["schema"] == 1 and browser.identity(record) == key and record["phase"] == "hosted-delivery",
            "Unsafe Hosted phase identity")
    require(record["result"] in {"passed", "failed"} and
            record["reason_category"] == (None if record["result"] == "passed" else "delivery_failed") and
            type(record["cleanup_verified"]) is bool, "Unsafe Hosted phase outcome")
    require(record["browser_version"] is None and record["result"] == "failed" or
            isinstance(record["browser_version"], str) and
            re.fullmatch(r"[0-9]+(?:\.[0-9]+){1,3}", record["browser_version"]), "Unsafe Hosted browser version")
    deliveries = record["deliveries"]
    require(isinstance(deliveries, list) and len(deliveries) == 2, "Missing Hosted root/prefix deliveries")
    for delivery, prefix in zip(deliveries, ("", "compat")):
        entry, base = ("/compat/login", "/compat/") if prefix else ("/login", "/")
        require(isinstance(delivery, dict) and set(delivery) == DELIVERY_FIELDS and
                delivery["route_prefix"] == prefix and delivery["entry_path"] == entry,
                "Unsafe Hosted delivery identity")
        checks, document = delivery["checks"], delivery["document"]
        require(isinstance(checks, dict) and set(checks) == CHECKS and all(type(v) is bool for v in checks.values()),
                "Unsafe Hosted delivery checks")
        require(isinstance(document, dict) and set(document) == {"path", "status", "content_type", "base_href_sha256"} and
                document["path"] in (None, entry) and document["content_type"] in (None, "text/html", "other") and
                (document["status"] is None or type(document["status"]) is int and 100 <= document["status"] <= 599),
                "Unsafe Hosted document receipt")
        if document["base_href_sha256"] is not None:
            _sha(document["base_href_sha256"])
        require(checks["document"] is (document["path"] == entry and document["status"] == 200 and
                document["content_type"] == "text/html"), "Hosted document assertion differs")
        require(checks["base"] is (document["base_href_sha256"] == hashlib.sha256(base.encode()).hexdigest()),
                "Hosted base assertion differs")
        callback = delivery["interactive_validation_observed"]
        require(type(callback) is bool and checks["managed_callback"] is callback,
                "Hosted callback assertion differs")
        normalized = _normalized(delivery["resources"], prefix, expected)
        proof = delivery["boot"]
        if proof is None:
            require(not any(checks[name] for name in boot.CHECKS), "Hosted boot assertions lack proof")
        else:
            require(isinstance(proof, dict) and proof.get("checks") == {name: checks[name] for name in boot.CHECKS},
                    "Hosted boot checks differ")
            boot.validate_boot_receipt(proof, all(proof["checks"].values()), normalized, callback, key[1])
            # The shared parser has canonical paths; validate those against the
            # original build only after all actual aliases were independently bound.
            boot.validate_boot_request_binding({"host": "wasm", "framework": key[1],
                "proof": {"wasm_boot": proof}, "resources": normalized}, expected, key[1])
        require(delivery["result"] == ("passed" if all(checks.values()) else "failed"),
                "Hosted delivery result differs")
    require(record["result"] == ("passed" if record["cleanup_verified"] and
            all(row["result"] == "passed" for row in deliveries) else "failed"), "Hosted aggregate result differs")
    return record


def summarize(receipt):
    root, prefixed = receipt["deliveries"]
    cleanup = receipt["cleanup_verified"]
    return {"phase_receipt_sha256": phases.browser_receipt_sha256(receipt),
            "checks": {"root_delivery": cleanup and root["result"] == "passed",
                       "prefixed_delivery": cleanup and prefixed["result"] == "passed",
                       "wasm_boot": cleanup and all(all(row["checks"][name] for name in boot.CHECKS)
                                                    for row in receipt["deliveries"])}}


def validate_summary(value, assertions, key):
    require(key in browser.MATRIX and key[2] == "hosted-wasm" and isinstance(value, dict) and
            set(value) == {"phase_receipt_sha256", "checks"}, "Unsafe Hosted summary")
    _sha(value["phase_receipt_sha256"])
    checks = value["checks"]
    require(isinstance(checks, dict) and set(checks) == ASSERTIONS and all(type(v) is bool for v in checks.values()),
            "Unsafe Hosted summary checks")
    require(all(assertions.get(name) is checks[name] for name in ASSERTIONS), "Hosted assertions differ from phase")


def run_delivery(handle, request, expected, *, timeout=240):
    value, _ = _request(request)
    actual = asdict(handle.request) if is_dataclass(handle.request) else dict(handle.request)
    require(actual == value and type(timeout) is int and 0 < timeout <= 240, "Hosted runtime/request differs")
    payload = {"phase": "hosted-delivery", "request": value, "studio_url": handle.studio_url,
               "backend_url": handle.backend_url, "username": handle.username, "password": handle.password,
               "safe_ids": handle.safe_ids, "resources": expected}
    try:
        completed = browser._run_browser_process(["npm", "exec", "--no", "--", "tsx", str(browser.JOURNEY)],
            cwd=browser.JOURNEY.parent, input=json.dumps(payload), timeout=timeout,
            env={key: value for key, value in os.environ.items() if key in {"PATH", "HOME", "TMPDIR", "SystemRoot"}})
        require(len(completed.stdout.encode()) <= 4 * 1024 * 1024, "Unbounded Hosted child receipt")
        record = json.loads(completed.stdout, object_pairs_hook=phases._unique_object)
    except (OSError, subprocess.TimeoutExpired, json.JSONDecodeError):
        raise ValueError("Hosted browser did not return a safe receipt") from None
    validate_receipt(record, value, expected)
    require(completed.returncode == (0 if record["result"] == "passed" else 1), "Hosted browser exit differs")
    return record
