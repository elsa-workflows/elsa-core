"""Distinct 3.10.1 Feedz executor prototype. Does not replace the 3.10.0 executor.

Single immutable deadline from inspector start (MAX_SECONDS = 1800) covers
admission, metadata, queue, PUT, GET, cleanup, and receipt. There is no
GitHubApi 120s clock, no 100-request sidecar, and no mutating a shared
transport deadline. Injected exchanges only: no network PUT and no publish
authority. Fit against 1800s is not claimed.
"""
from __future__ import annotations

from dataclasses import dataclass
import hashlib
import json
import math
import re
import threading
import time

MAX_SECONDS = 1800
MAX_REQUESTS = 5000
HTTP_CAP_SECONDS = 25
READBACK_RESERVE_SECONDS = 900
CLEANUP_MARGIN_SECONDS = 30
JOB_TIMEOUT_SECONDS = 3600
REVIEWED_CONCURRENCY = 4
MAX_BYTES = 32 * 1024 * 1024
MAX_RECEIPT_BYTES = 1024 * 1024
ACCEPTED_PENDING_PROOF = frozenset({200, 201, 202})
NOT_DUPLICATE_SUCCESS = frozenset({204, 409})
DEFINITELY_NOT_SENT = "definitely_not_sent"
MAY_HAVE_SENT = "may_have_sent"
RESPONSE_COMPLETE = "response_complete"
TRUSTED = "trusted_inspector_manifest"
UNVERIFIED = "unverified_fixture"
PUBLISH_SECRET = "ELSA_CONSOLIDATED_FEEDZ_PUBLISH_KEY"
METADATA_SECRET = "ELSA_CONSOLIDATED_METADATA_READ_TOKEN"
FEED_INDEX = "https://f.feedz.io/elsa-workflows/elsa-3/nuget/index.json"
PACKAGE_BASE = "https://f.feedz.io/elsa-workflows/elsa-3/nuget/v3/packages"
PACKAGE_PUBLISH = "https://f.feedz.io/elsa-workflows/elsa-3/nuget"
SYMBOL_PUBLISH = "https://f.feedz.io/elsa-workflows/elsa-3/symbols"
METADATA_PREFIX = "https://api.github.com/"
METADATA_PATH = re.compile(
    r"repos/elsa-workflows/elsa-core/(?:actions/(?:secrets|organization-secrets)|"
    r"environments/feedz-publish/secrets)\?per_page=100&page=[1-9][0-9]*\Z")
SHA256 = re.compile(r"[0-9a-f]{64}\Z")
KEY = re.compile(r"([a-z0-9_.-]+\.pdb)/([0-9a-f]{32}FFFFFFFF)/\1\Z")
PACKAGE_GET = re.compile(
    r"https://f\.feedz\.io/elsa-workflows/elsa-3/nuget/v3/packages/"
    r"[a-z0-9_.-]+/[0-9]+\.[0-9]+\.[0-9]+(?:-[a-z0-9_.-]+)?/[a-z0-9_.-]+\.nupkg\Z")


class PrototypeError(ValueError):
    """Fixed categories only. Never include URLs or credentials."""


def require(condition, code):
    if not condition:
        raise PrototypeError(code)


def sha(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def nupkg_get_url(package_id: str, version: str = "3.10.1") -> str:
    name = package_id.lower()
    return f"{PACKAGE_BASE}/{name}/{version}/{name}.{version}.nupkg"


def symbol_get_url(key: str) -> str:
    require(isinstance(key, str) and KEY.fullmatch(key), "symbol_key_invalid")
    return f"{SYMBOL_PUBLISH}/{key}"


@dataclass(frozen=True)
class ImmutableDeadline:
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
    committed_slots: int = 0


@dataclass
class ExchangeResult:
    status: int | None
    body: bytes
    complete: bool
    failure_category: str | None
    disposition: str
    redirected: bool = False


class BoundedReceipt:
    """Append-only bounded receipt. Partial rows survive exceptions."""

    def __init__(self, *, max_bytes: int = MAX_RECEIPT_BYTES):
        self.max_bytes = max_bytes
        self.size = 0
        self.rows: list[dict] = []

    def append(self, event: dict) -> None:
        payload = json.dumps(event, sort_keys=True, default=str) + "\n"
        require(self.size + len(payload.encode()) <= self.max_bytes, "receipt_limit")
        self.rows.append(event)
        self.size += len(payload.encode())


class Coordinator:
    """Atomic owner of committed request slots, including reserved proofs."""

    def __init__(self, deadline: ImmutableDeadline, *, max_requests: int = MAX_REQUESTS,
                 concurrency: int = REVIEWED_CONCURRENCY, http_cap: int = HTTP_CAP_SECONDS,
                 reserve: int = READBACK_RESERVE_SECONDS, cleanup: int = CLEANUP_MARGIN_SECONDS,
                 clock=time.monotonic):
        require(isinstance(deadline, ImmutableDeadline), "deadline_invalid")
        require(max_requests == MAX_REQUESTS, "request_budget_invalid")
        require(http_cap == HTTP_CAP_SECONDS, "http_cap_invalid")
        require(reserve == READBACK_RESERVE_SECONDS, "reserve_invalid")
        require(cleanup == CLEANUP_MARGIN_SECONDS, "cleanup_margin_invalid")
        require(concurrency == REVIEWED_CONCURRENCY, "concurrency_invalid")
        self.deadline = deadline
        self.max_requests = max_requests
        self.concurrency = concurrency
        self.http_cap = http_cap
        self.reserve = reserve
        self.cleanup = cleanup
        self.clock = clock
        self._lock = threading.Lock()
        self._exchange = threading.BoundedSemaphore(concurrency)
        self.used = 0
        self.reserved_proof = 0
        self.outstanding_proof = 0
        self.queued_puts = 0
        self.inflight_puts = 0
        self.queued_gets = 0
        self.inflight_gets = 0
        self.put_admission_stopped = False
        self.writes_halted = False
        self.abandoned = False
        self.reservations: list[Reservation] = []

    def _now(self) -> float:
        return self.clock()

    def _usable_remaining(self, now: float) -> float:
        return self.deadline.remaining(now) - self.cleanup

    def admit_put(self, resource: str, *, proof_slots: int = 1) -> Reservation | str:
        require(type(proof_slots) is int and proof_slots >= 1, "proof_slots_invalid")
        with self._lock:
            now = self._now()
            if self.abandoned or self.put_admission_stopped or self.writes_halted or self.deadline.expired(now):
                return "not_attempted"
            need = 1 + proof_slots
            if self.used + need > self.max_requests:
                self.put_admission_stopped = True
                return "readback_budget_exhausted"
            remaining = self.deadline.remaining(now)
            if remaining <= self.reserve:
                self.put_admission_stopped = True
                return "readback_budget_exhausted"
            sequential = need * self.http_cap + self.cleanup
            if remaining < sequential:
                self.put_admission_stopped = True
                return "readback_budget_exhausted"
            self.used += need
            self.reserved_proof += proof_slots
            self.outstanding_proof += proof_slots
            self.queued_puts += 1
            reservation = Reservation("put", need, proof_slots, resource, committed_slots=need)
            self.reservations.append(reservation)
            return reservation

    def start_put(self, reservation: Reservation) -> None:
        with self._lock:
            require(reservation.state == "reserved", "reservation_invalid")
            reservation.state = "inflight"
            self.queued_puts -= 1
            self.inflight_puts += 1

    def finish_put(self, reservation: Reservation, *, disposition: str) -> None:
        with self._lock:
            require(reservation.state == "inflight", "reservation_invalid")
            self.inflight_puts -= 1
            if disposition == DEFINITELY_NOT_SENT:
                self.used -= reservation.remaining_proofs
                self.reserved_proof -= reservation.remaining_proofs
                self.outstanding_proof -= reservation.remaining_proofs
                reservation.remaining_proofs = 0
                reservation.state = "not_attempted"
                return
            self.queued_gets += reservation.remaining_proofs
            reservation.state = "put_done"
            if disposition == MAY_HAVE_SENT:
                self.put_admission_stopped = True

    def halt_puts(self) -> None:
        with self._lock:
            self.put_admission_stopped = True
            self.writes_halted = True

    def admit_proof_get(self, reservation: Reservation) -> Reservation | str:
        with self._lock:
            now = self._now()
            if reservation.state not in {"put_done", "proving"} or reservation.remaining_proofs < 1:
                return "not_attempted"
            if self.abandoned or self.deadline.expired(now):
                self.put_admission_stopped = True
                return "readback_budget_exhausted"
            require(self.reserved_proof >= 1, "proof_reservation_missing")
            self.reserved_proof -= 1
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

    def abandon_remaining_proofs(self, reservation: Reservation, *, release_slots: bool) -> None:
        with self._lock:
            leftover = reservation.remaining_proofs
            if leftover:
                self.queued_gets -= leftover
                self.outstanding_proof -= leftover
                self.reserved_proof -= leftover
                if release_slots:
                    self.used -= leftover
                reservation.remaining_proofs = 0
            if reservation.state != "proving":
                reservation.state = "cleaned"

    def admit_get(self, resource: str) -> Reservation | str:
        """Independent GET. Cannot spend slots already committed as proofs."""
        with self._lock:
            now = self._now()
            if self.abandoned or self.deadline.expired(now) or self.used + 1 > self.max_requests:
                return "not_attempted"
            self.used += 1
            self.inflight_gets += 1
            reservation = Reservation("get", 1, 0, resource, state="inflight", committed_slots=1)
            self.reservations.append(reservation)
            return reservation

    def finish_get(self, reservation: Reservation) -> None:
        with self._lock:
            require(reservation.state == "inflight" and reservation.kind == "get", "reservation_invalid")
            reservation.state = "cleaned"
            self.inflight_gets -= 1

    def acquire_exchange(self) -> str | None:
        now = self._now()
        timeout = self._usable_remaining(now)
        if timeout <= 0 or self.abandoned:
            return "not_attempted"
        if not self._exchange.acquire(timeout=timeout):
            return "readback_budget_exhausted"
        now = self._now()
        if self.abandoned or self._usable_remaining(now) <= 0:
            self._exchange.release()
            return "not_attempted"
        return None

    def release_exchange(self) -> None:
        self._exchange.release()

    def reap(self) -> None:
        with self._lock:
            self.abandoned = True
            self.put_admission_stopped = True


class InjectedExchange:
    def __init__(self, handler):
        self.handler = handler
        self.calls = []

    def __call__(self, method, url, *, headers, data, timeout, limit) -> ExchangeResult:
        self.calls.append({"method": method, "url": url, "header_names": tuple(sorted(headers)),
                           "data_sha256": sha(data) if data else None, "timeout": timeout, "limit": limit})
        result = self.handler(method, url, headers=headers, data=data, timeout=timeout, limit=limit)
        require(isinstance(result, ExchangeResult), "invalid_transport")
        require(result.disposition in {DEFINITELY_NOT_SENT, MAY_HAVE_SENT, RESPONSE_COMPLETE},
                "invalid_transport")
        return result


def allowed_url(method: str, url: str, credential_role: str | None) -> bool:
    if not isinstance(url, str) or ".." in url or len(url) > 2048 or any(ord(c) <= 32 for c in url):
        return False
    if method == "PUT":
        return url in {PACKAGE_PUBLISH, SYMBOL_PUBLISH}
    if url == FEED_INDEX or PACKAGE_GET.fullmatch(url):
        return True
    if url.startswith(SYMBOL_PUBLISH + "/") and KEY.fullmatch(url[len(SYMBOL_PUBLISH) + 1:]):
        return True
    if credential_role == "metadata" and url.startswith(METADATA_PREFIX):
        return bool(METADATA_PATH.fullmatch(url[len(METADATA_PREFIX):]))
    return False


class SharedTransport:
    """Parent deadline, URL allowlist, no redirects, response caps. No authority."""

    authority = False

    def __init__(self, coordinator: Coordinator, exchange: InjectedExchange):
        require(isinstance(coordinator, Coordinator), "coordinator_invalid")
        require(isinstance(exchange, InjectedExchange), "offline_exchange_required")
        self.coordinator = coordinator
        self.exchange = exchange

    def request(self, method: str, url: str, *, headers=None, data=b"",
                credential_role=None, limit=MAX_BYTES) -> ExchangeResult:
        headers = dict(headers or {})
        require(method in {"GET", "PUT"}, "unsupported_method")
        require(set(headers) <= {"Authorization", "X-NuGet-ApiKey", "Content-Type"}, "unsafe_headers")
        require(allowed_url(method, url, credential_role), "unsafe_request")
        require(len(data) <= MAX_BYTES, "unsafe_request")
        if credential_role == "metadata":
            require(method == "GET" and "Authorization" in headers and "X-NuGet-ApiKey" not in headers,
                    "credential_separation")
        elif credential_role == "publish":
            require(method == "PUT" and url in {PACKAGE_PUBLISH, SYMBOL_PUBLISH}
                    and "X-NuGet-ApiKey" in headers and "Authorization" not in headers,
                    "credential_separation")
        else:
            require("Authorization" not in headers and "X-NuGet-ApiKey" not in headers
                    and method == "GET", "credential_separation")
        require(limit > 0 and limit <= MAX_BYTES, "response_limit")
        refusal = self.coordinator.acquire_exchange()
        if refusal:
            return ExchangeResult(None, b"", False, refusal, DEFINITELY_NOT_SENT)
        try:
            remaining = self.coordinator.deadline.remaining(self.coordinator.clock())
            if remaining <= 0:
                return ExchangeResult(None, b"", False, "deadline_exceeded", DEFINITELY_NOT_SENT)
            timeout = min(HTTP_CAP_SECONDS, remaining)
            result = self.exchange(method, url, headers=headers, data=data, timeout=timeout, limit=limit)
            if result.redirected or result.failure_category == "redirect_rejected":
                return ExchangeResult(result.status, b"", False, "redirect_rejected",
                                      MAY_HAVE_SENT if method == "PUT" else RESPONSE_COMPLETE, True)
            if len(result.body) > limit:
                return ExchangeResult(result.status, b"", False, "oversized_response",
                                      result.disposition if result.disposition != DEFINITELY_NOT_SENT
                                      else MAY_HAVE_SENT)
            return result
        except PrototypeError:
            raise
        except Exception:
            return ExchangeResult(None, b"", False, "transport_failed",
                                  MAY_HAVE_SENT if method == "PUT" else DEFINITELY_NOT_SENT)
        finally:
            self.coordinator.release_exchange()


@dataclass
class SymbolKey:
    key: str
    pdb_sha256: str
    snupkg_id: str
    pdb_bytes: bytes
    input_trust: str = UNVERIFIED
    inspector_digest: str | None = None
    declared_checksum: str | None = None
    manifest_pdb_sha256: str | None = None
    dll_sha256: str | None = None


def validate_symbol_key(item: SymbolKey) -> None:
    require(isinstance(item.key, str) and KEY.fullmatch(item.key), "symbol_key_invalid")
    require(isinstance(item.pdb_bytes, bytes) and sha(item.pdb_bytes) == item.pdb_sha256
            and SHA256.fullmatch(item.pdb_sha256), "symbol_key_unverified_hash")
    if item.input_trust == TRUSTED:
        require(isinstance(item.inspector_digest, str) and SHA256.fullmatch(item.inspector_digest),
                "symbol_inspector_unbound")
        require(item.manifest_pdb_sha256 == item.pdb_sha256, "symbol_manifest_mismatch")
        require(isinstance(item.declared_checksum, str) and item.declared_checksum != "",
                "symbol_checksum_unbound")
        require(isinstance(item.dll_sha256, str) and SHA256.fullmatch(item.dll_sha256),
                "symbol_dll_unbound")
        return
    require(item.input_trust == UNVERIFIED, "symbol_trust_invalid")


class SymbolIndex:
    def __init__(self):
        self.by_key: dict[str, SymbolKey] = {}
        self.by_snupkg: dict[str, set[str]] = {}
        self.observed: dict[str, str] = {}

    def add(self, item: SymbolKey) -> None:
        validate_symbol_key(item)
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
    kind: str
    archive_sha256: str
    archive: bytes
    state: str = "not_attempted"
    put_status: int | None = None
    put_outcome: str | None = None
    upload_outcome: str | None = None
    get_status: int | None = None
    get_classification: str | None = None
    content_presence: str | None = None
    failure_category: str | None = None


def classify_put(result: ExchangeResult) -> tuple[str, str | None]:
    if result.disposition == DEFINITELY_NOT_SENT:
        return "not_attempted", result.failure_category or DEFINITELY_NOT_SENT
    if result.disposition == MAY_HAVE_SENT:
        return "acceptance_unknown", result.failure_category or MAY_HAVE_SENT
    if not result.complete or result.status is None:
        return "acceptance_unknown", result.failure_category or "incomplete_response"
    if result.status in ACCEPTED_PENDING_PROOF:
        return "accepted_pending_proof", None
    if result.status in NOT_DUPLICATE_SUCCESS:
        return "not_duplicate_success", "ambiguous_put"
    return "acceptance_unknown", result.failure_category or "ambiguous_put"


def classify_archive_get(result: ExchangeResult, original: bytes) -> tuple[str, str | None]:
    if result.disposition == DEFINITELY_NOT_SENT:
        return "unverifiable", result.failure_category or DEFINITELY_NOT_SENT
    if not result.complete or result.failure_category or result.status is None:
        return "unverifiable", result.failure_category or "incomplete_response"
    if result.status == 404:
        return "missing", None
    if result.status != 200:
        return "unverifiable", "unexpected_status"
    if sha(result.body) != sha(original):
        return "conflicting", "archive_sha_mismatch"
    return "matching", None


def classify_association(item: SymbolKey, result: ExchangeResult) -> tuple[str, str | None]:
    classification, failure = classify_archive_get(result, item.pdb_bytes)
    if classification != "matching":
        return classification, failure
    if item.input_trust != TRUSTED:
        return "bytes_observed_unverified_inputs", "unverified_fixture"
    return "inspector_bound_association_observed", None


class Prototype3101:
    """Offline 3.10.1 executor. remote_snupkg_archive_verified stays false here."""

    authority = False

    def __init__(self, coordinator: Coordinator, transport: SharedTransport, symbols: SymbolIndex,
                 receipt: BoundedReceipt | None = None):
        self.coordinator = coordinator
        self.transport = transport
        self.symbols = symbols
        self.receipt = receipt or BoundedReceipt()
        self.remote_snupkg_archive_verified = False
        self._locks: dict[str, threading.Lock] = {}
        self._guard = threading.Lock()

    def _lock_for(self, name: str) -> threading.Lock:
        with self._guard:
            return self._locks.setdefault(name, threading.Lock())

    def _acquire_named(self, names: list[str]) -> bool:
        ordered = sorted(set(names))
        held = []
        for name in ordered:
            lock = self._lock_for(name)
            remaining = self.coordinator._usable_remaining(self.coordinator.clock())
            if remaining <= 0 or not lock.acquire(timeout=max(0.0, remaining)):
                for held_name in reversed(held):
                    self._lock_for(held_name).release()
                return False
            held.append(name)
        return True

    def _release_named(self, names: list[str]) -> None:
        for name in reversed(sorted(set(names))):
            self._lock_for(name).release()

    def _record(self, operation: Operation, **extra) -> None:
        self.receipt.append({
            "id": operation.id, "kind": operation.kind, "state": operation.state,
            "upload_outcome": operation.upload_outcome, "content_presence": operation.content_presence,
            "failure_category": operation.failure_category, **extra,
        })

    def preflight_keys(self, items: list[SymbolKey]) -> None:
        for item in items:
            self.symbols.add(item)

    def metadata_get(self, url: str, token: str) -> ExchangeResult:
        reservation = self.coordinator.admit_get(url)
        if isinstance(reservation, str):
            raise PrototypeError(reservation)
        try:
            return self.transport.request(
                "GET", url, headers={"Authorization": "Bearer " + token}, credential_role="metadata")
        finally:
            self.coordinator.finish_get(reservation)

    def pre_get_nupkg(self, package_id: str, original: bytes, *, version: str = "3.10.1") -> dict:
        url = nupkg_get_url(package_id, version)
        reservation = self.coordinator.admit_get(url)
        if isinstance(reservation, str):
            return {"state": reservation, "classification": "unverifiable"}
        try:
            result = self.transport.request("GET", url, limit=MAX_BYTES)
            classification, failure = classify_archive_get(result, original)
            return {"state": "observed", "classification": classification, "failure_category": failure,
                    "status": result.status}
        finally:
            self.coordinator.finish_get(reservation)

    def prove_pdb_association(self, item: SymbolKey) -> dict:
        validate_symbol_key(item)
        url = symbol_get_url(item.key)
        reservation = self.coordinator.admit_get(url)
        if isinstance(reservation, str):
            return {"state": reservation, "classification": "unverifiable",
                    "remote_snupkg_archive_verified": False}
        try:
            result = self.transport.request("GET", url, limit=MAX_BYTES)
            classification, failure = classify_association(item, result)
            self.remote_snupkg_archive_verified = False
            return {"state": "observed", "classification": classification, "failure_category": failure,
                    "status": result.status, "remote_snupkg_archive_verified": False,
                    "input_trust": item.input_trust}
        finally:
            self.coordinator.finish_get(reservation)

    def put_and_prove_nupkg(self, operation: Operation, *, publish_key: str,
                            version: str = "3.10.1") -> Operation:
        require(operation.kind == "nupkg", "kind_invalid")
        require(sha(operation.archive) == operation.archive_sha256, "original_archive_changed")
        if self.coordinator.writes_halted or self.coordinator.put_admission_stopped:
            operation.state = "not_attempted"
            operation.failure_category = "not_attempted"
            self._record(operation, event="nupkg_writes_halted")
            return operation
        names = [f"nupkg:{operation.id}"]
        reservation = None
        if not self._acquire_named(names):
            operation.state = "not_attempted"
            operation.failure_category = "not_attempted"
            self._record(operation, event="nupkg_lock_missed")
            return operation
        try:
            reservation = self.coordinator.admit_put(operation.id, proof_slots=1)
            if isinstance(reservation, str):
                operation.state = reservation
                operation.failure_category = reservation
                self._record(operation, event="nupkg_put_refused")
                return operation
            self.coordinator.start_put(reservation)
            try:
                result = self.transport.request(
                    "PUT", PACKAGE_PUBLISH,
                    headers={"X-NuGet-ApiKey": publish_key, "Content-Type": "application/octet-stream"},
                    data=operation.archive, credential_role="publish", limit=4096)
            except Exception:
                result = ExchangeResult(None, b"", False, "transport_failed", MAY_HAVE_SENT)
            self.coordinator.finish_put(reservation, disposition=result.disposition)
            operation.put_status = result.status
            operation.put_outcome, failure = classify_put(result)
            operation.upload_outcome = operation.put_outcome
            operation.failure_category = failure
            if result.disposition != DEFINITELY_NOT_SENT and operation.put_outcome != "accepted_pending_proof":
                self.coordinator.halt_puts()
            if result.disposition == DEFINITELY_NOT_SENT:
                operation.state = "not_attempted"
                self._record(operation, event="nupkg_not_sent")
                return operation
            proof = self.coordinator.admit_proof_get(reservation)
            if isinstance(proof, str):
                operation.state = proof
                operation.failure_category = proof
                self._record(operation, event="nupkg_proof_refused")
                return operation
            get = self.transport.request("GET", nupkg_get_url(operation.id, version), limit=MAX_BYTES)
            operation.get_status = get.status
            operation.get_classification, get_failure = classify_archive_get(get, operation.archive)
            operation.content_presence = operation.get_classification
            if get_failure and operation.failure_category is None:
                operation.failure_category = get_failure
            if operation.get_classification == "conflicting":
                self.coordinator.halt_puts()
            operation.state = operation.put_outcome
            self.coordinator.finish_proof_get(reservation)
            reservation = None
            self._record(operation, event="nupkg_put_get")
            return operation
        except Exception as error:
            if reservation is not None and reservation.state == "inflight":
                self.coordinator.finish_put(reservation, disposition=MAY_HAVE_SENT)
            operation.state = operation.state or "acceptance_unknown"
            operation.failure_category = operation.failure_category or "transport_failed"
            self._record(operation, event="nupkg_exception", error_type=type(error).__name__)
            raise
        finally:
            self._release_named(names)

    def put_snupkg_serialized(self, operation: Operation, *, publish_key: str) -> Operation:
        require(operation.kind == "snupkg", "kind_invalid")
        require(sha(operation.archive) == operation.archive_sha256, "original_archive_changed")
        overlap = sorted(self.symbols.overlapping_snupkgs(operation.id))
        keys = sorted(self.symbols.by_snupkg.get(operation.id, ()))
        names = [f"snupkg:{name}" for name in (*overlap, operation.id)] + [f"key:{k}" for k in keys]
        reservation = None
        if self.coordinator.writes_halted or self.coordinator.put_admission_stopped:
            operation.state = "not_attempted"
            operation.failure_category = "not_attempted"
            self._record(operation, event="snupkg_writes_halted")
            return operation
        if not self._acquire_named(names):
            operation.state = "not_attempted"
            operation.failure_category = "not_attempted"
            self._record(operation, event="snupkg_lock_missed")
            return operation
        try:
            self.remote_snupkg_archive_verified = False
            for key in keys:
                item = self.symbols.by_key[key]
                observed = self.prove_pdb_association(item)
                self.symbols.observed[key] = observed["classification"]
                if observed["classification"] == "conflicting":
                    self.coordinator.halt_puts()
                    operation.state = "symbol_overlap_conflict"
                    operation.failure_category = "symbol_overlap_conflict"
                    operation.content_presence = "conflicting"
                    self._record(operation, event="snupkg_conflict_before_put")
                    return operation
            if keys and all(self.symbols.observed.get(key) in {
                    "inspector_bound_association_observed", "bytes_observed_unverified_inputs"} for key in keys):
                if all(self.symbols.by_key[key].input_trust == TRUSTED
                       and self.symbols.observed.get(key) == "inspector_bound_association_observed"
                       for key in keys):
                    operation.state = "pdbs_already_matching_archive_unverified"
                else:
                    operation.state = "pdbs_observed_unverified_skip_put"
                operation.put_outcome = "not_attempted"
                operation.upload_outcome = "not_attempted"
                self.remote_snupkg_archive_verified = False
                self._record(operation, event="snupkg_skipped_after_reeval")
                return operation
            proof_slots = max(1, len(keys))
            reservation = self.coordinator.admit_put(operation.id, proof_slots=proof_slots)
            if isinstance(reservation, str):
                operation.state = reservation
                operation.failure_category = reservation
                self._record(operation, event="snupkg_put_refused")
                return operation
            self.coordinator.start_put(reservation)
            try:
                result = self.transport.request(
                    "PUT", SYMBOL_PUBLISH,
                    headers={"X-NuGet-ApiKey": publish_key, "Content-Type": "application/octet-stream"},
                    data=operation.archive, credential_role="publish", limit=4096)
            except Exception:
                result = ExchangeResult(None, b"", False, "transport_failed", MAY_HAVE_SENT)
            self.coordinator.finish_put(reservation, disposition=result.disposition)
            operation.put_status = result.status
            operation.put_outcome, failure = classify_put(result)
            operation.upload_outcome = operation.put_outcome
            operation.failure_category = failure
            if result.disposition != DEFINITELY_NOT_SENT and operation.put_outcome != "accepted_pending_proof":
                self.coordinator.halt_puts()
            if result.disposition == DEFINITELY_NOT_SENT:
                operation.state = "not_attempted"
                self._record(operation, event="snupkg_not_sent")
                return operation
            operation.get_classification = "association_pending_unverified_archive"
            targets = keys or [None]
            for key in targets:
                proof = self.coordinator.admit_proof_get(reservation)
                if isinstance(proof, str):
                    operation.state = proof
                    operation.failure_category = proof
                    self.coordinator.abandon_remaining_proofs(reservation, release_slots=False)
                    self._record(operation, event="snupkg_proof_refused")
                    return operation
                if key is None:
                    operation.content_presence = "snupkg_archive_unverified"
                    self.coordinator.finish_proof_get(reservation)
                    break
                item = self.symbols.by_key[key]
                get = self.transport.request("GET", symbol_get_url(key), limit=MAX_BYTES)
                operation.get_status = get.status
                classification, get_failure = classify_association(item, get)
                self.symbols.observed[key] = classification
                self.coordinator.finish_proof_get(reservation)
                operation.get_classification = classification
                operation.content_presence = classification
                if classification == "conflicting":
                    self.coordinator.halt_puts()
                    operation.failure_category = get_failure or "symbol_overlap_conflict"
                    self.coordinator.abandon_remaining_proofs(reservation, release_slots=False)
                    break
                if get_failure and operation.failure_category is None:
                    operation.failure_category = get_failure
            operation.state = operation.put_outcome
            self.remote_snupkg_archive_verified = False
            reservation = None
            self._record(operation, event="snupkg_put_get")
            return operation
        except Exception as error:
            if reservation is not None and reservation.state == "inflight":
                self.coordinator.finish_put(reservation, disposition=MAY_HAVE_SENT)
            operation.state = operation.state or "acceptance_unknown"
            operation.failure_category = operation.failure_category or "transport_failed"
            self._record(operation, event="snupkg_exception", error_type=type(error).__name__)
            raise
        finally:
            self._release_named(names)

    def finalize(self) -> dict:
        now = self.coordinator.clock()
        exceeded = self.coordinator.deadline.expired(now)
        self.receipt.append({
            "event": "final", "deadline_exceeded": exceeded,
            "remaining": self.coordinator.deadline.remaining(now),
            "used": self.coordinator.used, "outstanding_proof": self.coordinator.outstanding_proof,
        })
        self.coordinator.reap()
        require(not exceeded, "deadline_exceeded")
        return {"receipt_rows": len(self.receipt.rows), "used": self.coordinator.used}


def independent_put_get_pair_counterexample_seconds(pairs: int) -> int:
    """Counterexample only: independent PUT then GET pairs at cap/concurrency 4.

    Not a cold-publish upper bound. Overlap locks, sequential multi-PDB proofs,
    observation GETs, metadata, queue wait, cleanup, and receipt are omitted.
    """
    waves = math.ceil(pairs / REVIEWED_CONCURRENCY) if pairs else 0
    return waves * (2 * HTTP_CAP_SECONDS)


def operation_dag(*, nupkgs: int = 231, snupkgs: int = 231, associations: int = 688,
                  unique_pdb_sha256: int = 688, unique_pdb_names: int = 231,
                  extras_in_465: int = 3) -> dict:
    """One reconciled DAG. Architect U is archive PUTs (462), not PDB associations."""
    archive_puts = nupkgs + snupkgs
    two_nupkg_passes_plus_three = 2 * nupkgs + extras_in_465
    architect_total = two_nupkg_passes_plus_three + 2 * associations + archive_puts
    return {
        "nupkg_puts": nupkgs,
        "snupkg_puts": snupkgs,
        "archive_puts_U": archive_puts,
        "associations_S": associations,
        "unique_symbol_keys": {
            "status": "not_extracted_without_inspector",
            "unique_pdb_sha256": unique_pdb_sha256,
            "unique_pdb_names": unique_pdb_names,
        },
        "metadata_index_extras_in_465": extras_in_465,
        "github_metadata_pages": "unmeasured_offline",
        "nupkg_get_passes": 2,
        "overlap_rereads": "conditional; unique pdb sha overlap 0; keys unmeasured without inspector",
        "conditional_proof_operations": "GET after PUT unless definitely_not_sent",
        "architect_formula": "465+2S+U",
        "architect_465": two_nupkg_passes_plus_three,
        "architect_2S": 2 * associations,
        "architect_U": archive_puts,
        "architect_total": architect_total,
        "withdrawn_2529": "invalid; treated unique PDB bytes as U",
        "independent_pair_counterexample": {
            "pairs": archive_puts,
            "seconds": independent_put_get_pair_counterexample_seconds(archive_puts),
            "full_cold_upper_bound": False,
            "omits": ["observation GETs", "overlap serialization", "sequential multi-PDB proofs",
                      "metadata paging", "queue wait", "cleanup margin", "receipt"],
        },
        "max_seconds": MAX_SECONDS,
        "max_requests": MAX_REQUESTS,
        "http_cap_seconds": HTTP_CAP_SECONDS,
        "readback_reserve_seconds": READBACK_RESERVE_SECONDS,
        "cleanup_margin_seconds": CLEANUP_MARGIN_SECONDS,
        "reviewed_concurrency": REVIEWED_CONCURRENCY,
        "concurrency_4_worst_case_guarantee": False,
        "ceil_work_over_4_invalid_with_locks": True,
        "fits_worst_case_1800": False,
        "safe_1800_demonstrated": False,
        "fit_claim": False,
    }


def offline_measurement_report() -> dict:
    return operation_dag()
