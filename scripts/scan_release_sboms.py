"""Rescan signed, already-published Elsa 3 releases. See SECURITY.md."""

import argparse
import hashlib
import json
import re
import subprocess
import sys
import tempfile
import zipfile
from datetime import datetime, timedelta, timezone
from pathlib import Path
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


def packages(tag, commit, directory):
    import nuget_release_evidence as publisher

    for name in (INDEX, BUNDLE):
        run("gh", "release", "download", tag, "--repo", REPO, "--pattern", name, "--dir", str(directory))
    index_file = directory / INDEX
    publisher.verify_attestation(index_file, publisher.SIGNER)
    raw = index_file.read_bytes()
    index = json.loads(raw)
    require((index["schema"], index["version"], index["source_commit"]) == (1, tag, commit), "Wrong release index")
    bundle = directory / BUNDLE
    require(index["sbom_bundle"] == {"name": BUNDLE, "sha256": digest(bundle.read_bytes())}, "Wrong SBOM bundle")
    subjects = []
    index_sha = digest(raw)
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
        seen = set()
        for row in rows:
            identifier = row["id"].lower()
            require(publisher.candidates.PACKAGE_ID.fullmatch(identifier) and identifier not in seen, "Invalid package ID")
            seen.add(identifier)
            feeds = row["feeds"]
            require("feedz" in feeds and feeds.keys() <= publisher.FEEDS.keys() and row["frameworks"], "Missing feed or framework")
            owners = []
            for feed, record in sorted(feeds.items()):
                url = f"{publisher.FEEDS[feed]}/{identifier}/{tag}/{identifier}.{tag}.nupkg"
                require(record["url"] == url and digest(publisher.download(url)) == record["sha256"], "Wrong feed artifact")
                owners.append({"name": row["id"], "feed": feed, "sha256": record["sha256"]})
            records = list(row["frameworks"].items()) + [("browser-build-input", r) for r in row["browser_build_inputs"]]
            for context, record in records:
                content = archive.read(record["path"])
                checksum = digest(content)
                require(checksum == record["sha256"], "Wrong inventory digest")
                if context != "browser-build-input":
                    publisher.check_bom(content, row, tag, context)
                path = directory / checksum
                path.write_bytes(content)
                subjects.append((path, {"context": context, "inventory": record["path"], "sbom_sha256": checksum,
                                        "artifacts": owners, "release_sha256": index_sha}))
    return subjects


def image(tag, commit, directory, name):
    from docker import verify_community_image as publisher

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
        result.append((path, {"context": publisher.platform_name(platform), "inventory": name,
                              "sbom_sha256": digest(path.read_bytes()), "release_sha256": index[7:],
                              "artifacts": [{"name": repository, "sha256": child[7:]}]}))
    return result


def database(status):
    require(status.get("valid") is True, "Grype database unavailable or invalid")
    built = datetime.fromisoformat(status["built"].replace("Z", "+00:00"))
    require(timedelta(0) <= datetime.now(timezone.utc) - built <= timedelta(days=5), "Grype database stale or future dated")
    checksum = re.search(r"[?&]checksum=sha256(?:%3A|:)([a-f0-9]{64})(?:&|$)", status["from"])
    require(checksum, "Missing Grype database archive checksum")
    return {"schema": status["schemaVersion"], "built": status["built"], "sha256": checksum[1]}


def scan(path, expected_db):
    content = json.loads(path.read_bytes())
    require((content.get("bomFormat"), content.get("specVersion")) == ("CycloneDX", "1.6")
            or content.get("spdxVersion") == "SPDX-2.3", "Unsupported inventory")
    result = json.loads(run("grype", f"sbom:{path}", "--output", "json"))
    descriptor = result["descriptor"]
    require(descriptor["version"] == GRYPE and database(descriptor["db"]["status"]) == expected_db,
            "Scanner or database revision changed")
    matches = {}
    for match in result["matches"]:
        component = match["artifact"]
        advisory = match["vulnerability"]
        identity = component["purl"] or f"{component['type']}:{component['name']}@{component['version']}"
        matches[identity, advisory["id"]] = {"component": identity, "advisory": advisory["id"],
                                              "severity": advisory["severity"], "fix": advisory["fix"]}
    return [matches[k] for k in sorted(matches)]


def attempt(release, family, stage, cache):
    tag = release["tag_name"]
    record = {"version": tag, "family": family, "state": "error"}
    try:
        assets = {a["name"] for a in release["assets"]}
        if family == "nuget":
            require({INDEX, BUNDLE} <= assets, "Missing signed NuGet release assets")
        commit = release_commit(tag)
        with tempfile.TemporaryDirectory(dir=stage) as tmp:
            folder = Path(tmp)
            subjects = packages(tag, commit, folder) if family == "nuget" else image(tag, commit, folder, family)
            require(subjects, "No verified inventories")
            record["source_commit"] = commit
            record["release_sha256"] = subjects[0][1]["release_sha256"]
            if "database" not in cache:
                run("grype", "db", "update")
                cache["database"] = database(json.loads(run("grype", "db", "status", "-o", "json")))
            record["database"] = cache["database"]
            inventories = []
            findings = {}
            for path, subject in subjects:
                key = subject["sbom_sha256"]
                if key not in cache:
                    cache[key] = scan(path, cache["database"])
                inventories.append(subject)
                for owner in subject["artifacts"]:
                    for match in cache[key]:
                        k = owner["sha256"], match["component"], match["advisory"]
                        findings.setdefault(k, {**match, "subject_sha256": k[0], "inventories": set()})["inventories"].add(key)
            record.update(inventories=inventories, findings=[dict(findings[k], inventories=sorted(findings[k]["inventories"]))
                                                             for k in sorted(findings)])
            record["state"] = "findings" if findings else "clean"
    except Exception as error:
        # Each independent release family gets an outcome even if another scanner or remote fails.
        record["error"] = str(error)
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


def monitor():
    with tempfile.TemporaryDirectory() as tmp:
        reports = []
        cache = {}
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
                        has_evidence_candidate = has_assets if family == "nuget" else release["tag_name"] in backfills[family]
                        if historical and not has_evidence_candidate:
                            # Pre-rollout artifacts are never rebuilt by this job.
                            if release == group[0]:
                                reports.append({"version": release["tag_name"], "family": family,
                                                "state": "predates-evidence"})
                            continue
                        record = attempt(release, family, tmp, cache)
                        reports.append(record)
                        if record["state"] != "error":
                            break
        except Exception as error:
            reports.append({"version": "discovery", "family": "releases", "state": "error", "error": str(error)})
        return reports


def receipt_key(record):
    fields = ("version", "family", "state", "release_sha256", "database", "error")
    return digest(json.dumps([GRYPE] + [record.get(k) for k in fields], sort_keys=True).encode())


def receipts(records, run_url):
    path = f"{APIS}/issues/{ISSUE}/comments"
    pages = json.loads(run("gh", "api", "--paginate", "--slurp", path + "?per_page=100"))
    seen = set()
    for page in pages:
        for comment in page:
            if comment["user"]["login"] == "github-actions[bot]":
                seen.update(re.findall(r"<!-- elsa-rescan:([a-f0-9]{64}) -->", comment["body"]))
    updates = []
    for record in records:
        key = receipt_key(record)
        if key in seen:
            continue
        seen.add(key)
        identifier = record.get("release_sha256", "unavailable")
        db = record.get("database", {})
        count = len(record["findings"]) if "findings" in record else "unavailable"
        updates.append(f"<!-- elsa-rescan:{key} -->\n- {record['version']} {record['family']} · {record['state']} · "
                       f"{count} distinct matches · subject `{identifier}` · database `{db.get('sha256', 'unavailable')}`")
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
    reports = monitor()
    args.report.write_text(json.dumps({"scanner": GRYPE, "results": reports}, indent=2) + "\n")
    for record in reports:
        print(record["version"], record["family"], record["state"], len(record.get("findings", []))
              if "findings" in record else "", record.get("error", ""))
    sys.exit(any(r["state"] == "error" for r in reports))


if __name__ == "__main__":
    main()
