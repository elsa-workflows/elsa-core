#!/usr/bin/env python3
"""Rehearse recovery with original local bytes and a simulated feed; never publish."""
from __future__ import annotations

import argparse
import copy
import hashlib
import io
import json
from pathlib import Path
import zipfile

import consolidated_package_recovery as recovery


class SimulatedAcceptanceInterrupted(Exception):
    """The fake feed accepted bytes before the simulated caller stopped."""


class SimulatedFeed:
    """One in-memory feed model shared by contracts and full-inventory rehearsal."""

    base = "https://f.feedz.io/elsa-workflows/elsa-3/nuget/v3-flatcontainer/"

    def __init__(self, root: Path, outcome: int = 404):
        self.root, self.outcome = root, outcome
        self.calls, self.overrides = [], {}
        self.accepted, self.visible = {}, set()
        self.simulated_acceptance_calls = 0
        self.index = recovery.ReadResult(200, json.dumps({"version": "3.0.0", "resources": [
            {"@type": "PackageBaseAddress/3.0.0", "@id": self.base}]}).encode(), True)
        self.packages = {path.name.lower(): path for path in (root / "artifacts").glob("*.nupkg")}

    def simulate_accept(self, package_id: str, data: bytes, *, visible=True, interrupt=False):
        key = package_id.lower()
        if key in self.accepted:
            raise ValueError("Simulation refuses a duplicate acceptance")
        self.simulated_acceptance_calls += 1
        self.accepted[key] = data
        if visible:
            self.visible.add(key)
        if interrupt:
            raise SimulatedAcceptanceInterrupted()

    def get(self, url: str, **kwargs) -> recovery.ReadResult:
        # Keep only request identity. Credentials never enter retained fake-feed state.
        self.calls.append(url)
        if url == recovery.FEED_INDEX:
            return self.index
        key = url.split("/")[-3]
        override = self.overrides.get(key)
        if isinstance(override, BaseException):
            raise override
        if override is not None:
            return override
        if key in self.accepted:
            return recovery.ReadResult(200, self.accepted[key], True) if key in self.visible else recovery.ReadResult(404, b"", True)
        if self.outcome == 200:
            return recovery.ReadResult(200, self.packages[url.rsplit("/", 1)[1]].read_bytes(), True)
        return recovery.ReadResult(self.outcome, b"", True)


def _require(condition, message):
    if not condition:
        raise ValueError(message)


def _remote_mutation(data: bytes, *, duplicate: bool = False) -> bytes:
    """Only a simulated remote response is changed; original archives stay intact."""
    output = io.BytesIO()
    with zipfile.ZipFile(io.BytesIO(data)) as source, zipfile.ZipFile(output, "w") as target:
        for info in source.infolist():
            # ZipFile.writestr mutates ZipInfo offsets/flags. Preserve the source
            # metadata so subsequent reads still address the original archive.
            target.writestr(copy.copy(info), source.read(info))
        if duplicate:
            nuspec = next(info for info in source.infolist() if info.filename.endswith(".nuspec"))
            target.writestr(nuspec.filename.upper(), source.read(nuspec))
        else:
            target.writestr("simulated-conflicting-content.txt", b"not the accepted original payload")
    return output.getvalue()


def rehearse(root: Path, provenance: dict) -> dict:
    manifest = json.loads((root / "verified-artifacts.json").read_text())
    rows = manifest["packages"]
    package_ids = {row["id"] for row in rows}
    _require(len(package_ids) == 225, "Full original inventory required")
    first = rows[0]
    first_key = first["id"].lower()
    original = (root / "artifacts" / first["nupkg"]).read_bytes()
    feed = SimulatedFeed(root)
    results = []

    def observe(name, expected):
        before = len(feed.calls)
        acceptances = feed.simulated_acceptance_calls
        receipt = recovery.plan_recovery(root, provenance, transport=feed)
        _require(receipt["counts"] == expected, "Scenario classification mismatch: " + name)
        _require({row["id"] for row in receipt["packages"]} == package_ids, "Incomplete scenario inventory")
        _require(receipt["publication_performed"] is False and receipt["publication_ready"] is False
                 and receipt["remote_symbols_verified"] is False, "Simulation promoted publication authority")
        _require(feed.calls[before] == recovery.FEED_INDEX, "Fresh invocation did not inspect remote index")
        _require(feed.simulated_acceptance_calls == acceptances, "Read-only reconciliation accepted bytes")
        results.append({"scenario": name, "requests": len(feed.calls) - before,
                        "simulated_accepted_count": len(feed.accepted), "simulated_visible_count": len(feed.visible),
                        "acceptance_operations_by_planner": 0, "receipt": receipt})
        return receipt

    def counts(missing=0, matching=0, conflicting=0, unverifiable=0):
        return dict(missing=missing, matching=matching, conflicting=conflicting, unverifiable=unverifiable)

    observe("all_missing", counts(missing=225))
    feed.outcome = 200
    observe("all_matching", counts(matching=225))
    # An arbitrary noncontiguous subset has become visible after simulated acceptance.
    accepted = [row for index, row in enumerate(rows) if index % 3 == 1]
    feed.outcome = 404
    for row in accepted:
        feed.simulate_accept(row["id"], (root / "artifacts" / row["nupkg"]).read_bytes())
    observe("arbitrary_partial", counts(matching=len(accepted), missing=225-len(accepted)))
    # A simulated process stopped after acceptance. A new invocation queries truth;
    # there is no prior push exit code, skip-duplicate flag or cached success input.
    interrupted = False
    try:
        feed.simulate_accept(first["id"], original, interrupt=True)
    except SimulatedAcceptanceInterrupted:
        interrupted = True
    _require(interrupted and feed.accepted[first_key] == original, "Acceptance did not precede interruption")
    observe("interruption_after_simulated_acceptance", counts(matching=len(accepted)+1, missing=224-len(accepted)))
    feed.overrides[first_key] = recovery.ReadResult(None, b"", False, "timeout")
    observe("uncertain_outcome", counts(matching=len(accepted), missing=224-len(accepted), unverifiable=1))
    feed.overrides = {}
    feed.visible.remove(first_key)
    delayed = observe("delayed_visibility", counts(matching=len(accepted), missing=225-len(accepted)))
    _require(delayed["content_converged"] is False, "Delayed visibility certified convergence")
    acceptances = feed.simulated_acceptance_calls
    feed.visible.add(first_key)
    observe("visibility_recovered_without_reacceptance", counts(matching=len(accepted)+1, missing=224-len(accepted)))
    _require(feed.simulated_acceptance_calls == acceptances, "Visibility recovery retried an uncertain acceptance")
    feed.overrides = {first_key: recovery.ReadResult(200, _remote_mutation(original), True)}
    feed.outcome = 200
    observe("conflicting_payload", counts(matching=224, conflicting=1))
    feed.overrides[first_key] = recovery.ReadResult(200, _remote_mutation(original, duplicate=True), True)
    observe("duplicate_archive_identity", counts(matching=224, unverifiable=1))
    feed.overrides[first_key] = recovery.ReadResult(503, b"unretained synthetic body", True)
    observe("unreadable_package", counts(matching=224, unverifiable=1))
    feed.overrides = {}
    index = feed.index
    feed.index = recovery.ReadResult(200, b'{"version":"3.0.0","resources":[],"resources":[]}', True)
    observe("ambiguous_feed", counts(unverifiable=225))
    feed.index = index
    feed.overrides[first_key] = KeyboardInterrupt()
    observe("interrupted_observation", counts(unverifiable=225))
    feed.overrides = {}
    feed.outcome = 404
    for row in rows:
        if row["id"].lower() not in feed.accepted:
            feed.simulate_accept(row["id"], (root / "artifacts" / row["nupkg"]).read_bytes())
    final = observe("eventual_full_set_convergence", counts(matching=225))
    _require(final["content_converged"] is True, "Full set did not converge")
    # Re-read every archive pin after all simulated responses to prove no mutation.
    for row in rows:
        for extension in ("nupkg", "snupkg"):
            with (root / "artifacts" / row[extension]).open("rb") as stream:
                actual = hashlib.file_digest(stream, "sha256").hexdigest()
            _require(actual == row[extension + "_sha256"], "Original archive changed during simulation")
    return {"schema": 1, "result": "passed", "scope": "local_simulation_only", "publication_performed": False,
            "original_provenance": provenance, "package_count": len(rows), "exclusion_count": len(manifest["exclusions"]),
            "original_archives_unchanged": True, "simulated_acceptance_calls": feed.simulated_acceptance_calls,
            "scenarios": results}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ("inputs", "verified-root", "output"):
        parser.add_argument("--" + name, required=True, type=Path)
    parser.add_argument("--planner-source", required=True)
    parser.add_argument("--planner-run", type=int)
    parser.add_argument("--planner-attempt", type=int)
    args = parser.parse_args()
    receipt = {"schema": 1, "result": "failed", "scope": "local_simulation_only", "publication_performed": False,
               "failure_category": "local_input_invalid"}
    try:
        recovery.validate_cli_paths(args.inputs, args.verified_root, args.output)
        from consolidated_recovery_inputs import prepare_recovery_inputs
        root, provenance = prepare_recovery_inputs(args.inputs, args.verified_root, planner_source=args.planner_source,
                                                   planner_run=args.planner_run, planner_attempt=args.planner_attempt)
        receipt["failure_category"] = "simulation_failed"
        receipt = rehearse(root, provenance)
    except Exception:
        # Original manifest and parser exceptions may contain private paths.
        pass
    try:
        recovery.validate_cli_paths(args.inputs, args.verified_root, args.output)
        args.output.parent.mkdir(parents=True, exist_ok=True)
        with args.output.open("x") as stream:
            json.dump(receipt, stream, sort_keys=True, indent=2)
            stream.write("\n")
    except (OSError, recovery.RecoveryError):
        print('{"result":"failed","failure_category":"receipt_write_failed","publication_performed":false}')
        return 1
    print(json.dumps({key: receipt[key] for key in ("result", "scope", "publication_performed")}))
    return 0 if receipt["result"] == "passed" else 1


if __name__ == "__main__":
    raise SystemExit(main())
