"""Check the BuildKit evidence attached to a digest-pinned Community image using ORAS."""

import argparse
import hashlib
import json
import re
import subprocess
import sys
from pathlib import Path

SPDX = "https://spdx.dev/Document"
SLSA = "https://slsa.dev/provenance/v1"


def read_document(repository, digest, *, blob=False, layout=False):
    if not re.fullmatch(r"sha256:[0-9a-f]{64}", digest):
        raise ValueError(f"Invalid OCI digest: {digest}")
    command = ["oras", "blob" if blob else "manifest", "fetch"]
    if layout:
        command.append("--oci-layout")
    if blob:
        command.extend(["--output", "-"])
    data = subprocess.check_output([*command, "--", f"{repository}@{digest}"])
    if hashlib.sha256(data).hexdigest() != digest.removeprefix("sha256:"):
        raise ValueError(f"Content digest mismatch: {digest}")
    return json.loads(data)


def verify_image(image, output, source_commit, layout=False):
    repository, digest = image.rsplit("@", 1)
    index = read_document(repository, digest, layout=layout)
    children = {}
    attestations = {}
    for descriptor in index["manifests"]:
        annotations = descriptor.get("annotations", {})
        if annotations.get("vnd.docker.reference.type") == "attestation-manifest":
            child = annotations["vnd.docker.reference.digest"]
            if child in attestations:
                raise ValueError(f"Multiple attestation manifests for {child}")
            attestations[child] = descriptor["digest"]
        else:
            platform = descriptor["platform"]
            name = f"{platform['os']}/{platform['architecture']}"
            if name in children:
                raise ValueError(f"Duplicate runnable platform {name}")
            children[name] = descriptor["digest"]

    if set(children) != {"linux/amd64", "linux/arm64"} or len(set(children.values())) != 2:
        raise ValueError("Expected distinct linux/amd64 and linux/arm64 children")
    if set(attestations) != set(children.values()):
        raise ValueError("Attestation descriptors must cover exactly the runnable children")

    results = {}
    for platform, child in sorted(children.items()):
        child_manifest = read_document(repository, child, layout=layout)
        config = read_document(repository, child_manifest["config"]["digest"], blob=True, layout=layout)
        if f"{config['os']}/{config['architecture']}" != platform:
            raise ValueError(f"Runnable image configuration disagrees with platform {platform}")
        if config["config"].get("Labels", {}).get("org.opencontainers.image.revision") != source_commit:
            raise ValueError(f"Unexpected source revision for {platform}")
        manifest = read_document(repository, attestations[child], layout=layout)
        documents = {}
        for layer in manifest["layers"]:
            kind = layer.get("annotations", {}).get("in-toto.io/predicate-type")
            if kind not in (SPDX, SLSA):
                continue
            statement = read_document(repository, layer["digest"], blob=True, layout=layout)
            if layer["mediaType"] != "application/vnd.in-toto+json" or statement["_type"] != "https://in-toto.io/Statement/v1":
                raise ValueError(f"Expected an in-toto v1 statement for {kind} on {platform}")
            subjects = {item["digest"]["sha256"] for item in statement["subject"]}
            if subjects != {child.removeprefix("sha256:")} or statement["predicateType"] != kind:
                raise ValueError(f"Wrong subject or predicate type for {kind} on {platform}")
            if kind in documents:
                raise ValueError(f"Duplicate {kind} for {platform}")
            documents[kind] = statement["predicate"]
        if set(documents) != {SPDX, SLSA}:
            raise ValueError(f"Missing SPDX SBOM or SLSA provenance for {platform}")
        sbom = documents[SPDX]
        if sbom.get("spdxVersion") != "SPDX-2.3" or not sbom.get("packages"):
            raise ValueError(f"Expected an SPDX 2.3 package inventory for {platform}")
        purls = {ref["referenceLocator"] for package in sbom["packages"] for ref in package.get("externalRefs", [])
                 if ref.get("referenceType") == "purl"}
        # All three Community hosts use the Ubuntu ASP.NET runtime and an Elsa host assembly.
        for prefix in ("pkg:deb/", "pkg:nuget/Microsoft.AspNetCore.App.Runtime.", "pkg:nuget/Elsa."):
            if not any(purl.startswith(prefix) for purl in purls):
                raise ValueError(f"Missing expected {prefix} component in {platform} SBOM")
        if not documents[SLSA].get("buildDefinition") or not documents[SLSA].get("runDetails"):
            raise ValueError(f"Missing SLSA build details for {platform}")
        results[platform] = (child, sbom, documents[SLSA])

    output.mkdir(parents=True, exist_ok=True)
    for platform, (child, sbom, provenance) in results.items():
        arch = platform.split("/")[1]
        (output / f"{arch}.spdx.json").write_text(json.dumps(sbom, indent=2) + "\n")
        (output / f"{arch}.provenance.json").write_text(json.dumps(provenance, indent=2) + "\n")
        print(f"{platform} {child} verified SPDX and SLSA v1")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("image", help="repository@sha256:... using the trusted release index digest")
    parser.add_argument("--source-commit", required=True, help="product commit recorded by the trusted publisher")
    parser.add_argument("--output", type=Path, required=True, help="directory for the extracted predicates")
    parser.add_argument("--oci-layout", action="store_true", help="read a local OCI layout instead of a registry")
    args = parser.parse_args()
    try:
        verify_image(args.image, args.output, args.source_commit, args.oci_layout)
    except (ValueError, KeyError, TypeError, OSError, subprocess.CalledProcessError) as error:
        sys.exit(f"Image verification failed: {error}")
