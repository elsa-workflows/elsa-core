"""Distinct 3.10.1 Feedz executor prototype. Does not replace the 3.10.0 executor.

Single immutable deadline from inspector start (MAX_SECONDS = 1800) covers
admission, metadata, queue, PUT, GET, and cleanup. There is no GitHubApi 120s
clock, no 100-request sidecar budget, and no mutating a shared transport
deadline. Non-publication only: injected exchanges, no network PUT. Fit against
1800s is not claimed.
"""
from __future__ import annotations

from dataclasses import dataclass
import hashlib
import math
import re
import threading
import time

MAX_SECONDS = 1800
MAX_REQUESTS = 5000
HTTP_CAP_SECONDS = 25
READBACK_RESERVE_SECONDS = 900
JOB_TIMEOUT_SECONDS = 3600
REVIEWED_CONCURRENCY = 4  # reviewed constant; not a worst-case guarantee
ACCEPTED_PENDING_PROOF = frozenset({200, 201, 202})
NOT_DUPLICATE_SUCCESS = frozenset({204, 409})
PUBLISH_SECRET = "ELSA_CONSOLIDATED_FEEDZ_PUBLISH_KEY"
METADATA_SECRET = "ELSA_CONSOLIDATED_METADATA_READ_TOKEN"
SHA256 = re.compile(r"[0-9a-f]{64}\Z")
KEY = re.compile(r"([a-z0-9_.-]+\.pdb)/([0-9a-f]{32}FFFFFFFF)/\1\Z")


class PrototypeError(ValueError):
    """Fixed categories only. Never include URLs or credentials."""


def require(condition, code):
    if not condition:
        raise PrototypeError(code)


def sha(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


@dataclass(frozen=True)
class ImmutableDeadline:
    """Created at inspector start. Never mutated; remaining is derived."""

    start: float
    seconds: int = MAX_SECONDS

    def remaining(self, now: float) -> float:
        return self.start + self.seconds - now

    def expired(self, now: float) -> bool:
        return self.remaining(now) <= 0


@dataclass
class Reservation:
    kind: str
    slots: int
    remaining_proofs: int
    resource: str
    state: str = "reserved"


class Coordinator:
    """Atomic owner of MAX_REQUESTS, outstanding proof reservations, and time."""

    def __init__(self, deadline: ImmutableDeadline, *, max_requests: int = MAX_REQUESTS,
                 concurrency: int = REVIEWED_CONCURRENCY, http_cap: int = HTTP_CAP_SECONDS,
                 reserve: int = READBACK_RESERVE_SECONDS, clock=time.monotonic):
        require(isinstance(deadline, ImmutableDeadline), "deadline_invalid")
        require(max_requests == MAX_REQUESTS, "request_budget_invalid")
        require(http_cap == HTTP_CAP_SECONDS, "http_cap_invalid")
        require(reserve == READBACK_RESERVE_SECONDS, "reserve_invalid")
        require(concurrency >= 1, "concurrency_invalid")
        self.deadline = deadline
        self.max_requests = max_requests
        self.concurrency = concurrency
        self.http_cap = http_cap
        self.reserve = reserve
        self.clock = clock
        self._lock = threading.Lock()
        self.used = 0
        self.outstanding_proof = 0
        self.queued_puts = 0
        self.inflight_puts = 0
        self.queued_gets = 0
        self.inflight_gets = 0
        self.put_admission_stopped = False
        self.reservations: list[Reservation] = []

    def _now(self) -> float:
        return self.clock()

    def _worst_case_seconds(self, puts: int, gets: int) -> float:
        # A PUT that still needs proof occupies a lane for PUT then GET.
        # Concurrency does not shrink HTTP_CAP and is not a worst-case guarantee.
        put_waves = math.ceil(puts / self.concurrency) if puts else 0
        get_waves = math.ceil(gets / self.concurrency) if gets else 0
        return put_waves * (2 * self.http_cap) + get_waves * self.http_cap

    def _proof_capacity_ok(self, extra_puts: int, extra_gets: int, now: float) -> bool:
        remaining = self.deadline.remaining(now)
        if remaining <= self.reserve:
            return False
        puts = self.queued_puts + self.inflight_puts + extra_puts
        gets = self.queued_gets + self.inflight_gets + extra_gets
        return self._worst_case_seconds(puts, gets) <= remaining

    def admit_put(self, resource: str, *, proof_slots: int = 1) -> Reservation | str:
        """Pre-transmission. Returns reservation or not_attempted / readback_budget_exhausted."""
        require(type(proof_slots) is int and proof_slots >= 1, "proof_slots_invalid")
        with self._lock:
            now = self._now()
            if self.put_admission_stopped or self.deadline.expired(now):
                return "not_attempted"
            if self.used + 1 + proof_slots > self.max_requests:
                self.put_admission_stopped = True
                return "readback_budget_exhausted"
            extra_gets = max(0, proof_slots - 1)
            if not self._proof_capacity_ok(1, extra_gets, now):
                self.put_admission_stopped = True
                return "readback_budget_exhausted"
            self.used += 1
            self.queued_puts += 1
            self.outstanding_proof += proof_slots
            reservation = Reservation("put", 1, proof_slots, resource)
            self.reservations.append(reservation)
            return reservation

    def start_put(self, reservation: Reservation) -> None:
        with self._lock:
            require(reservation.state == "reserved", "reservation_invalid")
            reservation.state = "inflight"
            self.queued_puts -= 1
            self.inflight_puts += 1

    def finish_put(self, reservation: Reservation, *, transmitted: bool) -> None:
        with self._lock:
            require(reservation.state == "inflight", "reservation_invalid")
            reservation.state = "put_done" if transmitted else "acceptance_unknown"
            self.inflight_puts -= 1
            if transmitted:
                self.queued_gets += reservation.remaining_proofs
            else:
                # Transmission timeout: do not spend GET slots; preserve proof capacity.
                self.outstanding_proof -= reservation.remaining_proofs
                reservation.remaining_proofs = 0
                self.put_admission_stopped = True

    def admit_proof_get(self, reservation: Reservation) -> Reservation | str:
        with self._lock:
            now = self._now()
            if reservation.state not in {"put_done", "proving"} or reservation.remaining_proofs < 1:
                return "not_attempted"
            if self.deadline.expired(now) or self.used + 1 > self.max_requests:
                self.put_admission_stopped = True
                return "readback_budget_exhausted"
            self.used += 1
            self.queued_gets -= 1
            self.inflight_gets += 1
            reservation.remaining_proofs -= 1
            reservation.state = "proving"
            return reservation

    def finish_proof_get(self, reservation: Reservation) -> None:
        with self._lock:
            require(reservation.state == "proving", "reservation_invalid")
            self.inflight_gets -= 1
            self.outstanding_proof -= 1
            reservation.state = "cleaned" if reservation.remaining_proofs == 0 else "put_done"

    def abandon_remaining_proofs(self, reservation: Reservation) -> None:
        with self._lock:
            leftover = reservation.remaining_proofs
            if leftover:
                self.queued_gets -= leftover
                self.outstanding_proof -= leftover
                reservation.remaining_proofs = 0
            if reservation.state != "proving":
                reservation.state = "cleaned"

    def admit_get(self, resource: str) -> Reservation | str:
        """Independent GET (feed index, pre-GET, metadata). Not post-PUT proof.

        Readback reserve stops PUT admission only, so outstanding GET/cleanup can
        still run while remaining time is positive.
        """
        with self._lock:
            now = self._now()
            if self.deadline.expired(now) or self.used + 1 > self.max_requests:
                return "not_attempted"
            self.used += 1
            self.inflight_gets += 1
            reservation = Reservation("get", 1, 0, resource, state="inflight")
            self.reservations.append(reservation)
            return reservation

    def finish_get(self, reservation: Reservation) -> None:
        with self._lock:
            require(reservation.state == "inflight" and reservation.kind == "get", "reservation_invalid")
            reservation.state = "cleaned"
            self.inflight_gets -= 1


@dataclass
class ExchangeResult:
    status: int | None
    body: bytes
    complete: bool
    failure_category: str | None
    transmitted: bool


class InjectedExchange:
    """Offline stand-in. Production network PUT is not authorized on this prototype."""

    def __init__(self, handler):
        self.handler = handler
        self.calls = []

    def __call__(self, method: str, url: str, *, headers: dict, data: bytes, timeout: float) -> ExchangeResult:
        self.calls.append({"method": method, "url": url, "header_names": tuple(sorted(headers)),
                           "data_sha256": sha(data) if data else None, "timeout": timeout})
        result = self.handler(method, url, headers=headers, data=data, timeout=timeout)
        require(isinstance(result, ExchangeResult), "invalid_transport")
        return result


class SharedTransport:
    """All HTTP, including admission metadata, uses the coordinator deadline."""

    def __init__(self, coordinator: Coordinator, exchange: InjectedExchange):
        require(isinstance(coordinator, Coordinator), "coordinator_invalid")
        require(isinstance(exchange, InjectedExchange), "offline_exchange_required")
        self.coordinator = coordinator
        self.exchange = exchange
        # No mutable deadline attribute. Tests assert absence.

    def _timeout(self, now: float) -> float:
        remaining = self.coordinator.deadline.remaining(now)
        require(remaining > 0, "deadline_exceeded")
        return min(HTTP_CAP_SECONDS, remaining)

    def request(self, method: str, url: str, *, headers=None, data=b"", credential_role=None) -> ExchangeResult:
        headers = dict(headers or {})
        require(method in {"GET", "PUT"}, "unsupported_method")
        require(set(headers) <= {"Authorization", "X-NuGet-ApiKey", "Content-Type"}, "unsafe_headers")
        if credential_role == "metadata":
            require(method == "GET" and "Authorization" in headers and "X-NuGet-ApiKey" not in headers,
                    "credential_separation")
        elif credential_role == "publish":
            require(method == "PUT" and "X-NuGet-ApiKey" in headers and "Authorization" not in headers,
                    "credential_separation")
        else:
            require("Authorization" not in headers and "X-NuGet-ApiKey" not in headers, "credential_separation")
        timeout = self._timeout(self.coordinator.clock())
        return self.exchange(method, url, headers=headers, data=data, timeout=timeout)


@dataclass
class SymbolKey:
    key: str
    pdb_sha256: str
    snupkg_id: str
    pdb_bytes: bytes


class SymbolIndex:
    """Preflight: same key / different bytes is a conflict. Overlaps serialize."""

    def __init__(self):
        self.by_key: dict[str, SymbolKey] = {}
        self.by_snupkg: dict[str, set[str]] = {}

    def add(self, item: SymbolKey) -> None:
        require(isinstance(item.key, str) and KEY.fullmatch(item.key), "symbol_key_invalid")
        existing = self.by_key.get(item.key)
        if existing is not None and existing.pdb_bytes != item.pdb_bytes:
            raise PrototypeError("duplicate_symbol_key_conflict")
        self.by_key.setdefault(item.key, item)
        self.by_snupkg.setdefault(item.snupkg_id, set()).add(item.key)

    def overlapping_snupkgs(self, snupkg_id: str) -> set[str]:
        keys = self.by_snupkg.get(snupkg_id, set())
        owners = set()
        for key in keys:
            for other, other_keys in self.by_snupkg.items():
                if other != snupkg_id and key in other_keys:
                    owners.add(other)
        return owners


@dataclass
class Operation:
    id: str
    kind: str  # nupkg | snupkg
    archive_sha256: str
    archive: bytes
    state: str = "not_attempted"
    put_status: int | None = None
    put_outcome: str | None = None
    get_status: int | None = None
    get_classification: str | None = None
    failure_category: str | None = None


def classify_put(result: ExchangeResult) -> tuple[str, str | None]:
    if not result.transmitted:
        return "acceptance_unknown", result.failure_category or "timeout"
    if result.status in ACCEPTED_PENDING_PROOF:
        return "accepted_pending_proof", None
    if result.status in NOT_DUPLICATE_SUCCESS:
        return "not_duplicate_success", "ambiguous_put"
    return "acceptance_unknown", result.failure_category or "ambiguous_put"


def classify_archive_get(result: ExchangeResult, original: bytes) -> tuple[str, str | None]:
    """Full original nupkg SHA equality. Normalized payload is not proof."""
    if not result.complete or result.failure_category or result.status is None:
        return "unverifiable", result.failure_category or "incomplete_response"
    if result.status == 404:
        return "missing", None
    if result.status != 200:
        return "unverifiable", "unexpected_status"
    if sha(result.body) != sha(original):
        return "conflicting", "archive_sha_mismatch"
    return "matching", None


class Prototype3101:
    """Offline 3.10.1 executor. remote_snupkg_archive_verified stays false here."""

    def __init__(self, coordinator: Coordinator, transport: SharedTransport, symbols: SymbolIndex):
        self.coordinator = coordinator
        self.transport = transport
        self.symbols = symbols
        self.remote_snupkg_archive_verified = False
        self._snupkg_locks: dict[str, threading.Lock] = {}
        self._key_locks: dict[str, threading.Lock] = {}
        self._guard = threading.Lock()

    def _lock_for(self, table: dict, name: str) -> threading.Lock:
        with self._guard:
            return table.setdefault(name, threading.Lock())

    def preflight_keys(self, items: list[SymbolKey]) -> None:
        for item in items:
            self.symbols.add(item)

    def metadata_get(self, url: str, token: str) -> ExchangeResult:
        """Admission metadata uses the same coordinator clock and request budget."""
        reservation = self.coordinator.admit_get(url)
        if isinstance(reservation, str):
            raise PrototypeError(reservation)
        try:
            return self.transport.request(
                "GET", url, headers={"Authorization": "Bearer " + token}, credential_role="metadata")
        finally:
            self.coordinator.finish_get(reservation)

    def prove_pdb_association(self, key: str, expected: bytes) -> dict:
        """Matching PDB bytes are association proof, not snupkg archive proof."""
        reservation = self.coordinator.admit_get(key)
        if isinstance(reservation, str):
            return {"state": reservation, "classification": "unverifiable"}
        try:
            result = self.transport.request("GET", key)
            classification, failure = classify_archive_get(result, expected)
            self.remote_snupkg_archive_verified = False
            return {"state": "observed", "classification": classification, "failure_category": failure,
                    "status": result.status, "remote_snupkg_archive_verified": False}
        finally:
            self.coordinator.finish_get(reservation)

    def pre_get_nupkg(self, url: str, original: bytes) -> dict:
        """Pre-GET observes; it does not lock and is not post-PUT proof."""
        reservation = self.coordinator.admit_get(url)
        if isinstance(reservation, str):
            return {"state": reservation, "classification": "unverifiable"}
        try:
            result = self.transport.request("GET", url)
            classification, failure = classify_archive_get(result, original)
            return {"state": "observed", "classification": classification, "failure_category": failure,
                    "status": result.status, "archive_sha256": sha(result.body) if result.complete else None}
        finally:
            self.coordinator.finish_get(reservation)

    def put_and_prove_nupkg(self, operation: Operation, url: str, *, publish_key: str) -> Operation:
        require(operation.kind == "nupkg", "kind_invalid")
        require(sha(operation.archive) == operation.archive_sha256, "original_archive_changed")
        reservation = self.coordinator.admit_put(operation.id)
        if isinstance(reservation, str):
            operation.state = reservation
            operation.failure_category = reservation
            return operation
        self.coordinator.start_put(reservation)
        result = self.transport.request(
            "PUT", url, headers={"X-NuGet-ApiKey": publish_key, "Content-Type": "application/octet-stream"},
            data=operation.archive, credential_role="publish")
        self.coordinator.finish_put(reservation, transmitted=result.transmitted)
        operation.put_status = result.status
        operation.put_outcome, failure = classify_put(result)
        operation.failure_category = failure
        if operation.put_outcome != "accepted_pending_proof":
            operation.state = operation.put_outcome
            return operation
        proof = self.coordinator.admit_proof_get(reservation)
        if isinstance(proof, str):
            operation.state = proof
            operation.failure_category = proof
            return operation
        get = self.transport.request("GET", url)
        operation.get_status = get.status
        operation.get_classification, get_failure = classify_archive_get(get, operation.archive)
        if get_failure:
            operation.failure_category = get_failure
        operation.state = operation.get_classification
        self.coordinator.finish_proof_get(reservation)
        return operation

    def put_snupkg_serialized(self, operation: Operation, url: str, *, publish_key: str) -> Operation:
        """Overlapping snupkg keys serialize upload and readback, then re-evaluate."""
        require(operation.kind == "snupkg", "kind_invalid")
        require(sha(operation.archive) == operation.archive_sha256, "original_archive_changed")
        overlap = sorted(self.symbols.overlapping_snupkgs(operation.id))
        names = tuple(sorted({*overlap, operation.id}))
        locks = [self._lock_for(self._snupkg_locks, name) for name in names]
        for lock in locks:
            lock.acquire()
        try:
            # PDB GET is association proof, not original snupkg archive proof.
            self.remote_snupkg_archive_verified = False
            keys = sorted(self.symbols.by_snupkg.get(operation.id, ()))
            proof_slots = max(1, len(keys))
            reservation = self.coordinator.admit_put(operation.id, proof_slots=proof_slots)
            if isinstance(reservation, str):
                operation.state = reservation
                operation.failure_category = reservation
                return operation
            self.coordinator.start_put(reservation)
            result = self.transport.request(
                "PUT", url, headers={"X-NuGet-ApiKey": publish_key, "Content-Type": "application/octet-stream"},
                data=operation.archive, credential_role="publish")
            self.coordinator.finish_put(reservation, transmitted=result.transmitted)
            operation.put_status = result.status
            operation.put_outcome, failure = classify_put(result)
            operation.failure_category = failure
            if operation.put_outcome != "accepted_pending_proof":
                operation.state = operation.put_outcome
                return operation
            operation.get_classification = "pdb_association_pending"
            for key in keys or ("snupkg-unverified",):
                proof = self.coordinator.admit_proof_get(reservation)
                if isinstance(proof, str):
                    operation.state = proof
                    operation.failure_category = proof
                    self.coordinator.abandon_remaining_proofs(reservation)
                    return operation
                if key == "snupkg-unverified":
                    operation.get_classification = "snupkg_archive_unverified"
                    self.coordinator.finish_proof_get(reservation)
                    break
                item = self.symbols.by_key[key]
                get = self.transport.request("GET", key)
                operation.get_status = get.status
                classification, get_failure = classify_archive_get(get, item.pdb_bytes)
                self.coordinator.finish_proof_get(reservation)
                if classification != "matching":
                    operation.get_classification = classification
                    operation.failure_category = get_failure or "pdb_association_unverified"
                    break
            else:
                operation.get_classification = "pdb_association_matching"
            self.coordinator.abandon_remaining_proofs(reservation)
            operation.state = operation.put_outcome
            self.remote_snupkg_archive_verified = False
            return operation
        finally:
            for lock in reversed(locks):
                lock.release()


def worst_case_http_seconds(requests: int, *, concurrency: int = REVIEWED_CONCURRENCY) -> float:
    waves = math.ceil(requests / concurrency) if requests else 0
    return waves * HTTP_CAP_SECONDS


def request_model(*, nupkgs: int, snupkgs: int, symbol_associations: int, unique_pdb_keys: int,
                  admission_gets: int) -> dict:
    """HTTP model. 465 = 2*nupkgs + 3 when nupkgs=231 (two nupkg GET passes + 3 extras)."""
    two_nupkg_passes_plus_three = 2 * nupkgs + 3
    assumed_s, assumed_u = 688, 462
    assumed_total = two_nupkg_passes_plus_three + 2 * assumed_s + assumed_u
    measured_formula_total = two_nupkg_passes_plus_three + 2 * symbol_associations + unique_pdb_keys
    before_gets = admission_gets + 1 + nupkgs + unique_pdb_keys
    puts = nupkgs + snupkgs
    after_gets = nupkgs + unique_pdb_keys
    measured_total = before_gets + puts + after_gets
    before_worst_c4 = worst_case_http_seconds(before_gets, concurrency=4)
    put_then_get_c4 = math.ceil(puts / 4) * (2 * HTTP_CAP_SECONDS) if puts else 0
    return {
        "two_nupkg_passes_plus_three": two_nupkg_passes_plus_three,
        "architect_formula": "465+2S+U",
        "architect_assumed": {"S": assumed_s, "U": assumed_u, "total": assumed_total},
        "measured_counts": {
            "nupkgs": nupkgs, "snupkgs": snupkgs,
            "symbol_associations_S": symbol_associations,
            "unique_pdb_bytes_U": unique_pdb_keys,
            "admission_gets": admission_gets,
        },
        "measured_formula_465_2S_U": measured_formula_total,
        "measured_http_total": measured_total,
        "worst_case_seconds_sequential": measured_total * HTTP_CAP_SECONDS,
        "worst_case_seconds_concurrency_4_naive_waves": worst_case_http_seconds(measured_total, concurrency=4),
        "worst_case_seconds_before_observation_c4": before_worst_c4,
        "worst_case_seconds_put_then_get_c4": put_then_get_c4,
        "max_seconds": MAX_SECONDS,
        "max_requests": MAX_REQUESTS,
        "http_cap_seconds": HTTP_CAP_SECONDS,
        "readback_reserve_seconds": READBACK_RESERVE_SECONDS,
        "job_timeout_seconds": JOB_TIMEOUT_SECONDS,
        "fits_worst_case_1800": False,
        "concurrency_4_worst_case_guarantee": False,
        "safe_1800_demonstrated": False,
        "fit_claim": False,
    }


def offline_measurement_report() -> dict:
    """Pinned counts from unpublished artifact 13609452227 original.zip. No network PUT."""
    return request_model(
        nupkgs=231, snupkgs=231, symbol_associations=688, unique_pdb_keys=688, admission_gets=3)

