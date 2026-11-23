"""Rescan signed, already-published Elsa 3 releases. See SECURITY.md."""

import argparse
import hashlib
import json
import re
import signal
import subprocess
import sys
import tempfile
import zipfile
from datetime import datetime, timedelta, timezone
from pathlib import Path
from urllib.error import HTTPError, URLError
from urllib.request import urlopen

REPO = "elsa-workflows/elsa-core"
ISSUE = 9315
GRYPE = "0.122.1"
# Stable releases already published when the release-evidence rollout began.
ROLLOUT = "2026-11-23T00:00:00Z"
INDEX = "nuget-release-index.json"
BUNDLE = "nuget-sboms.zip"
DIGEST = re.compile(r"[a-f0-9]{64}")
APIS = f"repos/{REPO}"
# Import the repository verifier directly, independent of an installed Docker Python SDK.
sys.path.insert(0, str(Path(__file__).resolve().parent / "docker"))
IMAGE_WORKFLOW = f"{REPO}/.github/workflows/docker-publish.yml"


def require(condition, message):
    if not condition:
        raise ValueError(message)


def digest(data):
    return hashlib.sha256(data).hexdigest()


def run(*args):
    return subprocess.check_output(args, text=True).strip()


def lines(releases):
    groups = {}
    for release in releases:
        tag = release["tag_name"]
        if release["draft"] or release["prerelease"] or not re.fullmatch(r"3\.(0|[1-9]\d*)\.(0|[1-9]\d*)", tag):
            continue
        major, minor, patch = map(int, tag.split("."))
        groups.setdefault((major, minor), []).append((patch, release))
    require(groups, "No supported public stable GitHub Releases found")
    return [[r for _, r in sorted(group, key=lambda item: item[0], reverse=True)]
            for _, group in sorted(groups.items())]


def release_commit(tag):
    commit = run("gh", "api", f"{APIS}/commits/refs/tags/{tag}", "--jq", ".sha")
    require(re.fullmatch(r"[0-9a-f]{40}", commit), "Unexpected release tag identity")
    return commit


def read_feed(url, feed, failures):
    # Release-publication propagation retries are inappropriate for periodic monitoring.
    require(failures.get(feed, 0) < 3, f"{feed} readback deferred after three consecutive transient failures")
    try:
        with urlopen(url, timeout=15) as response:
            content = response.read()
    except (URLError, TimeoutError, ConnectionError) as error:
        if not isinstance(error, HTTPError) or error.code >= 500 or error.code == 429:
            failures[feed] = failures.get(feed, 0) + 1
        raise
    failures[feed] = 0
    return content


def packages(tag, commit, directory, evidence, feed_failures):
    import nuget_release_evidence as publisher

    download = ["gh", "release", "download", tag, "--repo", REPO, "--dir", str(directory), "--pattern"]
    run(*download, INDEX)
    index_file = directory / INDEX
    publisher.verify_attestation(index_file, publisher.SIGNER)
    raw = index_file.read_bytes()
    index = json.loads(raw)
    require((index["schema"], index["version"], index["source_commit"]) == (1, tag, commit), "Wrong release index")
    evidence["release_sha256"] = digest(raw)
    evidence["source_commit"] = commit
    run(*download, BUNDLE)
    bundle = directory / BUNDLE
    require(index["sbom_bundle"] == {"name": BUNDLE, "sha256": digest(bundle.read_bytes())}, "Wrong SBOM bundle")
    subjects = []
    errors = {}
    evidence["verification_errors"] = errors
    with zipfile.ZipFile(bundle) as archive:
        require(len(set(archive.namelist())) == len(archive.namelist()), "Duplicate SBOM archive member")
        source = directory / publisher.CANDIDATE
        source.write_bytes(archive.read(publisher.CANDIDATE))
        publisher.verify_attestation(source, publisher.PRODUCER, index["tooling_commit"])
        candidate = json.loads(source.read_bytes())
        require(all(candidate[k] == index[k] for k in ("schema", "version", "source_commit", "tooling_commit",
                                                        "producer_run", "producer_attempt")), "Wrong candidate provenance")
        rows = index["packages"]
        require(rows and [{k: v for k, v in row.items() if k != "feeds"} for row in rows]
                == candidate["packages"], "Candidate and release package mappings differ")
        identifiers = [r["id"].lower() for r in rows]
        require(len(set(identifiers)) == len(rows) and all(publisher.candidates.PACKAGE_ID.fullmatch(i) for i in identifiers),
                "Invalid or duplicate package identity")
        for row, identifier in zip(rows, identifiers):
            feeds = row["feeds"]
            if "feedz" not in feeds or not feeds.keys() <= publisher.FEEDS.keys() or not row["frameworks"]:
                errors[identifier] = "Missing or unexpected package feed or framework"
                continue
            owners = []
            for feed, record in sorted(feeds.items()):
                try:
                    url = f"{publisher.FEEDS[feed]}/{identifier}/{tag}/{identifier}.{tag}.nupkg"
                    require(record["url"] == url and digest(read_feed(url, feed, feed_failures)) == record["sha256"], "Wrong feed artifact")
                    if feed == "feedz":
                        require(record["sha256"] == row["candidate_sha256"], "Feedz artifact differs from candidate subject")
                    owners.append({"name": row["id"], "feed": feed, "sha256": record["sha256"]})
                except Exception as error:
                    errors[f"{identifier}/{feed}"] = error_message(error)
            if not owners:
                continue
            records = list(row["frameworks"].items()) + [("browser-build-input", r) for r in row["browser_build_inputs"]]
            for context, record in records:
                try:
                    content = archive.read(record["path"])
                    checksum = digest(content)
                    require(checksum == record["sha256"], "Wrong inventory digest")
                    if context != "browser-build-input":
                        publisher.check_bom(content, row, tag, context)
                    path = directory / checksum
                    path.write_bytes(content)
                    subject = {"context": context, "inventory": record["path"], "sbom_sha256": checksum,
                               "artifacts": owners, "release_sha256": evidence["release_sha256"]}
                    subjects.append((path, subject))
                    evidence.setdefault("inventories", []).append(subject)
                except Exception as error:
                    errors[f"{identifier}/{context}/{record.get('path', '<missing path>')}"] = error_message(error)
    if not errors:
        del evidence["verification_errors"]
    return subjects


def image(tag, commit, directory, name, evidence):
    import verify_community_image as publisher

    repository = f"docker.io/elsaworkflows/{name}"
    index = run("oras", "resolve", f"{repository}:{tag}")
    require(index.startswith("sha256:") and DIGEST.fullmatch(index[7:]), "Invalid registry index digest")
    reference = f"{repository}@{index}"
    args = ["gh", "attestation", "verify", f"oci://{reference}", "--repo", REPO, "--signer-workflow", IMAGE_WORKFLOW,
            "--deny-self-hosted-runners", "--predicate-type", "https://slsa.dev/provenance/v1", "--format", "json"]
    # The publisher accepts an exact version-tag build or a manual promotion from main.
    try:
        proofs = run(*args, "--source-ref", f"refs/tags/{tag}", "--source-digest", commit)
    except subprocess.CalledProcessError:
        proofs = run(*args, "--source-ref", "refs/heads/main")
    require(any(any(s["name"] == repository and s["digest"].get("sha256") == index[7:]
                    for s in proof["verificationResult"]["statement"]["subject"]) for proof in json.loads(proofs)),
            "Attestation subject does not name the official registry repository and digest")
    evidence["release_sha256"] = index[7:]
    publisher.verify_image(reference, directory, commit, publisher.spdx_validator())
    result = []
    for descriptor in publisher.read_document(repository, index)["manifests"]:
        platform = descriptor["platform"]
        if platform["os"] != "linux":
            continue
        child = descriptor["digest"]
        manifest = publisher.read_document(repository, child)
        config = publisher.read_document(repository, manifest["config"]["digest"], blob=True)
        require(config["config"]["Labels"]["org.opencontainers.image.version"] == tag, "Image version mismatch")
        path = directory / (platform["architecture"] + ".spdx.json")
        subject = {"context": publisher.platform_name(platform), "inventory": name,
                   "sbom_sha256": digest(path.read_bytes()), "release_sha256": index[7:],
                   "artifacts": [{"name": repository, "sha256": child[7:]}]}
        result.append((path, subject))
        evidence.setdefault("inventories", []).append(subject)
    return result


def database(status):
    require(status.get("valid") is True, "Grype database unavailable or invalid: " + status.get("error", "unknown reason"))
    built = datetime.fromisoformat(status["built"].replace("Z", "+00:00"))
    require(timedelta(0) <= datetime.now(timezone.utc) - built <= timedelta(days=5), "Grype database stale or future dated")
    checksum = re.search(r"[?&]checksum=sha256(?:%3A|:)([a-f0-9]{64})(?:&|$)", status["from"])
    require(checksum, "Missing Grype database archive checksum")
    return {"schema": status["schemaVersion"], "built": status["built"], "sha256": checksum[1]}


def scan(path, expected_db):
    content = json.loads(path.read_bytes())
    # npm's bundled build-input inventories use CycloneDX 1.5; framework inventories use 1.6.
    cyclonedx = content.get("bomFormat") == "CycloneDX" and content.get("specVersion") in {"1.5", "1.6"}
    require(cyclonedx or content.get("spdxVersion") == "SPDX-2.3", "Unsupported inventory")
    result = json.loads(run("grype", f"sbom:{path}", "--output", "json"))
    descriptor = result["descriptor"]
    require(descriptor["version"] == GRYPE and database(descriptor["db"]["status"]) == expected_db,
            "Scanner or database revision changed")
    matches = {}
    for match in result["matches"]:
        component = match["artifact"]
        advisory = match["vulnerability"]
        identity = component.get("purl") or f"{component.get('type', 'unknown')}:{component['name']}@{component.get('version') or 'unknown'}"
        matches[identity, advisory["id"]] = {"component": identity, "advisory": advisory["id"],
                                              "severity": advisory.get("severity") or "Unknown", "fix": advisory.get("fix")}
    return [matches[k] for k in sorted(matches)]


def error_message(error):
    if isinstance(error, subprocess.CalledProcessError):
        # Temporary input paths vary between runs. Full subprocess diagnostics are in the Actions log.
        command = str(error.cmd[0])
        operation = str(error.cmd[1]).split(":", 1)[0]
        return f"{command} {operation} failed (exit {error.returncode})"
    return str(error)


def refresh_database(cache):
    # Grype emits structured status even on a nonzero exit (e.g. no DB or stale DB).
    # A failed refresh is still an operational error when an older cache is present.
    update = subprocess.run(["grype", "db", "update"])
    check = subprocess.run(["grype", "db", "status", "-o", "json"], text=True, stdout=subprocess.PIPE)
    status = json.loads(check.stdout)
    cache["database_status"] = {key: status[key] for key in ("schemaVersion", "built", "from", "valid", "error") if key in status}
    revision = database(status)
    check.check_returncode()
    update.check_returncode()
    cache["database"] = revision


def attempt(release, family, stage, cache, record=None, progress=None):
    tag = release["tag_name"]
    if record is None:
        record = {"version": tag, "family": family, "state": "error"}
    try:
        assets = {a["name"] for a in release["assets"]}
        if family == "nuget":
            require(INDEX in assets, "Missing signed NuGet release index")
        commit = release_commit(tag)
        with tempfile.TemporaryDirectory(dir=stage) as tmp:
            folder = Path(tmp)
            if family == "nuget":
                subjects = packages(tag, commit, folder, record, cache.setdefault("feed_failures", {}))
            else:
                subjects = image(tag, commit, folder, family, record)
            require(subjects, "No verified inventories")
            record["source_commit"] = commit
            record["release_sha256"] = subjects[0][1]["release_sha256"]
            record["inventories"] = [subject for _, subject in subjects]
            if progress:
                progress()
            if "database" not in cache and "database_error" not in cache:
                try:
                    refresh_database(cache)
                except Exception as error:
                    cache["database_error"] = error_message(error)
            if "database_error" in cache:
                if "database_status" in cache:
                    record["database_status"] = cache["database_status"]
                raise ValueError(cache["database_error"])
            record["database"] = cache["database"]
            findings = {}
            record["findings"] = []
            errors = {}
            record["inventory_errors"] = errors
            if progress:
                progress()
            for path, subject in subjects:
                key = subject["sbom_sha256"]
                if key not in cache:
                    try:
                        cache[key] = scan(path, cache["database"])
                    except Exception as error:
                        errors[key] = error_message(error)
                        subject["scan_state"] = "error"
                        if progress:
                            progress()
                        continue
                for owner in subject["artifacts"]:
                    for match in cache[key]:
                        k = owner["sha256"], match["component"], match["advisory"]
                        if k not in findings:
                            findings[k] = {**match, "subject_sha256": k[0], "inventories": []}
                            record["findings"].append(findings[k])
                        if key not in findings[k]["inventories"]:
                            findings[k]["inventories"].append(key)
                subject["scan_state"] = "scanned"
                if progress:
                    progress()
            if not errors:
                del record["inventory_errors"]
            if errors:
                record["error"] = f"Grype could not scan {len(errors)} verified inventory digest(s)"
            elif record.get("verification_errors"):
                record["error"] = "Incomplete verified release coverage"
            else:
                record["state"] = "findings" if findings else "clean"
    except Exception as error:
        # Each independent release family gets an outcome even if another scanner or remote fails.
        record["error"] = error_message(error)
    return record


def registry_backfills(name):
    # A trusted version tag may be republished for an older GitHub Release. Discovery
    # never supplies a trust decision: each selected index is resolved and verified afresh.
    tags = set()
    page = 1
    while True:
        url = f"https://hub.docker.com/v2/namespaces/elsaworkflows/repositories/{name}/tags?page_size=100&page={page}"
        with urlopen(url, timeout=45) as response:
            data = json.load(response)
        for tag in data["results"]:
            if tag["last_updated"][:10] >= ROLLOUT[:10]:
                tags.add(tag["name"])
        if not data["next"]:
            return tags
        page += 1


def checkpoint(report_path, reports, active=None):
    if report_path is None:
        return
    results = reports if active is None else [*reports, dict(active, state="error",
                                                            error="Monitoring interrupted before this operation completed")]
    temporary = report_path.with_suffix(".tmp")
    temporary.write_text(json.dumps({"scanner": GRYPE, "results": results}, separators=(",", ":")) + "\n")
    temporary.replace(report_path)


class MonitoringInterrupted(BaseException):
    """The scan deadline interrupted the active release operation."""


def on_deadline(signum, frame):
    # Bypass per-distribution Exception handlers so the active partial result is immediately finalized.
    raise MonitoringInterrupted()


def monitor(report_path=None):
    with tempfile.TemporaryDirectory() as tmp:
        reports = []
        cache = {}
        active = None
        checkpoint(report_path, reports, {"version": "discovery", "family": "releases"})
        try:
            pages = json.loads(run("gh", "api", "--paginate", "--slurp", f"{APIS}/releases?per_page=100"))
            groups = lines([r for page in pages for r in page])
            backfills = {}
            for name in ("elsa-server-v3", "elsa-server-and-studio-v3", "elsa-studio-v3"):
                try:
                    backfills[name] = registry_backfills(name)
                except Exception as error:
                    reports.append({"version": "discovery", "family": name, "state": "error", "error": str(error)})
            for group in groups:
                families = ["nuget", "elsa-server-v3", "elsa-server-and-studio-v3"]
                if not group[0]["tag_name"].startswith("3.10."):
                    families.append("elsa-studio-v3")
                for family in families:
                    for release in group:
                        historical = release["published_at"] < ROLLOUT
                        has_assets = any(a["name"] in {INDEX, BUNDLE} for a in release["assets"])
                        if historical and family != "nuget" and family not in backfills:
                            continue  # Already recorded a registry discovery error.
                        has_backfill = has_assets if family == "nuget" else release["tag_name"] in backfills.get(family, set())
                        if historical and not has_backfill:
                            # Pre-rollout artifacts are never rebuilt by this job.
                            if release == group[0]:
                                reports.append({"version": release["tag_name"], "family": family,
                                                "state": "predates-evidence"})
                            continue
                        active = {"version": release["tag_name"], "family": family, "state": "error"}
                        def progress():
                            checkpoint(report_path, reports, active)
                        progress()
                        record = attempt(release, family, tmp, cache, active, progress)
                        reports.append(record)
                        active = None
                        # Corrupt input can be specific to a release. A shared database failure cannot.
                        if record["state"] != "error" or "database_error" in cache:
                            break
        except MonitoringInterrupted:
            interrupted = active if active is not None else {"version": "discovery", "family": "releases"}
            reports.append(dict(interrupted, state="error", error="Monitoring deadline interrupted this operation"))
        except Exception as error:
            reports.append({"version": "discovery", "family": "releases", "state": "error", "error": str(error)})
        checkpoint(report_path, reports)
        return reports


def finding_ids(record):
    return sorted((f["subject_sha256"], f["component"], f["advisory"]) for f in record.get("findings", []))


def receipt_key(record):
    fields = ("version", "family", "state", "release_sha256", "database", "inventory_errors", "verification_errors")
    error = record.get("error")
    if "database_status" in record:
        # Relative age in a stale-DB diagnostic changes between attempts.
        status = record["database_status"]
        error = ["database", status.get("valid"), status.get("built"), status.get("from")]
    scanned = sorted({s["sbom_sha256"] for s in record.get("inventories", [])
                      if isinstance(s, dict) and s.get("scan_state") == "scanned"}) if record["state"] == "error" else []
    key = [GRYPE, error, finding_ids(record), scanned] + [record.get(k) for k in fields]
    return digest(json.dumps(key, sort_keys=True).encode())


def receipts(records, run_url):
    path = f"{APIS}/issues/{ISSUE}/comments"
    pages = json.loads(run("gh", "api", "--paginate", "--slurp", path + "?per_page=100"))
    seen = set()
    finding_sets = set()
    for page in pages:
        for comment in page:
            if comment["user"]["login"] == "github-actions[bot]":
                seen.update(re.findall(r"<!-- elsa-rescan:([a-f0-9]{64}) -->", comment["body"]))
                finding_sets.update(re.findall(r"<!-- elsa-findings:([a-f0-9]{64}) -->", comment["body"]))
    updates = []
    for record in records:
        key = receipt_key(record)
        if key in seen:
            continue
        seen.add(key)
        identifier = record.get("release_sha256", "unavailable")
        db = record.get("database", {})
        details = "coverage incomplete"
        markers = f"<!-- elsa-rescan:{key} -->"
        if "findings" in record:
            ids = finding_ids(record)
            finding_key = digest(json.dumps([identifier, ids]).encode())
            markers += f"<!-- elsa-findings:{finding_key} -->"
            details = (f"{len(ids)} distinct matches" if record["state"] in {"clean", "findings"}
                       else f"coverage incomplete; {len(ids)} observed distinct matches in scanned inventories")
            if finding_key in finding_sets:
                details += " (previously recorded finding set)"
            finding_sets.add(finding_key)
        updates.append(f"{markers}\n- {record['version']} {record['family']} · {record['state']} · {details} · "
                       f"subject `{identifier}` · database `{db.get('sha256', 'unavailable')}`")
    # The issue API caps each comment at 65,536 characters.
    for offset in range(0, len(updates), 75):
        body = f"Release inventory rescan with Grype {GRYPE}. [Run and report]({run_url})\n\n" + "\n".join(updates[offset:offset + 75])
        run("gh", "api", "-X", "POST", path, "-f", "body=" + body)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=["scan", "receipt"])
    parser.add_argument("report", type=Path)
    parser.add_argument("--run-url")
    args = parser.parse_args()
    if args.command == "receipt":
        receipts(json.loads(args.report.read_bytes())["results"], args.run_url)
        return
    signal.signal(signal.SIGTERM, on_deadline)
    reports = monitor(args.report)
    for record in reports:
        print(record["version"], record["family"], record["state"], len(record.get("findings", []))
              if "findings" in record else "", record.get("error", ""))
    sys.exit(any(r["state"] == "error" for r in reports))


if __name__ == "__main__":
    main()
