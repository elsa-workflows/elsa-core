"""Distinct 3.10.1 Feedz executor prototype. Does not replace the 3.10.0 executor.

Owning issue: https://github.com/elsa-workflows/elsa-core/issues/8220
(F3.4 artifact provenance / publication / recovery; no dedicated 3.10.1
executor issue exists). Delete this prototype, its tests, measurement
record, and the 3.10.0 GET-sample template when a reviewed 3.10.1 executor
supersedes it or the 3.10.1 Feedz path is abandoned.

Single immutable deadline from inspector start (MAX_SECONDS = 1800) covers
admission, metadata, queue, PUT, GET, cleanup, and receipt. There is no
GitHubApi 120s clock, no 100-request sidecar, and no mutating a shared
transport deadline. Injected exchanges only: no network PUT and no publish
authority. Fit against 1800s is not claimed.
"""
from __future__ import annotations

from dataclasses import dataclass, field
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
    """Parent bound. `start` and the coordinator clock must both be `time.monotonic`."""

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
    """Synchronized in-memory receipt. Durable IO / process cancel-reap are not implemented."""

    durable_io_implemented = False
    process_cancel_reap_implemented = False

    def __init__(self, *, max_bytes: int = MAX_RECEIPT_BYTES):
        self.max_bytes = max_bytes
        self.size = 0
        self.rows: list[dict] = []
        self.overflowed = False
        self.dropped = 0
        self._lock = threading.Lock()

    def append(self, event: dict, *, drop_if_full: bool = False) -> None:
        payload = json.dumps(event, sort_keys=True, default=str) + "\n"
        encoded = payload.encode()
        with self._lock:
            if self.size + len(encoded) > self.max_bytes:
                self.overflowed = True
                self.dropped += 1
                if drop_if_full:
                    return
                raise PrototypeError("receipt_limit")
            self.rows.append(event)
            self.size += len(encoded)


class Coordinator:
    """Atomic owner of committed request slots, including reserved proofs."""

    def __init__(self, deadline: ImmutableDeadline, *, max_requests: int = MAX_REQUESTS,
                 concurrency: int = REVIEWED_CONCURRENCY, http_cap: int = HTTP_CAP_SECONDS,
                 reserve: int = READBACK_RESERVE_SECONDS, cleanup: int = CLEANUP_MARGIN_SECONDS,
                 clock=time.monotonic):
        require(isinstance(deadline, ImmutableDeadline), "deadline_invalid")
        require(deadline.seconds == MAX_SECONDS, "deadline_invalid")
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
        self.committed_put_time = 0
        self.committed_proof_time = 0
        self.committed_get_time = 0
        self.reservations: list[Reservation] = []

    def _now(self) -> float:
        return self.clock()

    def _usable_remaining(self, now: float) -> float:
        return self.deadline.remaining(now) - self.cleanup

    def _owed(self, extra_put: int = 0, extra_proof: int = 0, extra_get: int = 0) -> float:
        return (self.committed_put_time + extra_put + self.committed_proof_time + extra_proof
                + self.committed_get_time + extra_get + self.cleanup)

    def exchange_timeout(self) -> float | None:
        """One-exchange timeout: min(http_cap, remaining-cleanup). None if the usable slice is not positive."""
        usable = self._usable_remaining(self._now())
        if usable <= 0:
            return None
        return min(float(self.http_cap), usable)

    def send_boundary(self, *, is_put: bool) -> str | None:
        """Revalidate immediately before transmission. Halted unsent PUTs do not send."""
        with self._lock:
            now = self._now()
            remaining = self.deadline.remaining(now)
            usable = remaining - self.cleanup
            if self.abandoned or remaining <= 0:
                return "not_attempted"
            if is_put and (self.writes_halted or self.put_admission_stopped):
                return "not_attempted"
            if is_put:
                if remaining <= self._owed() or remaining <= self.http_cap + self.committed_proof_time + self.cleanup:
                    return "readback_budget_exhausted"
                if usable <= 0:
                    return "readback_budget_exhausted"
                return None
            if usable <= 0:
                return "readback_budget_exhausted"
            return None

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
            this_put, this_proof = self.http_cap, proof_slots * self.http_cap
            if remaining <= self._owed(this_put, this_proof, 0):
                self.put_admission_stopped = True
                return "readback_budget_exhausted"
            self.used += need
            self.reserved_proof += proof_slots
            self.outstanding_proof += proof_slots
            self.committed_put_time += this_put
            self.committed_proof_time += this_proof
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
            self.committed_put_time -= self.http_cap
            if disposition == DEFINITELY_NOT_SENT:
                self.used -= reservation.remaining_proofs
                self.reserved_proof -= reservation.remaining_proofs
                self.outstanding_proof -= reservation.remaining_proofs
                self.committed_proof_time -= reservation.remaining_proofs * self.http_cap
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
            self.committed_proof_time -= self.http_cap
            reservation.state = "cleaned" if reservation.remaining_proofs == 0 else "put_done"

    def abandon_remaining_proofs(self, reservation: Reservation, *, release_slots: bool) -> None:
        with self._lock:
            leftover = reservation.remaining_proofs
            if leftover:
                self.queued_gets -= leftover
                self.outstanding_proof -= leftover
                self.reserved_proof -= leftover
                self.committed_proof_time -= leftover * self.http_cap
                if release_slots:
                    self.used -= leftover
                reservation.remaining_proofs = 0
            if reservation.state != "proving":
                reservation.state = "cleaned"

    def admit_get(self, resource: str) -> Reservation | str:
        """Independent GET. Cannot spend slots or time already committed as proofs."""
        with self._lock:
            now = self._now()
            if self.abandoned or self.deadline.expired(now) or self.used + 1 > self.max_requests:
                return "not_attempted"
            if self.deadline.remaining(now) <= self._owed(0, 0, self.http_cap):
                return "not_attempted"
            self.used += 1
            self.inflight_gets += 1
            self.committed_get_time += self.http_cap
            reservation = Reservation("get", 1, 0, resource, state="inflight", committed_slots=1)
            self.reservations.append(reservation)
            return reservation

    def finish_get(self, reservation: Reservation) -> None:
        with self._lock:
            require(reservation.state == "inflight" and reservation.kind == "get", "reservation_invalid")
            reservation.state = "cleaned"
            self.inflight_gets -= 1
            self.committed_get_time -= self.http_cap

    def acquire_exchange(self, *, is_put: bool) -> str | None:
        now = self._now()
        timeout = self._usable_remaining(now)
        if timeout <= 0 or self.abandoned:
            return "not_attempted"
        if not self._exchange.acquire(timeout=timeout):
            return "readback_budget_exhausted"
        refusal = self.send_boundary(is_put=is_put)
        if refusal:
            self._exchange.release()
            return refusal
        return None

    def release_exchange(self) -> None:
        self._exchange.release()

    def reap(self) -> dict:
        """Flag abandoned and drain unsent PUTs. Process-level cancel of inflight HTTP is not implemented."""
        drained = 0
        unresolved = 0
        with self._lock:
            self.abandoned = True
            self.put_admission_stopped = True
            self.writes_halted = True
            for reservation in self.reservations:
                if reservation.kind == "put" and reservation.state == "reserved":
                    self.queued_puts -= 1
                    self.committed_put_time -= self.http_cap
                    leftover = reservation.remaining_proofs
                    self.used -= leftover
                    self.reserved_proof -= leftover
                    self.outstanding_proof -= leftover
                    self.committed_proof_time -= leftover * self.http_cap
                    reservation.remaining_proofs = 0
                    reservation.state = "not_attempted"
                    drained += 1
                elif reservation.kind == "put" and reservation.state in {"put_done", "proving", "inflight"}:
                    unresolved += reservation.remaining_proofs + (1 if reservation.state == "proving" else 0)
            return {
                "drained_unsent_puts": drained,
                "unresolved_proofs": unresolved,
                "outstanding_proof": self.outstanding_proof,
                "durable_io_implemented": False,
                "process_cancel_reap_implemented": False,
            }


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
    if credential_role == "metadata":
        return (method == "GET" and url.startswith(METADATA_PREFIX)
                and bool(METADATA_PATH.fullmatch(url[len(METADATA_PREFIX):])))
    if credential_role == "publish":
        return method == "PUT" and url in {PACKAGE_PUBLISH, SYMBOL_PUBLISH}
    if credential_role is not None:
        return False
    if method != "GET":
        return False
    if url == FEED_INDEX or PACKAGE_GET.fullmatch(url):
        return True
    return url.startswith(SYMBOL_PUBLISH + "/") and bool(KEY.fullmatch(url[len(SYMBOL_PUBLISH) + 1:]))


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
        refusal = self.coordinator.acquire_exchange(is_put=method == "PUT")
        if refusal:
            return ExchangeResult(None, b"", False, refusal, DEFINITELY_NOT_SENT)
        try:
            timeout = self.coordinator.exchange_timeout()
            if timeout is None:
                return ExchangeResult(None, b"", False, "readback_budget_exhausted", DEFINITELY_NOT_SENT)
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
    key_presence: dict = field(default_factory=dict)
    failure_category: str | None = None


def classify_put(result: ExchangeResult) -> tuple[str, str | None]:
    if result.disposition == DEFINITELY_NOT_SENT:
        return "not_attempted", result.failure_category or DEFINITELY_NOT_SENT
    if result.failure_category:
        return "acceptance_unknown", result.failure_category
    if result.disposition == MAY_HAVE_SENT:
        return "acceptance_unknown", MAY_HAVE_SENT
    if not result.complete or result.status is None:
        return "acceptance_unknown", "incomplete_response"
    if result.status in ACCEPTED_PENDING_PROOF:
        return "accepted_pending_proof", None
    if result.status in NOT_DUPLICATE_SUCCESS:
        return "not_duplicate_success", "ambiguous_put"
    return "acceptance_unknown", "ambiguous_put"


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


MATCHING_PRESENCE = frozenset({
    "matching", "inspector_bound_association_observed", "bytes_observed_unverified_inputs",
})
PRE_PUT_SKIP = frozenset({
    "inspector_bound_association_observed", "bytes_observed_unverified_inputs",
})


def aggregate_presence(keys: list[str], observed: dict[str, str]) -> str:
    if not keys:
        return "snupkg_archive_unverified"
    values = [observed.get(key) for key in keys]
    if any(value is None for value in values):
        return "incomplete"
    if any(value == "conflicting" for value in values):
        return "conflicting"
    if all(value in MATCHING_PRESENCE for value in values):
        return "all_required_observed"
    return "not_all_required"


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
        drop = extra.pop("drop_if_full", False)
        try:
            self.receipt.append({
                "id": operation.id, "kind": operation.kind, "state": operation.state,
                "upload_outcome": operation.upload_outcome, "content_presence": operation.content_presence,
                "key_presence": dict(operation.key_presence),
                "failure_category": operation.failure_category, **extra,
            }, drop_if_full=drop)
        except PrototypeError as error:
            self.coordinator.halt_puts()
            if str(error) == "receipt_limit":
                operation.failure_category = "receipt_limit"
            raise
        if self.receipt.overflowed:
            self.coordinator.halt_puts()
            if drop and operation.failure_category is None:
                operation.failure_category = "receipt_limit"

    def _refuse_unreceiptable(self, operation: Operation) -> bool:
        if not (self.receipt.overflowed or self.receipt.size >= self.receipt.max_bytes):
            return False
        self.coordinator.halt_puts()
        operation.state = "not_attempted"
        operation.failure_category = "receipt_limit"
        return True

    def _put_exchange(self, url: str, archive: bytes, publish_key: str) -> ExchangeResult:
        sent_before = len(self.transport.exchange.calls)
        try:
            return self.transport.request(
                "PUT", url,
                headers={"X-NuGet-ApiKey": publish_key, "Content-Type": "application/octet-stream"},
                data=archive, credential_role="publish", limit=4096)
        except PrototypeError as error:
            sent = len(self.transport.exchange.calls) > sent_before
            return ExchangeResult(None, b"", False, str(error),
                                  MAY_HAVE_SENT if sent else DEFINITELY_NOT_SENT)
        except Exception:
            return ExchangeResult(None, b"", False, "transport_failed", MAY_HAVE_SENT)

    def _on_upload_exception(self, operation: Operation, reservation: Reservation | None,
                             error: BaseException, *, event: str) -> bool:
        """Return True when the PrototypeError is handled and the caller should return."""
        # A PUT can only have started once start_put ran (reservation inflight or later) or an
        # outcome was recorded. Anything raised before that is a pre-PUT failure.
        put_started = operation.upload_outcome is not None or (
            isinstance(reservation, Reservation) and reservation.state in {"inflight", "put_done"})
        self._complete_reservation(reservation)
        self._terminal_state(operation)
        if isinstance(error, PrototypeError) and str(error) == "receipt_limit":
            self.coordinator.halt_puts()
            operation.failure_category = "receipt_limit"
        elif not put_started:
            # Fail closed: no PUT was sent, so this is not acceptance_unknown, and no later PUT may run.
            self.coordinator.halt_puts()
            operation.state = "pre_observation_unverified" if operation.kind == "snupkg" else "not_attempted"
            if operation.failure_category is None:
                operation.failure_category = str(error) if isinstance(error, PrototypeError) else "pre_put_exception"
        else:
            if operation.state == "not_attempted" and operation.upload_outcome is None:
                operation.state = "acceptance_unknown"
            if operation.failure_category is None:
                operation.failure_category = "transport_failed"
        self._record(operation, event=event, error_type=type(error).__name__, drop_if_full=True)
        return isinstance(error, PrototypeError)

    def _complete_reservation(self, reservation: Reservation | None) -> None:
        if not isinstance(reservation, Reservation):
            return
        if reservation.state == "inflight":
            self.coordinator.finish_put(reservation, disposition=MAY_HAVE_SENT)
        elif reservation.state == "proving":
            self.coordinator.finish_proof_get(reservation)
            self.coordinator.abandon_remaining_proofs(reservation, release_slots=False)
        elif reservation.state == "put_done" and reservation.remaining_proofs:
            self.coordinator.abandon_remaining_proofs(reservation, release_slots=False)

    def _terminal_state(self, operation: Operation) -> None:
        if operation.upload_outcome and operation.state == "not_attempted":
            operation.state = operation.upload_outcome

    def preflight_keys(self, items: list[SymbolKey]) -> None:
        for item in items:
            self.symbols.add(item)

    def metadata_get(self, url: str, token: str) -> ExchangeResult:
        reservation = self.coordinator.admit_get(url)
        if isinstance(reservation, str):
            self.coordinator.halt_puts()
            raise PrototypeError(reservation)
        try:
            return self.transport.request(
                "GET", url, headers={"Authorization": "Bearer " + token}, credential_role="metadata")
        except Exception:
            self.coordinator.halt_puts()
            raise
        finally:
            self.coordinator.finish_get(reservation)

    def pre_get_nupkg(self, package_id: str, original: bytes, *, version: str = "3.10.1") -> dict:
        url = nupkg_get_url(package_id, version)
        reservation = self.coordinator.admit_get(url)
        if isinstance(reservation, str):
            self.coordinator.halt_puts()
            return {"state": reservation, "classification": "unverifiable"}
        try:
            result = self.transport.request("GET", url, limit=MAX_BYTES)
            classification, failure = classify_archive_get(result, original)
            if classification not in {"missing", "matching"}:
                self.coordinator.halt_puts()
            return {"state": "observed", "classification": classification, "failure_category": failure,
                    "status": result.status}
        except Exception:
            self.coordinator.halt_puts()
            raise
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
        if self._refuse_unreceiptable(operation):
            return operation
        if len(operation.archive) > MAX_BYTES:
            operation.state = "not_attempted"
            operation.failure_category = "unsafe_request"
            self._record(operation, event="nupkg_archive_too_large")
            return operation
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
            admitted = self.coordinator.admit_put(operation.id, proof_slots=1)
            if isinstance(admitted, str):
                operation.state = admitted
                operation.failure_category = admitted
                self._record(operation, event="nupkg_put_refused")
                return operation
            reservation = admitted
            self.coordinator.start_put(reservation)
            result = self._put_exchange(PACKAGE_PUBLISH, operation.archive, publish_key)
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
                self._terminal_state(operation)
                self._record(operation, event="nupkg_proof_refused")
                return operation
            try:
                get = self.transport.request("GET", nupkg_get_url(operation.id, version), limit=MAX_BYTES)
            except PrototypeError as error:
                get = ExchangeResult(None, b"", False, str(error), DEFINITELY_NOT_SENT)
            self.coordinator.finish_proof_get(reservation)
            operation.get_status = get.status
            operation.get_classification, get_failure = classify_archive_get(get, operation.archive)
            operation.content_presence = operation.get_classification
            operation.key_presence[operation.id] = operation.get_classification
            if get_failure and operation.failure_category is None:
                operation.failure_category = get_failure
            if operation.get_classification == "conflicting":
                self.coordinator.halt_puts()
            operation.state = operation.put_outcome
            reservation = None
            self._record(operation, event="nupkg_put_get")
            return operation
        except Exception as error:
            if self._on_upload_exception(operation, reservation, error, event="nupkg_exception"):
                reservation = None
                return operation
            reservation = None
            raise
        finally:
            try:
                self._complete_reservation(reservation)
            finally:
                self._release_named(names)

    def put_snupkg_serialized(self, operation: Operation, *, publish_key: str) -> Operation:
        require(operation.kind == "snupkg", "kind_invalid")
        require(sha(operation.archive) == operation.archive_sha256, "original_archive_changed")
        overlap = sorted(self.symbols.overlapping_snupkgs(operation.id))
        keys = sorted(self.symbols.by_snupkg.get(operation.id, ()))
        names = [f"snupkg:{name}" for name in (*overlap, operation.id)] + [f"key:{k}" for k in keys]
        reservation = None
        if self._refuse_unreceiptable(operation):
            return operation
        if len(operation.archive) > MAX_BYTES:
            operation.state = "not_attempted"
            operation.failure_category = "unsafe_request"
            self._record(operation, event="snupkg_archive_too_large")
            return operation
        if not keys:
            self.coordinator.halt_puts()
            operation.state = "pre_observation_unverified"
            operation.failure_category = "snupkg_keys_missing"
            operation.content_presence = "snupkg_archive_unverified"
            self._record(operation, event="snupkg_keys_missing")
            return operation
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
                classification = observed["classification"]
                self.symbols.observed[key] = classification
                operation.key_presence[key] = classification
                if observed.get("state") != "observed" or classification not in PRE_PUT_SKIP | {"missing"}:
                    self.coordinator.halt_puts()
                    if classification == "conflicting":
                        operation.state = "symbol_overlap_conflict"
                        operation.failure_category = "symbol_overlap_conflict"
                    else:
                        operation.state = "pre_observation_unverified"
                        operation.failure_category = observed.get("failure_category") or classification
                    operation.content_presence = aggregate_presence(list(keys), operation.key_presence)
                    self._record(operation, event="snupkg_fail_closed_before_put")
                    return operation
            if keys and all(self.symbols.observed.get(key) in PRE_PUT_SKIP for key in keys):
                if all(self.symbols.by_key[key].input_trust == TRUSTED
                       and self.symbols.observed.get(key) == "inspector_bound_association_observed"
                       for key in keys):
                    operation.state = "pdbs_already_matching_archive_unverified"
                else:
                    operation.state = "pdbs_observed_unverified_skip_put"
                operation.put_outcome = "not_attempted"
                operation.upload_outcome = "not_attempted"
                operation.content_presence = aggregate_presence(list(keys), operation.key_presence)
                self.remote_snupkg_archive_verified = False
                self._record(operation, event="snupkg_skipped_after_reeval")
                return operation
            proof_slots = max(1, len(keys))
            admitted = self.coordinator.admit_put(operation.id, proof_slots=proof_slots)
            if isinstance(admitted, str):
                operation.state = admitted
                operation.failure_category = admitted
                self._record(operation, event="snupkg_put_refused")
                return operation
            reservation = admitted
            self.coordinator.start_put(reservation)
            result = self._put_exchange(SYMBOL_PUBLISH, operation.archive, publish_key)
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
            for key in keys:
                proof = self.coordinator.admit_proof_get(reservation)
                if isinstance(proof, str):
                    operation.failure_category = proof
                    self.coordinator.abandon_remaining_proofs(reservation, release_slots=False)
                    operation.content_presence = aggregate_presence(
                        [k for k in keys], operation.key_presence)
                    self._terminal_state(operation)
                    self._record(operation, event="snupkg_proof_refused")
                    return operation
                item = self.symbols.by_key[key]
                try:
                    get = self.transport.request("GET", symbol_get_url(key), limit=MAX_BYTES)
                except PrototypeError as error:
                    get = ExchangeResult(None, b"", False, str(error), DEFINITELY_NOT_SENT)
                classification, get_failure = classify_association(item, get)
                operation.get_status = get.status
                self.symbols.observed[key] = classification
                operation.key_presence[key] = classification
                self.coordinator.finish_proof_get(reservation)
                if classification == "conflicting":
                    self.coordinator.halt_puts()
                    operation.failure_category = get_failure or "symbol_overlap_conflict"
                    self.coordinator.abandon_remaining_proofs(reservation, release_slots=False)
                    break
                if get_failure and operation.failure_category is None:
                    operation.failure_category = get_failure
            operation.content_presence = aggregate_presence([k for k in keys], operation.key_presence)
            operation.get_classification = operation.content_presence
            operation.state = operation.put_outcome
            self.remote_snupkg_archive_verified = False
            reservation = None
            self._record(operation, event="snupkg_put_get")
            return operation
        except Exception as error:
            if self._on_upload_exception(operation, reservation, error, event="snupkg_exception"):
                reservation = None
                return operation
            reservation = None
            raise
        finally:
            try:
                self._complete_reservation(reservation)
            finally:
                self._release_named(names)

    def finalize(self) -> dict:
        accounting = self.coordinator.reap()
        now = self.coordinator.clock()
        exceeded = self.coordinator.deadline.expired(now)
        self.receipt.append({
            "event": "final", "deadline_exceeded": exceeded,
            "remaining": self.coordinator.deadline.remaining(now),
            "used": self.coordinator.used, "outstanding_proof": self.coordinator.outstanding_proof,
            "unresolved_proofs": accounting["unresolved_proofs"],
            "drained_unsent_puts": accounting["drained_unsent_puts"],
            "durable_io_implemented": False,
            "process_cancel_reap_implemented": False,
            "receipt_overflowed": self.receipt.overflowed,
        }, drop_if_full=True)
        require(not exceeded, "deadline_exceeded")
        return {"receipt_rows": len(self.receipt.rows), "used": self.coordinator.used,
                "unresolved_proofs": accounting["unresolved_proofs"],
                "durable_io_implemented": False, "process_cancel_reap_implemented": False}


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
    """Baseline formula only. Incomplete DAG. Not a global orchestrator."""
    archive_puts = nupkgs + snupkgs
    two_nupkg_passes_plus_three = 2 * nupkgs + extras_in_465
    architect_total = two_nupkg_passes_plus_three + 2 * associations + archive_puts
    return {
        "kind": "baseline_formula_only",
        "dag_complete": False,
        "orchestrator": False,
        "nupkg_puts": nupkgs,
        "snupkg_puts": snupkgs,
        "archive_puts_U": archive_puts,
        "associations_S": associations,
        "unique_symbol_keys": "unknown",
        "metadata_index_extras_in_465": extras_in_465,
        "github_metadata_pages": "unknown",
        "nupkg_get_passes": 2,
        "overlap_rereads": "unknown",
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
