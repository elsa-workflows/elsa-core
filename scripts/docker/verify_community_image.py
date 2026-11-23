"""Check the BuildKit evidence attached to a digest-pinned Community image using ORAS."""

import argparse
import hashlib
import json
import re
import subprocess
import sys
from pathlib import Path
from urllib.parse import parse_qs, unquote, urlsplit
from urllib.request import urlopen

from jsonschema import Draft201909Validator, ValidationError

SPDX = "https://spdx.dev/Document"
SLSA = "https://slsa.dev/provenance/v1"
BUILDKIT_TYPE = "https://github.com/moby/buildkit/blob/master/docs/attestations/slsa-definitions.md"
PUBLISHER = "https://github.com/elsa-workflows/elsa-core/actions/workflows/docker-publish.yml"


SPDX_SCHEMA_URL = "https://raw.githubusercontent.com/spdx/spdx-spec/6f2cb47d13db19b12c23885573b22ef32ec1ada5/schemas/spdx-schema.json"
SPDX_SCHEMA_SHA256 = "4126dc29f15e92feec92e9622f4939131ad58b125d6f578aa93e980a6e6f3212"


def spdx_validator(path=None):
    if path:
        schema = Path(path).read_bytes()
    else:
        with urlopen(SPDX_SCHEMA_URL, timeout=30) as response:
            schema = response.read()
    if hashlib.sha256(schema).hexdigest() != SPDX_SCHEMA_SHA256:
        raise ValueError("SPDX 2.3 schema checksum mismatch")
    return Draft201909Validator(json.loads(schema))


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


def platform_name(platform):
    arch = platform["architecture"]
    variant = platform.get("variant", "")
    baseline = {"arm64": "v8", "amd64": "v1"}.get(arch)
    if variant and variant != baseline:
        raise ValueError(f"Unsupported {arch} platform variant {variant}")
    return f"{platform['os']}/{arch}"


PROFILES = {
    "elsa-server-v3": ("ElsaServer.Dockerfile", {"Elsa.Server.Web"}),
    "elsa-server-and-studio-v3": ("ElsaServerAndStudio.Dockerfile", {"Elsa.ServerAndStudio.Web", "Elsa.ModularServer.Web"}),
    "elsa-studio-v3": ("ElsaStudio.Dockerfile", {"Elsa.Studio.Host.Server"}),
}


def check_components(packages, platform, config, image_name):
    # Match actual SPDX package identity and version to the PURL, and to the .NET command this image runs.
    command = config.get("Cmd")
    if not isinstance(command, list) or len(command) != 2 or command[0] != "dotnet" or not command[1].endswith(".dll"):
        raise ValueError(f"Unexpected .NET host command for {image_name}")
    host = command[1][:-4]
    hosts = PROFILES[image_name][1]
    version = config.get("Labels", {}).get("org.opencontainers.image.version", "")
    if image_name == "elsa-studio-v3" and re.fullmatch(r"3\.[0-9]\.\d+(-preview\.\d+)?", version):
        hosts = hosts | {"Elsa.ModularServer.Web"}  # Legacy Studio image, e.g. 3.9.0.
    if host not in hosts:
        raise ValueError(f"Unexpected entry assembly {host} for {image_name}")
    arch = platform.split("/")[1]
    runtime = "Microsoft.AspNetCore.App.Runtime." + {"amd64": "linux-x64", "arm64": "linux-arm64"}[arch]
    required = {host, runtime, "libc6"}
    found = set()
    for package in packages:
        name, version = package["name"], package.get("versionInfo", "")
        if name not in required or not version:
            continue
        for ref in package.get("externalRefs", []):
            if ref.get("referenceType") != "purl" or ref.get("referenceCategory") != "PACKAGE-MANAGER":
                continue
            locator = urlsplit(ref["referenceLocator"])
            if locator.scheme != "pkg" or locator.netloc or locator.fragment:
                continue
            path, _, purl_version = locator.path.rpartition("@")
            if version != unquote(purl_version):
                continue
            if name == "libc6":
                if (path in {"deb/ubuntu/libc6", "deb/debian/libc6"} and
                        re.fullmatch(r"[0-9][A-Za-z0-9.+:~\-]*", version) and parse_qs(locator.query).get("arch") == [arch]):
                    found.add(name)
            elif path == "nuget/" + name and re.fullmatch(r"[0-9]+(?:\.[0-9]+){0,3}(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?", version):
                found.add(name)
    if required != found:
        raise ValueError(f"Missing versioned packages for {platform}: {', '.join(sorted(required - found))}")


def verify_image(image, output, source_commit, validator, layout=False, image_name=None):
    repository, digest = image.rsplit("@", 1)
    image_name = image_name if layout else repository.rsplit("/", 1)[-1]
    if image_name not in PROFILES:
        raise ValueError("Expected a Community image name; use --image-name for a local layout")
    index = read_document(repository, digest, layout=layout)
    children = {}
    attestations = {}
    for descriptor in index["manifests"]:
        annotations = descriptor.get("annotations", {})
        if annotations.get("vnd.docker.reference.type") == "attestation-manifest":
            if platform_name(descriptor["platform"]) != "unknown/unknown":
                raise ValueError("Attestation descriptor must declare a non-runnable platform")
            child = annotations["vnd.docker.reference.digest"]
            if child in attestations:
                raise ValueError(f"Multiple attestation manifests for {child}")
            attestations[child] = descriptor["digest"]
        else:
            name = platform_name(descriptor["platform"])
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
        if platform_name(config) != platform:
            raise ValueError(f"Runnable image configuration disagrees with platform {platform}")
        if config["config"].get("Labels", {}).get("org.opencontainers.image.revision") != source_commit:
            raise ValueError(f"Unexpected source revision for {platform}")
        manifest = read_document(repository, attestations[child], layout=layout)
        # BuildKit supports both its original attestation manifests and OCI 1.1 artifact manifests.
        if "artifactType" in manifest or "subject" in manifest:
            if manifest.get("subject", {}).get("digest") != child:
                raise ValueError(f"Wrong attestation manifest OCI subject for {platform}")
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
        try:
            validator.validate(sbom)
        except ValidationError as error:
            raise ValueError(f"Invalid SPDX 2.3 inventory for {platform}: {error.message}") from error
        if sbom.get("spdxVersion") != "SPDX-2.3" or not sbom.get("packages"):
            raise ValueError(f"Expected an SPDX 2.3 package inventory for {platform}")
        check_components(sbom["packages"], platform, config["config"], image_name)
        definition = documents[SLSA].get("buildDefinition")
        details = documents[SLSA].get("runDetails")
        if not isinstance(definition, dict) or definition.get("buildType") != BUILDKIT_TYPE:
            raise ValueError(f"Unexpected SLSA build type for {platform}")
        external = definition.get("externalParameters")
        if not isinstance(external, dict) or external.get("request", {}).get("frontend") != "dockerfile.v0":
            raise ValueError(f"Missing Dockerfile build request in SLSA external parameters for {platform}")
        dockerfile = PROFILES[image_name][0]
        if external.get("configSource", {}).get("path") not in {dockerfile, f"docker/{dockerfile}"}:
            raise ValueError(f"Expected {dockerfile} in SLSA config source for {platform}")
        valid_targets = {platform, platform + {"linux/arm64": "/v8", "linux/amd64": "/v1"}[platform]}
        # Build and scanner inputs may use the builder architecture. The final ASP.NET base must match the child.
        bases = []
        for item in definition.get("resolvedDependencies", []):
            uri = urlsplit(item["uri"])
            if uri.scheme == "pkg" and uri.path.split("@", 1)[0] == "docker/mcr.microsoft.com/dotnet/aspnet":
                if not re.fullmatch(r"[0-9a-f]{64}", item.get("digest", {}).get("sha256", "")):
                    raise ValueError(f"Invalid ASP.NET base digest for {platform}")
                bases.append(parse_qs(uri.query).get("platform", []))
        if not bases or any(len(targets) != 1 or targets[0] not in valid_targets for targets in bases):
            raise ValueError(f"Expected build inputs with the ASP.NET runtime base for {platform}")
        # Older BuildKit versions omit targetPlatform. builderPlatform refers to the build machine.
        params = definition.get("internalParameters", {})
        if not isinstance(params, dict) or params.get("targetPlatform", platform) not in valid_targets:
            raise ValueError(f"Unexpected provenance target for {platform}")
        if not params.get("buildConfig", {}).get("llbDefinition"):
            raise ValueError(f"Missing BuildKit build configuration for {platform}")
        if not isinstance(details, dict) or not details.get("metadata", {}).get("invocationId"):
            raise ValueError(f"Missing BuildKit build invocation for {platform}")
        builder = details.get("builder") if isinstance(details, dict) else None
        if not isinstance(builder, dict) or builder.get("id") != PUBLISHER:
            raise ValueError(f"Unexpected SLSA builder identity for {platform}")
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
    parser.add_argument("--spdx-schema", type=Path, help="local copy of the pinned SPDX 2.3 JSON schema for offline use")
    parser.add_argument("--image-name", choices=sorted(PROFILES), help="expected Community image when reading a local layout")
    args = parser.parse_args()
    try:
        verify_image(args.image, args.output, args.source_commit, spdx_validator(args.spdx_schema), args.oci_layout, args.image_name)
    except (ValueError, KeyError, TypeError, AttributeError, OSError, subprocess.CalledProcessError) as error:
        sys.exit(f"Image verification failed: {error}")
