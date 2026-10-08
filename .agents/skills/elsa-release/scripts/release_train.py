#!/usr/bin/env python3
"""Checkpoint and verify an agent-operated Elsa release; never publish implicitly."""
from __future__ import annotations

import argparse
import contextlib
import fcntl
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import tempfile
import urllib.error
import urllib.request
import zipfile
from datetime import datetime, timedelta, timezone
from urllib.parse import quote, urlparse

from release_support import parse_version

HERE = Path(__file__).resolve().parent
DEFAULT_PROFILE = HERE.parent / 'references' / 'elsa-profile.json'
SITE_TARGETS = ('website', 'documentation')
VALID_SITE_STATUSES = {'completed', 'published', 'verified'}
RECOVERY_REGISTRY = 'nuget.org'


def command(args, cwd=None):
    result = subprocess.run(args, cwd=cwd, text=True, capture_output=True, timeout=120)
    if result.returncode:
        raise ValueError(result.stderr.strip() or f'Command failed: {args[0]}')
    return result.stdout.strip()


def gh(*args):
    return json.loads(command(['gh', *args]))


def read(path):
    return json.loads(Path(path).read_text())


def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def save(path, value):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.NamedTemporaryFile(mode='w', dir=path.parent, delete=False) as f:
        json.dump(value, f, indent=2, ensure_ascii=False)
        f.write('\n')
        f.flush()
        os.fsync(f.fileno())
        temporary = f.name
    os.replace(temporary, path)


@contextlib.contextmanager
def locked(path):
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.with_suffix('.lock').open('a') as lock:
        try:
            fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError:
            raise ValueError('Another process is updating this release state')
        yield


def config(state, name):
    return next(r for r in state['profile']['repositories'] if r['name'] == name)


def entry(state, name):
    if name not in state['repositories']:
        raise ValueError(f'{name} is outside this release scope')
    return state['repositories'][name]


def default_source_ref(repository, version):
    """Resolve a repository source policy without falling back to checkout HEAD."""

    policy = repository.get('source_policy', {})
    if not policy:
        template = 'origin/release/{base}'
    else:
        channel = 'stable' if version.kind == 'stable' else 'prerelease'
        template = policy.get(channel)
        if not template:
            raise ValueError(f"{repository['name']}: source policy has no {channel} source")
    return template.format(base=version.base, version=version.version, kind=version.kind)


def compatible_profile(existing, current):
    """Allow additive repository/profile evolution when resuming old checkpoints."""

    if not isinstance(existing, dict) or not isinstance(current, dict):
        return False
    old_repositories = {item['name']: item for item in existing.get('repositories', []) if isinstance(item, dict) and item.get('name')}
    new_repositories = {item['name']: item for item in current.get('repositories', []) if isinstance(item, dict) and item.get('name')}
    if any(new_repositories.get(name) != item for name, item in old_repositories.items()):
        return False
    # New top-level sections such as a required artifact inventory are additive.
    # Existing policy must still match exactly when a checkpoint is resumed.
    old_rest = {key: value for key, value in existing.items() if key != 'repositories'}
    new_rest = {key: value for key, value in current.items() if key != 'repositories'}
    return all(new_rest.get(key) == value for key, value in old_rest.items())


def configured_container_release(profile):
    value = profile.get('container_release')
    if not isinstance(value, dict):
        raise ValueError('Profile has no configured container release inventory')
    if not isinstance(value.get('images'), list) or not value['images']:
        raise ValueError('Container release profile requires a non-empty image inventory')
    names = [image.get('name') for image in value['images'] if isinstance(image, dict)]
    if len(names) != len(value['images']) or any(not name for name in names) or len(set(names)) != len(names):
        raise ValueError('Container image names must be present and unique')
    repositories = [image.get('repository') for image in value['images']]
    if any(not isinstance(repository, str) or '/' not in repository for repository in repositories):
        raise ValueError('Container image repositories must use namespace/name form')
    if len(set(zip(repositories, (image.get('tag') for image in value['images'])))) != len(value['images']):
        raise ValueError('Container image repository and tag pairs must be unique')
    if not isinstance(value.get('repository'), str) or not isinstance(value.get('workflow'), str) or not value.get('workflow'):
        raise ValueError('Container release profile requires an Apps repository and workflow path')
    if not isinstance(value.get('source_ref'), str) or not value['source_ref']:
        raise ValueError('Container release profile requires an Apps source ref')
    package_ids = value.get('package_ids', {})
    probes = value.get('package_probe_ids', {})
    if not isinstance(package_ids, dict) or not isinstance(probes, dict):
        raise ValueError('Container profile requires package IDs for dependency checks')
    if any(not isinstance(ids, list) or not ids for ids in package_ids.values()) or any(not isinstance(ids, list) or not ids for ids in probes.values()):
        raise ValueError('Container package family IDs must be non-empty lists')
    known_names = set(names)
    for image in value['images']:
        if image.get('tag') != '{version}':
            raise ValueError('Container image tags must use the exact release version')
        if not image.get('release_repositories') or not set(image['release_repositories']) <= {r['name'] for r in profile['repositories']}:
            raise ValueError(f"Container image {image['name']} has an invalid release scope")
        if not isinstance(image.get('packages'), list) or not image['packages'] or not set(image['packages']) <= set(package_ids):
            raise ValueError(f"Container image {image['name']} has invalid package-version requirements")
        if not isinstance(image.get('platforms'), list) or not image['platforms'] or len(set(image['platforms'])) != len(image['platforms']):
            raise ValueError(f"Container image {image['name']} requires unique platform declarations")
        if image.get('alias_of') and image['alias_of'] not in known_names:
            raise ValueError(f"Container image {image['name']} aliases an unknown image")
    return value


def container_image_scope(profile, selected_repositories=None):
    inventory = configured_container_release(profile)
    if selected_repositories is None:
        return [image['name'] for image in inventory['images']]
    selected = set(selected_repositories)
    return [
        image['name'] for image in inventory['images']
        if selected.intersection(image.get('release_repositories', []))
    ]


def expand_container_image_selection(profile, names):
    inventory = configured_container_release(profile)
    by_name = {image['name']: image for image in inventory['images']}
    if names == ['all']:
        return [image['name'] for image in inventory['images']]
    unknown = set(names) - set(by_name)
    if unknown:
        raise ValueError(f'Unknown configured container image name(s): {", ".join(sorted(unknown))}')
    selected = set(names)
    for name in tuple(selected):
        alias = by_name[name].get('alias_of')
        if alias:
            selected.add(alias)
    for image in inventory['images']:
        if image.get('alias_of') in selected:
            selected.add(image['name'])
    return [image['name'] for image in inventory['images'] if image['name'] in selected]


def make_container_state(profile, version, selected_repositories=None, no_containers=False, available_repositories=None):
    if no_containers:
        return {
            'enabled': False,
            'images': [],
            'reason': 'explicitly disabled by the release request',
            'binding': None,
            'dispatch': None,
            'receipt': None,
            'verification': None,
        }
    inventory = configured_container_release(profile)
    selected_images = container_image_scope(profile, selected_repositories)
    if not selected_images:
        return {
            'enabled': False,
            'images': [],
            'reason': 'no configured container images apply to the selected repositories',
            'binding': None,
            'dispatch': None,
            'receipt': None,
            'verification': None,
        }
    packages = {package for image in inventory['images'] if image['name'] in selected_images for package in image.get('packages', [])}
    available_repositories = set(available_repositories or [])
    package_versions = {package: version if package in available_repositories else None for package in sorted(packages)}
    return {
        'enabled': True,
        'images': selected_images,
        'reason': None,
        'binding': None,
        'dispatch': None,
        'receipt': None,
        'verification': None,
        'source_ref': inventory['source_ref'],
        'version': version,
        'package_versions': package_versions,
    }


def containers_configured(state):
    value = state.get('containers')
    return isinstance(value, dict) and isinstance(value.get('enabled'), bool) and isinstance(value.get('images'), list)


def dockerhub_manifest(repository, tag):
    """Fetch a public Docker Hub manifest and its platform descriptors."""

    if repository.count('/') != 1:
        raise ValueError(f'Container repository must use namespace/name form: {repository}')
    scope = quote(f'repository:{repository}:pull', safe=':')
    token_url = f'https://auth.docker.io/token?service=registry.docker.io&scope={scope}'
    try:
        with urllib.request.urlopen(token_url, timeout=20) as response:
            token_payload = json.loads(response.read())
        token = token_payload.get('token') or token_payload.get('access_token')
        if not token:
            raise ValueError('Docker Hub did not return a pull token')
        manifest_url = f'https://registry-1.docker.io/v2/{quote(repository, safe="/")}/manifests/{quote(tag, safe="")}'
        request = urllib.request.Request(manifest_url, headers={
            'Authorization': f'Bearer {token}',
            'Accept': ', '.join([
                'application/vnd.oci.image.index.v1+json',
                'application/vnd.docker.distribution.manifest.list.v2+json',
                'application/vnd.oci.image.manifest.v1+json',
                'application/vnd.docker.distribution.manifest.v2+json',
            ]),
        })
        with urllib.request.urlopen(request, timeout=30) as response:
            content_digest = response.headers.get('Docker-Content-Digest')
            payload = json.loads(response.read())
    except urllib.error.HTTPError as exc:
        raise ValueError(f'Docker Hub registry lookup failed for {repository}:{tag}: HTTP {exc.code}') from exc
    except (urllib.error.URLError, TimeoutError, json.JSONDecodeError) as exc:
        raise ValueError(f'Docker Hub registry lookup failed for {repository}:{tag}: {exc}') from exc
    if not isinstance(content_digest, str) or not re.fullmatch(r'sha256:[0-9a-f]{64}', content_digest):
        raise ValueError(f'Docker Hub returned no valid manifest digest for {repository}:{tag}')
    descriptors = payload.get('manifests')
    if not isinstance(descriptors, list):
        raise ValueError(f'{repository}:{tag} is not a multi-platform image index')
    platforms = {}
    for descriptor in descriptors:
        platform = descriptor.get('platform') or {}
        os_name = platform.get('os')
        architecture = platform.get('architecture')
        if os_name != 'linux' or not architecture:
            continue
        name = f'{os_name}/{architecture}'
        descriptor_digest = descriptor.get('digest')
        if not re.fullmatch(r'sha256:[0-9a-f]{64}', str(descriptor_digest)):
            raise ValueError(f'{repository}:{tag} has a platform without a valid digest')
        if name in platforms:
            raise ValueError(f'{repository}:{tag} contains duplicate platform {name}')
        platforms[name] = descriptor_digest
    if not platforms:
        raise ValueError(f'{repository}:{tag} has no Linux platform manifests')
    return {'digest': content_digest, 'platforms': platforms}


def normalized_branch(ref):
    value = ref.removeprefix('origin/')
    if value.startswith('refs/heads/'):
        return value.removeprefix('refs/heads/')
    if value.startswith('refs/tags/'):
        return value.removeprefix('refs/tags/')
    return value


def container_artifact_name(version, run_id, run_attempt):
    return f'container-release-receipt-{version}-{run_id}-{run_attempt}'


def container_artifact_metadata(repository, run_id, expected_name):
    pages = gh('api', '--paginate', '--slurp', f'repos/{repository}/actions/runs/{run_id}/artifacts?per_page=100')
    artifacts = [artifact for page in pages for artifact in page.get('artifacts', []) if artifact.get('name') == expected_name]
    if len(artifacts) != 1:
        raise ValueError('Successful Apps run must have exactly one current container receipt artifact')
    artifact = artifacts[0]
    if artifact.get('expired') is True or not isinstance(artifact.get('id'), int):
        raise ValueError('Container receipt artifact is expired or has no live id')
    if not isinstance(artifact.get('digest'), str) or not re.fullmatch(r'sha256:[0-9a-f]{64}', artifact['digest']):
        raise ValueError('Container receipt artifact has no valid GitHub digest')
    return artifact


def read_container_artifact_archive(path, expected_digest, receipt_path):
    path = Path(path)
    if not path.is_file():
        raise ValueError('Container receipt artifact archive does not exist')
    if 'sha256:' + digest(path) != expected_digest:
        raise ValueError('Downloaded container receipt artifact digest differs from GitHub')
    try:
        with zipfile.ZipFile(path) as archive:
            names = [name for name in archive.namelist() if name.rstrip('/') == 'container-release-receipt.json']
            if len(names) != 1:
                raise ValueError('Container receipt artifact must contain exactly one container-release-receipt.json')
            info = archive.getinfo(names[0])
            if info.file_size <= 0 or info.file_size > 4 * 1024 * 1024:
                raise ValueError('Container receipt artifact has an invalid receipt size')
            if sum(item.file_size for item in archive.infolist()) > 16 * 1024 * 1024:
                raise ValueError('Container receipt artifact is too large')
            payload = archive.read(names[0])
            embedded = json.loads(payload)
    except (OSError, zipfile.BadZipFile, KeyError, json.JSONDecodeError) as error:
        raise ValueError(f'Cannot read container receipt artifact: {error}') from error
    if not isinstance(embedded, dict):
        raise ValueError('Container artifact receipt must be a JSON object')
    if Path(receipt_path).read_bytes() != payload:
        raise ValueError('Supplied container receipt differs from the exact successful-run artifact')
    return embedded


def package_feed_url(base_url, package_id, version):
    base = base_url.rstrip('/')
    package = quote(package_id.lower(), safe='')
    encoded_version = quote(version.lower(), safe='')
    filename = quote(f'{package_id.lower()}.{version.lower()}.nupkg', safe='')
    return f'{base}/{package}/{encoded_version}/{filename}'


def package_feed_available(url):
    request = urllib.request.Request(url, method='HEAD')
    try:
        with urllib.request.urlopen(request, timeout=20):
            return True
    except urllib.error.HTTPError as exc:
        if exc.code not in (405, 501):
            return False
    try:
        request = urllib.request.Request(url, method='GET', headers={'Range': 'bytes=0-0'})
        with urllib.request.urlopen(request, timeout=20) as response:
            response.read(1)
            return 200 <= response.status < 300
    except (urllib.error.HTTPError, urllib.error.URLError, TimeoutError):
        return False


def verify_container_package_feeds(profile, version, resolved_packages):
    version_info = parse_version(version)
    feed_names = profile['release_kinds'][version_info.kind]['feeds']
    feeds = {feed['name']: feed for feed in profile['feeds']}
    results = []
    for family, packages in sorted(resolved_packages.items()):
        for package in packages:
            package_id = package['id']
            package_version = package['version']
            for name in feed_names:
                feed = feeds.get(name)
                if not feed:
                    raise ValueError(f'Profile has no configured {name} package feed')
                url = package_feed_url(feed['base_url'], package_id, package_version)
                if not package_feed_available(url):
                    raise ValueError(f'Container dependency package is unavailable from {name}: {package_id} {package_version}')
                results.append({'family': family, 'id': package_id, 'version': package_version, 'feed': name, 'url': url})
    return results


def validate_container_receipt(profile, version, receipt, image_names=None, binding=None, artifact_archive=None, receipt_path=None, expected_package_versions=None):
    """Validate Apps workflow provenance, version tags, smoke evidence, and live registry state."""

    inventory = configured_container_release(profile)
    version_info = parse_version(version)
    if not isinstance(receipt, dict):
        raise ValueError('Container receipt must be a JSON object')
    if receipt.get('schemaVersion') != 1:
        raise ValueError('Container receipt schemaVersion must be 1')
    if receipt.get('releaseVersion') != version_info.version:
        raise ValueError('Container receipt releaseVersion does not match the release')
    if receipt.get('appsRepository') != inventory['repository']:
        raise ValueError('Container receipt Apps repository does not match the profile')
    if receipt.get('publication') != 'published':
        raise ValueError('Container receipt must prove published images, not build-only output')

    source = receipt.get('appsSource')
    source_commit = receipt.get('appsSourceCommit')
    if not isinstance(source, dict) or not isinstance(source.get('ref'), str) or not source.get('ref'):
        raise ValueError('Container receipt requires appsSource.ref')
    if not isinstance(source_commit, str) or not re.fullmatch(r'[0-9a-f]{40}', source_commit) or source.get('commit') != source_commit:
        raise ValueError('Container receipt Apps source commit is invalid or inconsistent')
    workflow = receipt.get('workflowRun')
    if not isinstance(workflow, dict):
        raise ValueError('Container receipt requires workflowRun provenance')
    run_id = container_workflow_run_id(workflow.get('id'))
    run_attempt = positive_int(workflow.get('runAttempt'), 'workflowRun.runAttempt')
    expected_workflow = inventory['workflow']
    event = workflow.get('event')
    if workflow.get('workflow') not in {expected_workflow, Path(expected_workflow).name} or event not in {'workflow_dispatch', 'release'} or workflow.get('headSha') != source_commit or workflow.get('conclusion') != 'success':
        raise ValueError('Container receipt workflowRun does not match the successful Apps source build')
    expected_run_url = f"https://github.com/{inventory['repository']}/actions/runs/{run_id}"
    if workflow.get('url') != expected_run_url or workflow.get('repository') != inventory['repository']:
        raise ValueError('Container receipt workflowRun URL does not match its repository and run id')
    live_run = gh('api', f"repos/{inventory['repository']}/actions/runs/{run_id}")
    workflow_ref = workflow.get('ref')
    if not isinstance(workflow_ref, str) or normalized_branch(workflow_ref) != normalized_branch(source['ref']):
        raise ValueError('Container receipt workflow ref does not match appsSource.ref')
    if event == 'release':
        if normalized_branch(source['ref']) != version_info.version or not source['ref'].startswith('refs/tags/'):
            raise ValueError('Apps release-event images must be built from the exact package-version tag')
    elif binding and normalized_branch(source['ref']) != normalized_branch(binding['source_ref']):
        raise ValueError('Container receipt Apps source ref differs from the release binding')
    expected_ref = binding['source_ref'] if binding else source['ref']
    if binding and source_commit != binding['commit']:
        raise ValueError('Container receipt Apps source commit differs from the release binding')
    expected_branch = normalized_branch(expected_ref)
    if (
        live_run.get('id') != run_id
        or live_run.get('run_attempt') != run_attempt
        or live_run.get('html_url') != workflow['url']
        or live_run.get('path') != expected_workflow
        or live_run.get('event') != event
        or live_run.get('head_sha') != source_commit
        or normalized_branch(live_run.get('head_branch', '')) != (version_info.version if event == 'release' else expected_branch)
        or live_run.get('status') != 'completed'
        or live_run.get('conclusion') != 'success'
        or live_run.get('repository', {}).get('full_name') != inventory['repository']
    ):
        raise ValueError('Live Apps workflow run does not match the receipt source, workflow, or successful conclusion')
    canonical_ref = inventory.get('canonical_ref', 'main')
    comparison = gh('api', f"repos/{inventory['repository']}/compare/{source_commit}...{quote(canonical_ref, safe='')}")
    if comparison.get('status') not in {'ahead', 'identical'}:
        raise ValueError('Apps source commit is not in the canonical main branch history')
    artifact_name = container_artifact_name(version_info.version, run_id, run_attempt)
    artifact = container_artifact_metadata(inventory['repository'], run_id, artifact_name)
    if artifact_archive is None or receipt_path is None:
        raise ValueError('Container receipt verification requires the exact GitHub workflow artifact archive')
    read_container_artifact_archive(artifact_archive, artifact['digest'], receipt_path)

    expected_images = [image for image in inventory['images'] if image['name'] in (image_names or [item['name'] for item in inventory['images']])]
    if image_names is not None and {image['name'] for image in expected_images} != set(image_names):
        raise ValueError('Container checkpoint names an image absent from the configured inventory')
    if not expected_images:
        raise ValueError('Container receipt has no configured images to verify')
    expected_rows = {
        image['name']: (image['repository'], image['tag'].format(version=version_info.version))
        for image in expected_images
    }
    rows = receipt.get('images')
    if not isinstance(rows, list) or len(rows) != len(expected_rows):
        raise ValueError('Container receipt image inventory does not match the release profile')
    by_name = {}
    for row in rows:
        if not isinstance(row, dict) or not isinstance(row.get('name'), str) or row['name'] in by_name:
            raise ValueError('Container receipt images must have unique configured names')
        by_name[row['name']] = row
    if set(by_name) != set(expected_rows):
        raise ValueError('Container receipt image inventory does not match the release profile')

    package_versions = receipt.get('packageVersions')
    workflow_inputs = receipt.get('workflowInputs', {})
    if not isinstance(package_versions, dict) or not isinstance(workflow_inputs, dict):
        raise ValueError('Container receipt requires packageVersions and workflowInputs')
    family_inputs = inventory['workflow_inputs']
    required_packages = {package for image in expected_images for package in image.get('packages', [])}
    expected_all_versions = {}
    for family in ('core', 'studio', 'extensions'):
        input_version = package_versions.get(family)
        if not isinstance(input_version, str):
            raise ValueError(f'Container receipt requires the {family} package version')
        expected_all_versions[family] = parse_version(input_version).version
        if event == 'workflow_dispatch' and family in required_packages:
            family_input = family_inputs[family]
            if workflow_inputs.get(family_input) != expected_all_versions[family]:
                raise ValueError(f'Container receipt package version for {family} differs from its workflow input')
    if set(package_versions) != set(expected_all_versions):
        raise ValueError('Container receipt packageVersions must cover core, studio, and extensions inputs')
    canonical_images = [image['name'] for image in expected_images if not image.get('alias_of')]
    all_canonical_images = [image['name'] for image in inventory['images'] if not image.get('alias_of')]
    if event == 'workflow_dispatch':
        required_inputs = {'version', 'publish', 'images', 'expected_commit'}
        if not required_inputs <= set(family_inputs):
            raise ValueError('Container profile must configure all workflow-dispatch input names')
        if workflow_inputs.get(family_inputs['version']) != version_info.version:
            raise ValueError('Container workflow version input does not match the release')
        if workflow_inputs.get(family_inputs['expected_commit']) != source_commit:
            raise ValueError('Container workflow expected_commit input does not match Apps source SHA')
        if workflow_inputs.get(family_inputs['publish']) not in (True, 'true'):
            raise ValueError('Container workflow must have publication enabled')
        image_input = workflow_inputs.get(family_inputs['images'])
        allowed_image_inputs = {','.join(canonical_images)}
        if canonical_images == all_canonical_images:
            allowed_image_inputs.add('all')
        if image_input not in allowed_image_inputs:
            raise ValueError('Container workflow image selection does not match the release scope')
    expected_family_versions = {package: version_info.version for package in required_packages}
    if binding:
        expected_family_versions.update(binding.get('packages', {}))
    if expected_package_versions:
        expected_family_versions.update(expected_package_versions)
    for package in required_packages:
        expected_version = expected_family_versions.get(package)
        if not expected_version:
            raise ValueError(f'Bind an explicit published {package} package version before container dispatch')
        if package_versions.get(package) != expected_version:
            raise ValueError(f'Container receipt package version for {package} does not match the release binding')
    resolved_packages = receipt.get('resolvedPackages')
    if not isinstance(resolved_packages, dict) or set(resolved_packages) != required_packages:
        raise ValueError('Container receipt requires resolved package assets for every selected package family')
    allowed_package_ids = inventory.get('package_ids', {})
    receipt_assets = {family: set() for family in required_packages}
    for family in required_packages:
        assets = resolved_packages.get(family)
        if not isinstance(assets, list) or not assets:
            raise ValueError(f'Container receipt has no resolved package assets for {family}')
        allowed_ids = {package_id.lower() for package_id in allowed_package_ids.get(family, [])}
        seen_ids = set()
        for asset in assets:
            if not isinstance(asset, dict) or not isinstance(asset.get('id'), str) or not isinstance(asset.get('version'), str):
                raise ValueError(f'Container receipt has malformed resolved package assets for {family}')
            package_id = asset['id']
            package_version = asset['version']
            if package_id.lower() in seen_ids:
                raise ValueError(f'Container receipt has duplicate resolved package id in {family}: {package_id}')
            seen_ids.add(package_id.lower())
            if package_id.lower() not in allowed_ids or package_version != expected_family_versions[family]:
                raise ValueError(f'Container image resolved a wrong package version for {family}: {package_id} {package_version}')
            receipt_assets[family].add((package_id.lower(), package_version))
    image_assets = {family: set() for family in required_packages}

    def validate_image_packages(image, row):
        expected_families = set(image.get('packages', []))
        image_versions = row.get('packageVersions')
        image_packages = row.get('resolvedPackages')
        if not isinstance(image_versions, dict) or set(image_versions) != expected_families:
            raise ValueError(f"Container receipt packageVersions for {image['name']} do not match its dependencies")
        if not isinstance(image_packages, dict) or set(image_packages) != expected_families:
            raise ValueError(f"Container receipt resolvedPackages for {image['name']} do not match its dependencies")
        for family in expected_families:
            if image_versions[family] != expected_family_versions[family]:
                raise ValueError(f"Container image {image['name']} used the wrong {family} package version")
            row_assets = image_packages[family]
            union = {(item['id'].lower(), item['version']) for item in row_assets if isinstance(item, dict) and isinstance(item.get('id'), str) and isinstance(item.get('version'), str)} if isinstance(row_assets, list) else set()
            if not isinstance(row_assets, list) or not row_assets or len(union) != len(row_assets):
                raise ValueError(f"Container receipt has malformed or duplicate resolved packages for {image['name']} ({family})")
            allowed_ids = {package_id.lower() for package_id in allowed_package_ids.get(family, [])}
            if any(package_id not in allowed_ids or package_version != expected_family_versions[family] for package_id, package_version in union):
                raise ValueError(f"Container image {image['name']} resolved an unconfigured {family} package")
            if image.get('alias_of'):
                source_row = by_name[image['alias_of']]
                source_assets = source_row.get('resolvedPackages', {}).get(family, [])
                source_set = {(item['id'].lower(), item['version']) for item in source_assets if isinstance(item, dict) and isinstance(item.get('id'), str) and isinstance(item.get('version'), str)}
                if union != source_set:
                    raise ValueError(f"Container alias {image['name']} has different {family} packages from its source image")
            else:
                image_assets[family].update(union)

    registry_at = receipt.get('registryVerifiedAt')
    if not isinstance(registry_at, str):
        raise ValueError('Container receipt requires registryVerifiedAt')
    try:
        registry_time = datetime.fromisoformat(registry_at.replace('Z', '+00:00'))
    except ValueError as exc:
        raise ValueError('Container receipt registryVerifiedAt must be ISO-8601') from exc
    if registry_time.tzinfo is None or registry_time > datetime.now(timezone.utc):
        raise ValueError('Container receipt registryVerifiedAt must be a timezone-bearing past timestamp')

    smoke = receipt.get('smoke')
    if not isinstance(smoke, dict) or smoke.get('success') is not True or not isinstance(smoke.get('results'), list) or not smoke['results'] or any(not isinstance(item, dict) or item.get('success') is not True for item in smoke['results']):
        raise ValueError('Container receipt requires successful smoke-test results')
    live_images = {}
    for image in expected_images:
        name = image['name']
        repository, tag = expected_rows[name]
        row = by_name[name]
        validate_image_packages(image, row)
        if row.get('repository') != repository or row.get('tag') != tag:
            raise ValueError(f'Container receipt tag for {name} does not match the configured version alias')
        source_ref = row.get('sourceRef')
        source_image = next((candidate for candidate in inventory['images'] if candidate['name'] == image.get('alias_of')), image)
        allowed_source_refs = {
            f"{source_image['repository']}:{version_info.version}",
            f"{source_image['repository']}:{version_info.version}-sha-{source_commit}",
        }
        if source_ref not in allowed_source_refs:
            raise ValueError(f'Container image {name} sourceRef does not bind its exact version or Apps commit')
        if row.get('registryVerified') is not True:
            raise ValueError(f'Container receipt does not report a live registry check for {name}')
        image_digest = row.get('digest')
        if not isinstance(image_digest, str) or not re.fullmatch(r'sha256:[0-9a-f]{64}', image_digest):
            raise ValueError(f'Container receipt has no valid registry digest for {name}')
        expected_platforms = set(image.get('platforms', []))
        platform_rows = row.get('platforms')
        if not isinstance(platform_rows, list):
            raise ValueError(f'Container receipt requires platform evidence for {name}')
        platform_digests = {}
        for platform_row in platform_rows:
            if not isinstance(platform_row, dict) or not isinstance(platform_row.get('platform'), str):
                raise ValueError(f'Container receipt has malformed platform evidence for {name}')
            platform_name = platform_row['platform']
            platform_digest = platform_row.get('digest')
            if platform_name in platform_digests or not isinstance(platform_digest, str) or not re.fullmatch(r'sha256:[0-9a-f]{64}', platform_digest):
                raise ValueError(f'Container receipt has duplicate or invalid platform evidence for {name}')
            platform_digests[platform_name] = platform_digest
        if set(platform_digests) != expected_platforms:
            raise ValueError(f'Container receipt platforms for {name} do not match the configured inventory')
        image_smoke = row.get('smoke')
        if not isinstance(image_smoke, dict) or image_smoke.get('success') is not True or image_smoke.get('imageDigest') != image_digest:
            raise ValueError(f'Container receipt has no successful smoke check bound to {name} digest')
        smoke_platforms = image_smoke.get('platforms')
        if not isinstance(smoke_platforms, list) or len(smoke_platforms) != len(expected_platforms) or {item.get('platform') for item in smoke_platforms if isinstance(item, dict)} != expected_platforms:
            raise ValueError(f'Container receipt smoke checks do not cover every platform for {name}')
        if any(
            item.get('status') not in ('success', 'passed')
            or isinstance(item.get('httpStatus'), bool)
            or not isinstance(item.get('httpStatus'), int)
            or not 200 <= item['httpStatus'] < 400
            or item.get('imageDigest') != platform_digests.get(item.get('platform'))
            for item in smoke_platforms
        ):
            raise ValueError(f'Container receipt contains a failed platform smoke check for {name}')

        live = dockerhub_manifest(repository, tag)
        if live['digest'] != image_digest or live['platforms'] != platform_digests:
            raise ValueError(f'Live Docker Hub manifest for {repository}:{tag} differs from the receipt')
        if set(live['platforms']) != expected_platforms:
            raise ValueError(f'Live Docker Hub platforms for {repository}:{tag} do not match the configured inventory')
        live_images[name] = {'repository': repository, 'tag': tag, **live}

    if any(receipt_assets[family] != image_assets[family] for family in required_packages):
        raise ValueError('Top-level resolvedPackages do not match the selected image project.assets evidence')

    for image in expected_images:
        alias = image.get('alias_of')
        if alias and live_images[image['name']]['digest'] != live_images[alias]['digest']:
            raise ValueError(f'Container alias {image["name"]} does not resolve to the configured source image digest')
    feed_evidence = verify_container_package_feeds(profile, version_info.version, resolved_packages)
    return {
        'verified': True,
        'version': version_info.version,
        'source_commit': source_commit,
        'apps_source_ref': source['ref'],
        'workflow_run_id': run_id,
        'workflow_attempt': run_attempt,
        'images': live_images,
        'package_feeds': feed_evidence,
        'receipt_sha256': digest(receipt_path),
        'artifact': {
            'id': artifact['id'],
            'name': artifact_name,
            'digest': artifact['digest'],
            'archive_sha256': digest(artifact_archive),
        },
        'verified_at': datetime.now(timezone.utc).isoformat(),
    }


def container_receipt_valid(state):
    containers = state.get('containers', {})
    receipt_record = containers.get('receipt')
    verification = containers.get('verification')
    binding = containers.get('binding')
    if not containers.get('enabled'):
        return True
    if not isinstance(receipt_record, dict) or not isinstance(verification, dict) or not isinstance(binding, dict):
        return False
    try:
        receipt_path = receipt_record['path']
        report_path = verification['report']
        if not Path(receipt_path).is_file() or digest(receipt_path) != receipt_record['sha256']:
            return False
        archive_path = receipt_record['artifact_archive']
        if not Path(archive_path).is_file() or digest(archive_path) != receipt_record['artifact_archive_sha256']:
            return False
        if not Path(report_path).is_file() or digest(report_path) != verification['sha256']:
            return False
        report = read(report_path)
        if report.get('verified') is not True or report.get('version') != state['version']:
            return False
        if report.get('source_commit') != binding.get('commit'):
            return False
        if report.get('receipt_sha256') != receipt_record.get('sha256'):
            return False
        artifact = report.get('artifact')
        if not isinstance(artifact, dict) or artifact.get('id') != receipt_record.get('artifact_id') or artifact.get('digest') != receipt_record.get('artifact_digest'):
            return False
        if artifact.get('archive_sha256') != receipt_record.get('artifact_archive_sha256'):
            return False
        if report.get('workflow_run_id') != receipt_record.get('run_id'):
            return False
        verified_at = datetime.fromisoformat(report['verified_at'].replace('Z', '+00:00'))
        age = datetime.now(timezone.utc) - verified_at
        if verified_at.tzinfo is None or age < timedelta(0) or age > timedelta(hours=24):
            return False
        return True
    except (OSError, ValueError, KeyError, TypeError):
        return False


def container_plan(state):
    containers = state.get('containers', {})
    inventory = configured_container_release(state['profile'])
    selected_names = containers.get('images', [])
    selected = [image for image in inventory['images'] if image['name'] in selected_names]
    if len(selected) != len(selected_names):
        raise ValueError('Container checkpoint contains an image absent from its frozen profile')
    packages = sorted({package for image in selected for package in image.get('packages', [])})
    return {
        'version': state['version'],
        'enabled': containers.get('enabled') is True,
        'reason': containers.get('reason'),
        'repository': inventory['repository'],
        'workflow': inventory['workflow'],
        'source_ref': containers.get('source_ref', inventory['source_ref']),
        'packages': {name: containers.get('package_versions', {}).get(name) for name in packages},
        'images': [
            {
                'name': image['name'],
                'repository': image['repository'],
                'tag': image['tag'].format(version=state['version']),
                'platforms': image['platforms'],
                'alias_of': image.get('alias_of'),
            }
            for image in selected
        ],
    }


def validate_container_source(inventory, source_ref, version, expected_commit=None):
    canonical_ref = inventory.get('canonical_ref', 'main')
    if source_ref in {canonical_ref, f'origin/{canonical_ref}', f'refs/heads/{canonical_ref}'}:
        pinned_ref = canonical_ref
    elif source_ref in {version, f'refs/tags/{version}'}:
        pinned_ref = version
    else:
        raise ValueError('Apps source ref must be the canonical branch or the exact release-version tag')

    ref = gh('api', f"repos/{inventory['repository']}/commits/{quote(pinned_ref, safe='')}")
    commit = ref.get('sha')
    if not isinstance(commit, str) or not re.fullmatch(r'[0-9a-f]{40}', commit):
        raise ValueError('Apps source ref did not resolve to a full commit SHA')
    if expected_commit and commit != expected_commit:
        raise ValueError('Apps source ref no longer points to the pinned commit')
    comparison = gh('api', f"repos/{inventory['repository']}/compare/{commit}...{quote(canonical_ref, safe='')}")
    if comparison.get('status') not in {'ahead', 'identical'}:
        raise ValueError('Apps source commit is not in the canonical main branch history')
    return {'source_ref': pinned_ref, 'commit': commit}


def require_container_upstreams(state):
    current = status(state)
    results = current.get('repositories', {})
    if not results or any(result.get('phase') != 'verified' for result in results.values()):
        raise ValueError(f"Container stage waits for verified release packages: {results}")


def prepare_containers(state, args):
    if not containers_configured(state):
        raise ValueError('Adopt the container phase before preparing an existing checkpoint')
    if not state['containers']['enabled']:
        raise ValueError('Container publication is disabled for this release scope')
    require_container_upstreams(state)
    return container_plan(state)


def bind_containers(state, args):
    if not isinstance(args.commit, str) or not re.fullmatch(r'[0-9a-f]{40}', args.commit):
        raise ValueError('The reviewed Apps commit must be a full 40-character SHA')
    plan = prepare_containers(state, args)
    inventory = configured_container_release(state['profile'])
    source = validate_container_source(
        inventory, args.source_ref or plan['source_ref'], state['version'], expected_commit=args.commit,
    )
    source_ref = source['source_ref']
    commit = source['commit']
    packages = dict(state['containers'].get('package_versions', {}))
    required_packages = set(plan['packages'])
    for item in args.package_version or []:
        if '=' not in item:
            raise ValueError('Package version overrides must use FAMILY=VERSION')
        family, package_version = item.split('=', 1)
        if family not in required_packages:
            raise ValueError(f'Package version override is outside this image scope: {family}')
        packages[family] = parse_version(package_version).version
    for family in required_packages:
        if family in state['repositories']:
            if packages.get(family) != state['version']:
                raise ValueError(f'{family} is part of this release train and must use {state["version"]}')
        elif not packages.get(family):
            raise ValueError(f'Container images require an explicit published {family} package version; pass --package-version {family}=<version>')
    package_ids = inventory.get('package_probe_ids', {})
    for family in required_packages - set(state['repositories']):
        ids = package_ids.get(family, [])
        if not ids:
            raise ValueError(f'Profile has no published package IDs for external {family} dependencies')
        version = packages[family]
        feeds = verify_container_package_feeds(state['profile'], version, {family: [{'id': package_id, 'version': version} for package_id in ids]})
        state['containers'].setdefault('package_feed_checks', {})[family] = feeds
    state['containers']['package_versions'] = packages
    binding = {
        'source_ref': source_ref.removeprefix('origin/'),
        'commit': commit,
        'packages': packages,
        'images': [image['name'] for image in plan['images']],
    }
    previous = state['containers'].get('binding')
    if previous and previous != binding:
        if not args.replace:
            raise ValueError('Existing Apps source binding differs; use --replace only before dispatch')
        if state['containers'].get('dispatch') or state['containers'].get('receipt'):
            raise ValueError('Cannot replace the Apps source after workflow dispatch or image publication')
    state['containers']['binding'] = binding
    state['containers']['source_ref'] = binding['source_ref']
    return binding


def container_run_by_id(inventory, run_id):
    return gh('api', f"repos/{inventory['repository']}/actions/runs/{run_id}")


def reconcile_container_dispatch(inventory, dispatch, binding):
    runs = gh('api', '--paginate', '--slurp', f"repos/{inventory['repository']}/actions/runs?event=workflow_dispatch&branch={quote(normalized_branch(binding['source_ref']), safe='')}&per_page=100")
    requested_at = datetime.fromisoformat(dispatch['requested_at'].replace('Z', '+00:00')) - timedelta(minutes=1)
    candidates = []
    for page in runs:
        for run in page.get('workflow_runs', []):
            try:
                created_at = datetime.fromisoformat((run.get('created_at') or '').replace('Z', '+00:00'))
            except ValueError:
                continue
            if (
                run.get('event') == 'workflow_dispatch'
                and run.get('path') == inventory['workflow']
                and run.get('head_sha') == binding['commit']
                and normalized_branch(run.get('head_branch', '')) == normalized_branch(binding['source_ref'])
                and created_at >= requested_at
            ):
                candidates.append(run)
    if len(candidates) > 1:
        raise ValueError('More than one Apps workflow run matches the pending dispatch; inspect Actions before proceeding')
    if not candidates:
        return None
    run = candidates[0]
    dispatch.update(run_id=run['id'], url=run['html_url'], run_attempt=run.get('run_attempt', 1))
    return run


def dispatch_containers(state, args):
    plan = prepare_containers(state, args)
    containers = state['containers']
    binding = containers.get('binding')
    if not isinstance(binding, dict):
        raise ValueError('Bind the reviewed Apps source before dispatching container publication')
    if containers.get('receipt'):
        raise ValueError('Container publication already has a recorded receipt')
    inventory = configured_container_release(state['profile'])
    dispatch = containers.get('dispatch')
    if dispatch:
        run_id = dispatch.get('run_id')
        if not run_id:
            run = reconcile_container_dispatch(inventory, dispatch, binding)
            if not run:
                return {'phase': 'dispatch-pending', 'source_commit': binding['commit']}
            return {'phase': 'wait-for-container-run', 'run_id': run['id'], 'url': run['html_url'], 'source_commit': binding['commit']}
        return {'phase': 'wait-for-container-run', 'run_id': run_id, 'source_commit': binding['commit']}
    source = validate_container_source(inventory, binding.get('source_ref'), state['version'], expected_commit=binding.get('commit'))
    workflow_inputs = inventory.get('workflow_inputs', {})
    if not {'version', 'publish', 'images', 'expected_commit'} <= set(workflow_inputs) or any(family not in workflow_inputs for family in binding['packages']):
        raise ValueError('Container workflow profile must configure version, publish, expected-commit, and image-selection inputs')
    submitted_at = datetime.now(timezone.utc).isoformat()
    dispatch = containers['dispatch'] = {'requested_at': submitted_at, 'source_commit': binding['commit']}
    save(args.state, state)
    fields = {
        workflow_inputs['version']: state['version'],
        workflow_inputs['publish']: 'true',
        workflow_inputs['images']: ','.join(image['name'] for image in plan['images'] if not image.get('alias_of')),
        workflow_inputs['expected_commit']: binding['commit'],
    }
    fields.update({workflow_inputs[family]: version for family, version in binding['packages'].items()})
    command_args = [
        'gh', 'workflow', 'run', inventory['workflow'], '--repo', inventory['repository'],
        '--ref', source['source_ref'],
    ]
    for name, value in fields.items():
        command_args.extend(['--field', f'{name}={value}'])
    try:
        command(command_args)
    except ValueError as error:
        # Only a clear client-side HTTP rejection proves GitHub did not accept
        # the dispatch. EOF, transport, and server errors remain ambiguous.
        if re.search(r'\b(?:HTTP(?:/\d(?:\.\d)?)?\s+|status(?: code)?[=: ]+)(?:400|401|403|404|405|410|422)\b', str(error), re.IGNORECASE):
            if containers.get('dispatch') is dispatch:
                containers['dispatch'] = None
                save(args.state, state)
        raise
    run = reconcile_container_dispatch(inventory, dispatch, binding)
    if not run:
        return {'phase': 'dispatch-pending', 'source_commit': binding['commit']}
    return {'phase': 'wait-for-container-run', 'run_id': run['id'], 'url': run['html_url'], 'source_commit': binding['commit']}


def record_containers(state, args):
    if not containers_configured(state) or not state['containers']['enabled']:
        raise ValueError('No container images are required by this release scope')
    observed = status(state)
    if observed['next'] not in ('containers', 'sites', 'announcements', 'complete'):
        raise ValueError('All selected packages must be verified before the container release')
    binding = state['containers'].get('binding')
    if not binding:
        raise ValueError('Bind the reviewed Apps source before recording its image receipt')
    dispatch = state['containers'].get('dispatch')
    receipt = read(args.receipt)
    receipt_run = receipt.get('workflowRun', {})
    if dispatch:
        if not dispatch.get('run_id'):
            raise ValueError('Reconcile the pending Apps workflow dispatch before recording a container receipt')
        if receipt_run.get('id') != dispatch['run_id']:
            raise ValueError('Container receipt workflow run differs from the release checkpoint dispatch')
    elif receipt_run.get('event') != 'release':
        raise ValueError('Dispatch and reconcile the Apps container workflow before recording its receipt')
    report = validate_container_receipt(
        state['profile'], state['version'], receipt,
        image_names=state['containers']['images'], binding=binding,
        artifact_archive=args.artifact_archive, receipt_path=args.receipt,
    )
    report_path = args.state.parent / f"containers-verification-{state['version']}.json"
    receipt_record = {
        'path': str(args.receipt.resolve()),
        'sha256': digest(args.receipt),
        'artifact_archive': str(args.artifact_archive.resolve()),
        'artifact_archive_sha256': digest(args.artifact_archive),
        'artifact_id': report['artifact']['id'],
        'artifact_digest': report['artifact']['digest'],
        'run_id': report['workflow_run_id'],
    }
    previous = state['containers'].get('receipt')
    if previous and previous != receipt_record:
        if not args.replace:
            raise ValueError('A different container receipt is already recorded; use --replace after fresh verification')
    save(report_path, report)
    verification_record = {'report': str(report_path), 'sha256': digest(report_path)}
    state['containers']['receipt'] = receipt_record
    state['containers']['verification'] = verification_record
    return {'phase': 'verified', **report, 'report': str(report_path)}


def verify_containers(args):
    version = parse_version(args.version).version
    profile = read(args.profile)
    image_names = expand_container_image_selection(profile, args.images) if args.images else container_image_scope(profile)
    expected_versions = {}
    for value in args.package_version or []:
        if '=' not in value:
            raise ValueError('Package version overrides must use FAMILY=VERSION')
        family, package_version = value.split('=', 1)
        if family not in {'core', 'studio', 'extensions'}:
            raise ValueError(f'Unknown package family in container verification: {family}')
        if family in expected_versions:
            raise ValueError(f'Duplicate package version override for {family}')
        expected_versions[family] = parse_version(package_version).version
    receipt = read(args.receipt)
    report = validate_container_receipt(
        profile, version, receipt, image_names=image_names,
        artifact_archive=args.artifact_archive, receipt_path=args.receipt,
        expected_package_versions=expected_versions,
    )
    if args.output:
        save(args.output, report)
    return report


def post_refresh_configured(state):
    """Return whether this checkpoint explicitly adopted the post-release gate."""

    value = state.get('post_refresh')
    return (
        isinstance(value, dict)
        and isinstance(value.get('receipts'), dict)
        and isinstance(value.get('targets'), list)
        and bool(value['targets'])
        and all(target in SITE_TARGETS for target in value['targets'])
        and 'enabled' in value
    )


def post_refresh_targets(state):
    return state['post_refresh']['targets'] if state['post_refresh']['enabled'] else []


def site_scope(state):
    """Return the selected repositories whose release content may be refreshed."""

    return sorted(name for name, item in state['repositories'].items() if item['publish'])


def site_config(state, target):
    try:
        return state['profile']['post_release_sites'][target]
    except (KeyError, TypeError):
        raise ValueError(f'Profile has no post-release configuration for {target}')


def parse_timestamp(value, field):
    if not isinstance(value, str) or not value.strip():
        raise ValueError(f'Site receipt requires {field}')
    try:
        parsed = datetime.fromisoformat(value.replace('Z', '+00:00'))
    except ValueError as exc:
        raise ValueError(f'Site receipt {field} must be an ISO-8601 timestamp') from exc
    if parsed.tzinfo is None:
        raise ValueError(f'Site receipt {field} must include a timezone')
    if parsed > datetime.now(timezone.utc):
        raise ValueError(f'Site receipt {field} cannot be in the future')
    return parsed


def same_origin(url, configured_urls):
    parsed = urlparse(url)
    if not parsed.scheme or not parsed.netloc:
        return False
    return any(
        parsed.scheme == configured.scheme
        and parsed.netloc == configured.netloc
        and (parsed.path == configured.path or parsed.path.startswith(configured.path.rstrip('/') + '/'))
        for configured in (urlparse(value) for value in configured_urls)
    )


def stable_version_key(value):
    parsed = parse_version(value)
    if parsed.kind != 'stable':
        raise ValueError(f'Latest stable version must be stable: {value}')
    return tuple(int(part) for part in parsed.base.split('.'))


def validate_site_receipt(state, target, receipt):
    """Validate live, resumable evidence for one post-release content target."""

    if target not in SITE_TARGETS:
        raise ValueError(f'Unknown post-release target: {target}')
    if not isinstance(receipt, dict):
        raise ValueError('Site receipt must be a JSON object')
    if receipt.get('target') != target:
        raise ValueError(f'Site receipt target must be {target}')
    if receipt.get('status') not in VALID_SITE_STATUSES:
        raise ValueError('Site receipt must describe completed production work, not a queue or draft')
    if receipt.get('version') != state['version']:
        raise ValueError('Site receipt version does not match the release')
    if receipt.get('scope') != site_scope(state):
        raise ValueError('Site receipt scope does not match the selected release repositories')

    changed_urls = receipt.get('changed_urls')
    configured_urls = site_config(state, target)['production_urls']
    if not isinstance(changed_urls, list) or not changed_urls or any(not isinstance(url, str) or not url.strip() or not same_origin(url, configured_urls) for url in changed_urls):
        raise ValueError('Site receipt requires non-empty changed_urls')
    deployment_or_commit = receipt.get('deployment_or_commit')
    if not isinstance(deployment_or_commit, str) or not deployment_or_commit.strip():
        raise ValueError('Site receipt requires deployment_or_commit')
    evidence_at = parse_timestamp(receipt.get('evidence_at'), 'evidence_at')

    production = receipt.get('production_verification')
    if not isinstance(production, dict) or production.get('verified') is not True:
        raise ValueError('Site receipt requires verified live production evidence')
    if production.get('version') != state['version']:
        raise ValueError('Live production version does not match the release')
    production_url = production.get('url')
    if production_url not in configured_urls:
        raise ValueError('Live production URL does not match the configured target')
    if parse_timestamp(production.get('evidence_at'), 'production_verification.evidence_at') != evidence_at:
        raise ValueError('Live production evidence timestamp differs from receipt evidence_at')

    if state['kind'] == 'stable':
        if receipt.get('content_label') != 'stable':
            raise ValueError('Stable site receipts must be labeled stable')
        updates_current = receipt.get('updates_current_stable')
        replaces_latest = receipt.get('replaces_latest_stable')
        if updates_current is True and replaces_latest is True:
            pass
        elif updates_current is False and replaces_latest is False:
            latest_version = receipt.get('latest_stable_version')
            latest_evidence = receipt.get('latest_stable_verification')
            if not isinstance(latest_version, str) or stable_version_key(latest_version) <= stable_version_key(state['version']):
                raise ValueError('Stable site receipts must identify a newer stable version when preserving latest guidance')
            if not isinstance(latest_evidence, dict) or latest_evidence.get('verified') is not True or latest_evidence.get('version') != latest_version:
                raise ValueError('Stable site receipts must verify the preserved newer stable guidance')
            if latest_evidence.get('url') not in configured_urls:
                raise ValueError('Preserved stable guidance URL does not match the configured target')
            parse_timestamp(latest_evidence.get('evidence_at'), 'latest_stable_verification.evidence_at')
        else:
            raise ValueError('Stable site receipts must update current guidance or verify a newer stable version')
    else:
        if receipt.get('updates_current_stable') is not False or receipt.get('replaces_latest_stable') is not False:
            raise ValueError('Prerelease site receipts must preserve latest stable guidance')
        if receipt.get('content_label') != state['kind']:
            raise ValueError(f"Prerelease site receipts must be labeled {state['kind']}")

    configured = site_config(state, target)
    if target == 'website':
        if receipt.get('project_id') != configured['project_id']:
            raise ValueError('Website receipt project_id does not match the Elsa Hub project')
        if receipt.get('workspace_name') != configured['workspace_name']:
            raise ValueError('Website receipt workspace_name does not match the configured Lovable workspace')
    else:
        if receipt.get('repository') != configured['repository'] or receipt.get('branch') != configured['branch']:
            raise ValueError('Documentation receipt target does not match the configured repository and branch')
    if not receipt.get('id'):
        raise ValueError('Site receipt requires a resumable operation/message id')
    return receipt


def site_receipt_valid(state, target, record):
    if not isinstance(record, dict) or not isinstance(record.get('receipt'), str) or not isinstance(record.get('sha256'), str):
        return False
    try:
        if not Path(record['receipt']).is_file() or digest(record['receipt']) != record['sha256']:
            return False
        validate_site_receipt(state, target, read(record['receipt']))
    except (OSError, ValueError, KeyError, TypeError):
        return False
    return True


def init(args):
    version = parse_version(args.version, args.kind)
    profile = read(args.profile)
    if profile.get("candidate_only"):
        raise ValueError("Nonpublishing candidate profiles cannot initialize a publication release train")
    names = [r['name'] for r in profile['repositories']]
    selected = args.repositories or names
    sources = dict(x.split('=', 1) for x in (args.source or []))
    if not set(sources) <= set(selected):
        raise ValueError('Source overrides must name a selected repository')
    for source in sources.values():
        try:
            source_version = parse_version(source.removesuffix('^{}').rsplit('/', 1)[-1])
        except ValueError:
            if re.match(r'^\d+\.\d+\.\d+(?:-|$)', source.rsplit('/', 1)[-1]):
                raise ValueError('Versioned source override is invalid or lacks a release number')
            continue
        if source_version.base != version.base:
            raise ValueError('Versioned source override belongs to a different release line')
    if not set(selected) <= set(names):
        raise ValueError('Unknown repository selection')
    needed = set(selected)
    for r in reversed(profile['repositories']):
        if r['name'] in needed:
            needed.update(r['dependencies'])
    state = {
        'schema': 2, 'version': version.version, 'kind': version.kind,
        'profile': profile, 'prerequisites': args.pr or [],
        'announce': not args.no_announcements, 'announcements': {},
        'containers': make_container_state(
            profile,
            version.version,
            selected if args.repositories is not None else None,
            getattr(args, 'no_containers', False),
            needed,
        ),
        'post_refresh': {
            'enabled': not getattr(args, 'no_post_refresh', False),
            'targets': list(SITE_TARGETS),
            'receipts': {},
        },
        'repositories': {r['name']: {
            'path': str((Path(args.repos_root).expanduser().resolve() / r['directory'])),
            'publish': r['name'] in selected,
            'source_ref': sources.get(r['name'], default_source_ref(r, version)),
        } for r in profile['repositories'] if r['name'] in needed},
    }
    if args.state.exists():
        existing = read(args.state)
        # Never erase a partially completed train by repeating init.
        for field in ['version', 'kind', 'prerequisites', 'announce']:
            if existing[field] != state[field]:
                raise ValueError(f'Existing state has a different {field}; use its recorded inputs')
        if existing.get('profile') != state['profile'] and not compatible_profile(existing.get('profile'), state['profile']):
            raise ValueError('Existing state has a different profile; use its recorded inputs')
        old_scope = {
            k: (v['publish'], v['path'], v['source_ref']) for k, v in existing['repositories'].items()
        }
        new_scope = {
            k: (v['publish'], v['path'], v['source_ref']) for k, v in state['repositories'].items()
        }
        if args.repositories is not None or args.source is not None:
            if old_scope != new_scope:
                raise ValueError('Existing state has different repository scope or paths')
        elif any(new_scope.get(name) != values for name, values in old_scope.items()):
            raise ValueError('Existing state has different repository paths or source refs')
        if post_refresh_configured(existing):
            existing_refresh = existing['post_refresh']
            requested_refresh = state['post_refresh']
            if (existing_refresh['enabled'], existing_refresh['targets']) != (requested_refresh['enabled'], requested_refresh['targets']):
                raise ValueError('Existing state has different post-refresh settings; use its recorded inputs')
        if containers_configured(existing):
            old_containers = existing['containers']
            requested_containers = state['containers']
            if (old_containers['enabled'], old_containers['images'], old_containers.get('reason')) != (
                requested_containers['enabled'], requested_containers['images'], requested_containers.get('reason')
            ):
                raise ValueError('Existing state has different container scope; use its recorded inputs')
        return existing
    save(args.state, state)
    return state


def check_prerequisites(state):
    for url in state['prerequisites']:
        pr = gh('pr', 'view', url, '--json', 'state,mergeCommit,url')
        if pr['state'] != 'MERGED':
            raise ValueError(f'Prerequisite is not merged: {url}')
        # Inclusion in the selected source is verified when binding that repository.


def published_release(state, name):
    cfg = config(state, name)
    releases = gh('api', '--paginate', '--slurp', f"repos/{cfg['github']}/releases?per_page=100")
    release = next((r for page in releases for r in page if r['tag_name'] == state['version']), None)
    if release is None:
        return None
    version = parse_version(state['version'], state['kind'])
    if release['draft'] or release['prerelease'] != version.prerelease:
        raise ValueError(f'{name}: existing release kind/draft mismatch')
    obj = gh('api', f"repos/{cfg['github']}/git/ref/tags/{state['version']}")['object']
    for _ in range(4):
        if obj['type'] == 'commit':
            return dict(release, commit=obj['sha'])
        if obj['type'] != 'tag':
            break
        obj = gh('api', f"repos/{cfg['github']}/git/tags/{obj['sha']}")['object']
    raise ValueError(f'{name}: tag does not resolve to a commit')


def positive_int(value, field):
    if isinstance(value, bool) or not isinstance(value, int) or value <= 0:
        raise ValueError(f'Recovery receipt requires positive integer {field}')
    return value


def container_workflow_run_id(value):
    if isinstance(value, str) and re.fullmatch(r'[1-9][0-9]*', value):
        value = int(value)
    if isinstance(value, bool) or not isinstance(value, int) or value <= 0:
        raise ValueError('Container receipt requires a positive integer or canonical decimal string workflowRun.id')
    return value


def recovery_job_names(cfg):
    names = [
        name
        for name in cfg.get('required_jobs', [])
        if isinstance(name, str) and name.lower().rsplit(' ', 1)[-1] == RECOVERY_REGISTRY
    ]
    if len(names) != 1:
        raise ValueError('Recovery requires exactly one configured nuget.org publishing job')
    return names[0]


def recovery_artifact_name(cfg):
    name = cfg.get('recovery_artifact')
    if not isinstance(name, str) or not name:
        raise ValueError('Recovery requires a configured machine-readable evidence artifact')
    return name


def commit_sha(value, field):
    if not isinstance(value, str) or not re.fullmatch(r'[0-9a-f]{40}', value):
        raise ValueError(f'Recovery receipt requires a lowercase 40-character SHA for {field}')
    return value


def workflow_jobs(cfg, run_id):
    pages = gh('api', '--paginate', '--slurp', f"repos/{cfg['github']}/actions/runs/{run_id}/jobs?filter=latest&per_page=100")
    return [job for page in pages for job in page.get('jobs', [])]


def workflow_artifacts(cfg, run_id):
    pages = gh('api', '--paginate', '--slurp', f"repos/{cfg['github']}/actions/runs/{run_id}/artifacts?per_page=100")
    return [artifact for page in pages for artifact in page.get('artifacts', [])]


def indexed_workflow_jobs(cfg, run_id, phase):
    indexed = {}
    for job in workflow_jobs(cfg, run_id):
        name = job.get('name')
        if not isinstance(name, str) or not name:
            raise ValueError(f'{phase} recovery run contains a job without a name')
        if name in indexed:
            raise ValueError(f'{phase} recovery run contains duplicate job {name!r}')
        indexed[name] = job
    return indexed


def validate_recovery_source(cfg, receipt, recovery_sha):
    approved_sha = commit_sha(receipt.get('approved_source_commit'), 'approved_source_commit')
    repository = gh('api', f"repos/{cfg['github']}")
    if repository.get('full_name') != cfg['github']:
        raise ValueError('Recovery source repository does not match the release profile')
    default_branch = repository.get('default_branch')
    if not isinstance(default_branch, str) or not default_branch:
        raise ValueError('Recovery source repository has no canonical default branch')
    comparison = gh('api', f"repos/{cfg['github']}/compare/{approved_sha}...{quote(default_branch, safe='')}")
    if comparison.get('status') not in {'ahead', 'identical'}:
        raise ValueError('Approved recovery source is not in the canonical default-branch history')
    approved_commit = gh('api', f"repos/{cfg['github']}/commits/{approved_sha}")
    recovery_commit = gh('api', f"repos/{cfg['github']}/commits/{recovery_sha}")
    if approved_commit.get('sha') != approved_sha or recovery_commit.get('sha') != recovery_sha:
        raise ValueError('Recovery source commit lookup returned a different SHA')
    approved_tree = approved_commit.get('commit', {}).get('tree', {}).get('sha')
    recovery_tree = recovery_commit.get('commit', {}).get('tree', {}).get('sha')
    if not approved_tree or not recovery_tree or approved_tree != recovery_tree:
        raise ValueError('Recovery workflow tree differs from the approved default-branch source tree')


def read_recovery_evidence_archive(path, expected_digest=None):
    path = Path(path)
    if not path.is_file():
        raise ValueError('Recovery evidence archive does not exist')
    actual_digest = 'sha256:' + digest(path)
    if expected_digest is not None and actual_digest != expected_digest:
        raise ValueError('Downloaded recovery evidence archive hash differs from its receipt')
    try:
        with zipfile.ZipFile(path) as archive:
            names = [name for name in archive.namelist() if name.rstrip('/') == 'recovery-receipt.json']
            if len(names) != 1:
                raise ValueError('Recovery evidence archive must contain exactly one recovery-receipt.json')
            info = archive.getinfo(names[0])
            if info.file_size <= 0 or info.file_size > 1024 * 1024:
                raise ValueError('Recovery evidence receipt has an invalid size')
            if sum(item.file_size for item in archive.infolist()) > 4 * 1024 * 1024:
                raise ValueError('Recovery evidence archive is too large')
            evidence = json.loads(archive.read(names[0]))
    except (OSError, zipfile.BadZipFile, KeyError, json.JSONDecodeError) as error:
        raise ValueError(f'Cannot read recovery evidence archive: {error}') from error
    if not isinstance(evidence, dict):
        raise ValueError('Recovery evidence receipt must be a JSON object')
    return evidence


def validate_recovery_evidence(state, name, receipt, evidence, original_id, artifact_id, artifact_digest, artifact_size, recovery_id, recovery_sha):
    cfg = config(state, name)
    if not isinstance(evidence, dict) or evidence.get('schema') != 1:
        raise ValueError('Recovery evidence must be a version 1 JSON object')
    if evidence.get('repository') != cfg['github'] or evidence.get('version') != state['version']:
        raise ValueError('Recovery evidence repository/version does not match the release')
    if evidence.get('recovery_run_id') != recovery_id or evidence.get('recovery_workflow_sha') != recovery_sha:
        raise ValueError('Recovery evidence does not identify the live recovery run/workflow source')
    if evidence.get('original_release_run_id') != original_id or evidence.get('original_source_commit') != entry(state, name)['binding']['commit']:
        raise ValueError('Recovery evidence is bound to a different original release run')
    original_artifact = evidence.get('original_artifact')
    if not isinstance(original_artifact, dict):
        raise ValueError('Recovery evidence requires original_artifact metadata')
    if (
        original_artifact.get('id') != artifact_id
        or original_artifact.get('name') != cfg['artifact']
        or original_artifact.get('run_id') != original_id
        or original_artifact.get('digest') != artifact_digest
        or original_artifact.get('size_in_bytes') != artifact_size
    ):
        raise ValueError('Recovery evidence does not bind the original artifact payload')
    target = evidence.get('target')
    if not isinstance(target, dict) or target.get('registry') != RECOVERY_REGISTRY or target.get('version') != state['version']:
        raise ValueError('Recovery evidence target must be the exact NuGet release/version')
    package_ids = target.get('package_ids')
    if not isinstance(package_ids, list) or any(not isinstance(package_id, str) for package_id in package_ids):
        raise ValueError('Recovery evidence target requires package_ids')
    expected_ids = config(state, name).get('expected_package_ids')
    if expected_ids is None:
        expected_ids = [package['id'] for package in read(entry(state, name)['binding']['manifest']).get('nuget', [])]
    if sorted(package_ids, key=str.lower) != sorted(expected_ids, key=str.lower):
        raise ValueError('Recovery evidence package inventory does not match the release profile')


def validate_recovery_receipt(state, name, receipt, expected_original_run_id=None, evidence=None):
    """Validate a no-rebuild NuGet recovery against the immutable failed release run."""

    item = entry(state, name)
    cfg = config(state, name)
    if not cfg.get('artifact'):
        raise ValueError(f'{name}: recovery requires a configured source workflow artifact')
    binding = item.get('binding')
    if not isinstance(binding, dict) or not binding.get('commit'):
        raise ValueError('Recovery requires a frozen source binding')
    if not isinstance(receipt, dict):
        raise ValueError('Recovery receipt must be a JSON object')
    if receipt.get('repository') != cfg['github']:
        raise ValueError('Recovery receipt repository does not match the release profile')
    if receipt.get('version') != state['version'] or receipt.get('tag') != state['version']:
        raise ValueError('Recovery receipt version/tag does not match the release')
    if receipt.get('source_commit') != binding['commit']:
        raise ValueError('Recovery receipt source commit does not match the frozen binding')

    target = receipt.get('target')
    if not isinstance(target, dict) or target.get('registry') != RECOVERY_REGISTRY or target.get('version') != state['version']:
        raise ValueError('Recovery receipt target must be the exact NuGet release/version')
    raw_package_ids = target.get('package_ids') if isinstance(target, dict) else None
    if not isinstance(raw_package_ids, list) or any(not isinstance(package_id, str) for package_id in raw_package_ids):
        raise ValueError('Recovery receipt target requires package_ids')
    package_ids = sorted(raw_package_ids, key=str.lower)
    expected_ids = cfg.get('expected_package_ids')
    if expected_ids is None:
        expected_ids = [package['id'] for package in read(binding['manifest']).get('nuget', [])]
    if package_ids != sorted(expected_ids, key=str.lower):
        raise ValueError('Recovery receipt package inventory does not match the release profile')

    original = receipt.get('original_release_run')
    artifact = receipt.get('artifact')
    recovery = receipt.get('recovery_run')
    if not isinstance(original, dict) or not isinstance(artifact, dict) or not isinstance(recovery, dict):
        raise ValueError('Recovery receipt requires original_release_run, artifact, and recovery_run objects')

    original_id = positive_int(original.get('id'), 'original_release_run.id')
    if expected_original_run_id is not None and original_id != expected_original_run_id:
        raise ValueError('Recovery receipt is bound to a different original release run')
    recovery_id = positive_int(recovery.get('id'), 'recovery_run.id')
    if original_id == recovery_id:
        raise ValueError('Recovery run must be distinct from the original release run')
    artifact_id = positive_int(artifact.get('id'), 'artifact.id')
    if artifact.get('name') != cfg['artifact'] or artifact.get('run_id') != original_id:
        raise ValueError('Recovery artifact is not the configured artifact from the original run')
    if not isinstance(artifact.get('digest'), str) or not artifact['digest'].startswith('sha256:'):
        raise ValueError('Recovery artifact requires its immutable SHA-256 digest')
    positive_int(artifact.get('size_in_bytes'), 'artifact.size_in_bytes')
    if recovery.get('event') != 'workflow_dispatch':
        raise ValueError('Recovery run must be a workflow_dispatch run')
    if recovery.get('publish_job') != recovery_job_names(cfg):
        raise ValueError('Recovery run must identify the configured NuGet publishing job')
    recovery_sha = commit_sha(recovery.get('workflow_sha'), 'recovery_run.workflow_sha')
    if recovery.get('artifact_id') != artifact_id or recovery.get('artifact_digest') != artifact['digest']:
        raise ValueError('Recovery run is not bound to the original artifact payload')
    evidence_metadata = receipt.get('evidence')
    if not isinstance(evidence_metadata, dict):
        raise ValueError('Recovery receipt requires evidence artifact metadata')
    evidence_id = positive_int(evidence_metadata.get('id'), 'evidence.id')
    if evidence_metadata.get('name') != recovery_artifact_name(cfg) or evidence_metadata.get('run_id') != recovery_id:
        raise ValueError('Recovery evidence artifact is not from the recovery run')
    if not isinstance(evidence_metadata.get('digest'), str) or not evidence_metadata['digest'].startswith('sha256:'):
        raise ValueError('Recovery evidence artifact requires its immutable SHA-256 digest')
    evidence_size = positive_int(evidence_metadata.get('size_in_bytes'), 'evidence.size_in_bytes')
    if evidence is None:
        raise ValueError('Recovery requires the downloaded machine-readable workflow evidence')

    release = published_release(state, name)
    if release is None or release['commit'] != binding['commit']:
        raise ValueError('Recovery requires the existing immutable release tag at the bound source commit')

    original_remote = gh('api', f"repos/{cfg['github']}/actions/runs/{original_id}")
    if original_remote.get('id') != original_id or original_remote.get('event') != 'release':
        raise ValueError('Original recovery run is not the release-event run')
    if original_remote.get('status') != 'completed' or original_remote.get('conclusion') != 'failure':
        raise ValueError('Original recovery run must preserve its completed failed history')
    if original_remote.get('head_sha') != binding['commit'] or original_remote.get('head_branch') != state['version']:
        raise ValueError('Original recovery run source does not match the immutable release tag')
    expected_workflow = f".github/workflows/{cfg['workflow']}"
    if original_remote.get('path') and original_remote['path'].lstrip('/') != expected_workflow:
        raise ValueError('Original recovery run used a different workflow')

    target_job = recovery_job_names(cfg)
    required_jobs = set(cfg.get('required_jobs', []))
    original_jobs = indexed_workflow_jobs(cfg, original_id, 'Original')
    if not required_jobs <= original_jobs.keys():
        raise ValueError('Original recovery run is missing configured required jobs')
    unknown_jobs = set(original_jobs) - required_jobs
    if any(original_jobs[job].get('conclusion') not in {'success', 'skipped'} for job in unknown_jobs):
        raise ValueError('Original recovery run contains an unknown non-success job')
    failed_jobs = original.get('failed_jobs')
    failed_live = [job for job, value in original_jobs.items() if value.get('conclusion') == 'failure']
    if failed_jobs != [target_job] or failed_live != [target_job] or original_jobs[target_job].get('conclusion') != 'failure':
        raise ValueError('Recovery is allowed only for the failed NuGet publishing job')
    if any(original_jobs[job].get('conclusion') != 'success' for job in required_jobs if job != target_job):
        raise ValueError('Recovery cannot bypass a failed or skipped build/feed job')

    matching_artifacts = [artifact for artifact in workflow_artifacts(cfg, original_id) if artifact.get('id') == artifact_id]
    if len(matching_artifacts) != 1:
        raise ValueError('Recovery receipt must identify exactly one original workflow artifact')
    remote_artifact = matching_artifacts[0]
    if remote_artifact.get('name') != cfg['artifact'] or remote_artifact.get('expired') is True:
        raise ValueError('Original recovery artifact is missing, expired, or has the wrong name')
    if remote_artifact.get('workflow_run', {}).get('id') != original_id:
        raise ValueError('Recovery artifact does not belong to the original release run')
    if remote_artifact.get('digest') != artifact['digest'] or remote_artifact.get('size_in_bytes') != artifact['size_in_bytes']:
        raise ValueError('Recovery receipt artifact payload differs from GitHub evidence')

    recovery_remote = gh('api', f"repos/{cfg['github']}/actions/runs/{recovery_id}")
    if recovery_remote.get('id') != recovery_id or recovery_remote.get('event') != 'workflow_dispatch':
        raise ValueError('Recovery run is not a workflow_dispatch run')
    if recovery_remote.get('status') != 'completed' or recovery_remote.get('conclusion') != 'success':
        raise ValueError('Recovery run did not complete successfully')
    if recovery_remote.get('head_sha') != recovery_sha:
        raise ValueError('Recovery run source commit does not match the reviewed recovery workflow SHA')
    if recovery_remote.get('path') and recovery_remote['path'].lstrip('/') != expected_workflow:
        raise ValueError('Recovery run used a different workflow')
    validate_recovery_source(cfg, receipt, recovery_sha)
    required_jobs = set(cfg.get('required_jobs', []))
    recovery_jobs = indexed_workflow_jobs(cfg, recovery_id, 'Recovery')
    if set(recovery_jobs) != required_jobs:
        raise ValueError('Recovery run must contain exactly the configured required jobs')
    if recovery_jobs.get(target_job, {}).get('conclusion') != 'success':
        raise ValueError('Recovery NuGet publishing job did not succeed')
    if any(recovery_jobs[job].get('conclusion') != 'skipped' for job in required_jobs if job != target_job):
        raise ValueError('Recovery run performed non-NuGet work instead of publishing the original artifact')
    matching_evidence = [artifact for artifact in workflow_artifacts(cfg, recovery_id) if artifact.get('id') == evidence_id]
    if len(matching_evidence) != 1:
        raise ValueError('Recovery receipt must identify exactly one workflow evidence artifact')
    remote_evidence = matching_evidence[0]
    if remote_evidence.get('name') != evidence_metadata['name'] or remote_evidence.get('expired') is True:
        raise ValueError('Recovery evidence artifact is missing, expired, or has the wrong name')
    if remote_evidence.get('workflow_run', {}).get('id') != recovery_id:
        raise ValueError('Recovery evidence artifact does not belong to the recovery run')
    if remote_evidence.get('digest') != evidence_metadata['digest'] or remote_evidence.get('size_in_bytes') != evidence_size:
        raise ValueError('Recovery evidence artifact payload differs from GitHub evidence')
    validate_recovery_evidence(
        state,
        name,
        receipt,
        evidence,
        original_id,
        artifact_id,
        artifact['digest'],
        artifact['size_in_bytes'],
        recovery_id,
        recovery_sha,
    )
    return receipt


def recovery_receipt_valid(state, name, record, expected_original_run_id=None):
    evidence_record = record.get('evidence') if isinstance(record, dict) else None
    if (
        not isinstance(record, dict)
        or not isinstance(record.get('receipt'), str)
        or not isinstance(record.get('sha256'), str)
        or not isinstance(evidence_record, dict)
        or not isinstance(evidence_record.get('archive'), str)
        or not isinstance(evidence_record.get('sha256'), str)
    ):
        return False
    try:
        if not Path(record['receipt']).is_file() or digest(record['receipt']) != record['sha256']:
            return False
        if not Path(evidence_record['archive']).is_file() or digest(evidence_record['archive']) != evidence_record['sha256']:
            return False
        receipt = read(record['receipt'])
        evidence_digest = receipt.get('evidence', {}).get('digest')
        evidence = read_recovery_evidence_archive(evidence_record['archive'], evidence_digest)
        validate_recovery_receipt(state, name, receipt, expected_original_run_id, evidence)
    except (OSError, ValueError, KeyError, TypeError):
        return False
    return True


def inspect_release(state, name):
    item = entry(state, name)
    cfg = config(state, name)
    release = published_release(state, name)
    if 'binding' not in item:
        if release:
            return {'phase':'adopt-existing','commit':release['commit'],'release':release['html_url']}
        return {'phase': 'prepare', 'publish': item['publish']}
    sha = item['binding']['commit']
    if release is None:
        return {'phase': 'publish' if item['publish'] else 'missing-upstream-release', 'commit': sha}
    if release['commit'] != sha:
        raise ValueError(f'{name}: published tag points to a different commit')
    runs = gh('api', '--paginate', '--slurp', f"repos/{cfg['github']}/actions/workflows/{cfg['workflow']}/runs?event=release&head_sha={sha}&per_page=100")
    candidates = [r for page in runs for r in page['workflow_runs'] if r['head_sha'] == sha and r['head_branch'] == state['version'] and r['event'] == 'release']
    if not candidates:
        return {'phase': 'wait-for-run', 'release': release['html_url'], 'commit': sha}
    run = max(candidates, key=lambda r: (r['run_number'], r.get('run_attempt', 1)))
    result = {'phase': 'wait-for-run', 'run_id': run['id'], 'run_url': run['html_url'], 'commit': sha, 'release': release['html_url']}
    if run['status'] != 'completed':
        return result
    if run['conclusion'] != 'success':
        recovery = item.get('recovery')
        if recovery_receipt_valid(state, name, recovery, run['id']):
            receipt = read(recovery['receipt'])
            result.update(
                original_run_id=run['id'],
                recovery_run_id=receipt['recovery_run']['id'],
                recovery_receipt=recovery['receipt'],
            )
            result['phase'] = 'verified' if receipt_valid(item) else 'verify-packages'
            return result
        return dict(result, phase='repair-pipeline', conclusion=run['conclusion'])
    jobs = gh('api', '--paginate', '--slurp', f"repos/{cfg['github']}/actions/runs/{run['id']}/jobs?filter=latest&per_page=100")
    complete = {j['name'] for page in jobs for j in page['jobs'] if j['conclusion'] == 'success'}
    missing = set(cfg['required_jobs']) - complete
    if missing:
        raise ValueError(f'{name}: required jobs did not succeed: {sorted(missing)}')
    result['phase'] = 'verified' if receipt_valid(item) else 'verify-packages'
    return result


def receipt_valid(item):
    receipt = item.get('verification')
    binding = item.get('binding')
    if not receipt or not binding or receipt['commit'] != binding['commit']:
        return False
    try:
        if any(digest(binding[k]) != binding[k + '_sha256'] for k in ('manifest', 'notes')):
            return False
        report = read(receipt['report'])
        valid = digest(receipt['report']) == receipt['report_sha256'] and report.get('verified') is True and report.get('source_commit') == binding['commit'] and report.get('version') == read(binding['manifest'])['version']
        recovery = item.get('recovery')
        if recovery:
            valid = valid and receipt.get('run_id') == recovery.get('recovery_run_id')
        return valid
    except (OSError, ValueError, KeyError):
        return False


def require_upstreams(state, name):
    check_prerequisites(state)
    required = config(state, name)['dependencies'] + config(state, name).get('stage_after', [])
    for upstream in required:
        if upstream not in state['repositories']:
            continue
        observed = inspect_release(state, upstream)
        if observed['phase'] != 'verified':
            raise ValueError(f"{name} waits for {upstream}: {observed['phase']}")


def aligned_text(text, rule, version):
    if 'property' in rule:
        tag = re.escape(rule['property'])
        pattern = rf'(<{tag}>)([^<]*)(</{tag}>)'
        value, count = re.subn(pattern, lambda m: m[1] + version + m[3], text)
    elif 'yaml_key' in rule:
        key = re.escape(rule['yaml_key'])
        pattern = rf'(^\s*{key}:\s*)([^\s#]+)'
        yaml_version = parse_version(version).base if rule.get('base_version') else version
        value, count = re.subn(pattern, lambda m: m[1] + yaml_version, text, flags=re.MULTILINE)
    elif 'package_prefix' in rule:
        prefix = re.escape(rule['package_prefix'])
        pattern = rf'(<PackageReference\b(?=[^>]*\bInclude=["\']{prefix}[^"\']*["\'])[^>]*?\bVersion=["\'])([^"\']*)(["\'])'
        value, count = re.subn(pattern, lambda m: m[1] + version + m[3], text)
        if count == 0:
            raise ValueError(f'Expected at least one package reference for {rule}, found 0; inspect changed repository structure')
        return value
    elif 'studio_branding' in rule:
        pattern = r'(["\']Elsa Studio\s+)\d+\.\d+(["\'])'
        major_minor = '.'.join(version.split('.')[:2])
        value, count = re.subn(pattern, rf'\g<1>{major_minor}\g<2>', text)
    elif 'string_constant' in rule:
        constant = re.escape(rule['string_constant'])
        pattern = rf'(\b(?:const\s+)?string\s+{constant}\s*=\s*["\'])[^"\']+(["\'])'
        value, count = re.subn(pattern, lambda m: m[1] + version + m[2], text)
    else:
        package = re.escape(rule['package'])
        pattern = rf'(<PackageVersion\b[^>]*\bInclude=[\"\']{package}[\"\'][^>]*\bVersion=[\"\'])([^\"\']*)([\"\'])'
        value, count = re.subn(pattern, lambda m: m[1] + version + m[3], text)
    if count != 1:
        raise ValueError(f'Expected one dependency declaration for {rule}, found {count}; inspect changed repository structure')
    return value


def alignment_files(path, rule):
    if 'glob' in rule:
        files = sorted(path.glob(rule['glob']))
        if not files:
            raise ValueError(f"Alignment glob matched no files: {rule['glob']}")
        return files
    if 'file' not in rule:
        raise ValueError(f'Alignment rule requires file or glob: {rule}')
    file = path / rule['file']
    if not file.is_file():
        raise ValueError(f'Alignment file does not exist: {file}')
    return [file]


def aligned_files(path, rules, version):
    files = {}
    for rule in rules:
        for file in alignment_files(path, rule):
            text = files.get(file, file.read_text())
            files[file] = aligned_text(text, rule, version)
    return files


def align(state, args):
    item = entry(state, args.repo)
    if not item['publish']:
        raise ValueError('Cannot edit a verification-only upstream repository')
    require_upstreams(state, args.repo)
    path = args.repo_path.resolve()
    # Binding a release freezes the source; changing it requires an explicit new plan.
    if 'binding' in item:
        raise ValueError('Repository is already bound; do not edit a frozen release source')
    if command(['git', 'status', '--porcelain', '--untracked-files=no'], path):
        raise ValueError('Alignment requires a clean tracked worktree')
    expected = config(state, args.repo)['github']
    remote = command(['git', 'remote', 'get-url', 'origin'], path)
    if not re.search(r'github\.com[:/]' + re.escape(expected) + r'(?:\.git)?$', remote):
        raise ValueError('Worktree remote does not match repository profile')
    files = aligned_files(path, config(state, args.repo)['alignment'], state['version'])
    changed = [str(p) for p,t in files.items() if p.read_text() != t]
    if args.execute:
        for file,text in files.items():
            file.write_text(text)
    return {'mode': 'execute' if args.execute else 'dry-run', 'changed_files': changed}


def validate_manifest(state, name, manifest, commit):
    cfg = config(state, name)
    if manifest['version'] != state['version'] or manifest['source_commit'] != commit or not manifest.get('nuget'):
        raise ValueError('Manifest does not match selected version/source or has no expected packages')
    policy = state['profile']['release_kinds'][state['kind']]
    expected_feeds = [f for f in state['profile']['feeds'] if f['name'] in policy['feeds']]
    if manifest.get('feeds') != expected_feeds:
        raise ValueError('Manifest feed policy differs from release profile')
    if sorted(x['name'] for x in manifest.get('npm', [])) != sorted(cfg['npm']):
        raise ValueError('Manifest npm package set differs from release profile')
    if any(x['dist_tag'] != policy['npm_dist_tag'] for x in manifest.get('npm', [])):
        raise ValueError('Manifest npm dist-tag differs from release profile')
    expected_ids = cfg.get('expected_package_ids')
    if expected_ids is not None and sorted(expected_ids, key=str.lower) != sorted(
        (x['id'] for x in manifest['nuget']), key=str.lower
    ):
        raise ValueError('Manifest package inventory differs from the release profile')
    if cfg.get('content_expectations') and not manifest.get('content_expectations'):
        raise ValueError('Templates manifest is missing source-derived content expectations')
    for package in manifest['nuget']:
        if package.get('version', state['version']) != state['version'] or package.get('verify_published') is False:
            exception = cfg['fixed_packages'].get(package['id'])
            if not exception or any(package.get(k) != v for k,v in exception.items()):
                raise ValueError('Unconfigured fixed-version/package-verification exception')


def validate_source_content_manifest(state, name, path, manifest):
    """Rebuild source-derived archive expectations before freezing a binding."""

    cfg = config(state, name)
    if not cfg.get('content_expectations'):
        return
    from package_manifest import embedded_content

    upstream_manifests = {}
    for upstream in cfg['content_expectations'].get('upstream_repositories', []):
        binding = entry(state, upstream).get('binding')
        if not binding:
            raise ValueError(f'Bind upstream {upstream} before validating embedded content')
        upstream_manifests[upstream] = read(binding['manifest'])
    expected = embedded_content(path, cfg, state['version'], upstream_manifests)
    if manifest.get('content_expectations') != expected:
        raise ValueError('Manifest content expectations differ from the checked-out source')


def bind(state, args):
    item = entry(state, args.repo)
    require_upstreams(state, args.repo)
    path = args.repo_path.resolve()
    sha = command(['git', 'rev-parse', args.commit + '^{commit}'], path)
    if command(['git', 'rev-parse', 'HEAD'], path) != sha or command(['git', 'status', '--porcelain', '--untracked-files=no'], path):
        raise ValueError('Bind a clean worktree checked out at the tested commit')
    cfg = config(state, args.repo)
    remote = command(['git', 'remote', 'get-url', 'origin'], path)
    if not re.search(r'github\.com[:/]' + re.escape(cfg['github']) + r'(?:\.git)?$', remote):
        raise ValueError('Worktree remote does not match repository profile')
    existing = published_release(state, args.repo)
    if existing and existing['commit'] != sha:
        raise ValueError('Existing release must be adopted at its exact immutable tag commit')
    if not existing and item['publish'] and command(['git', 'rev-parse', item['source_ref'] + '^{commit}'], path) != sha:
        raise ValueError('Tested commit differs from the intended release source; fetch and inspect the source before binding')
    for file, text in aligned_files(path, cfg['alignment'], state['version']).items():
        if text != file.read_text():
            raise ValueError('Downstream dependency references are not aligned to this release')
    for url in state['prerequisites']:
        pr = gh('pr', 'view', url, '--json', 'state,mergeCommit,url')
        if f"github.com/{cfg['github']}/pull/" in pr['url']:
            command(['git', 'merge-base', '--is-ancestor', pr['mergeCommit']['oid'], sha], path)
    manifest = read(args.manifest)
    validate_manifest(state, args.repo, manifest, sha)
    validate_source_content_manifest(state, args.repo, path, manifest)
    notes = args.notes_file.resolve()
    if not notes.read_text().strip() or 'Review before publishing:' in notes.read_text():
        raise ValueError('Curate the release notes before binding')
    binding = {'commit': sha, 'worktree': str(path), 'manifest': str(args.manifest.resolve()), 'notes': str(notes)}
    binding.update({k+'_sha256': digest(binding[k]) for k in ('manifest','notes')})
    if 'binding' in item and item['binding'] != binding:
        if not args.replace:
            raise ValueError('Existing binding differs; use --replace only for a reviewed, unpublished source change')
        if command(['git', 'ls-remote', '--tags', 'origin', f"refs/tags/{state['version']}"], path):
            raise ValueError('Cannot replace a binding after the remote tag exists')
        releases = gh('api', '--paginate', '--slurp', f"repos/{cfg['github']}/releases?per_page=100")
        if any(r['tag_name'] == state['version'] for page in releases for r in page):
            raise ValueError('Cannot replace a binding after release creation')
        item.pop('verification', None)
    item['binding'] = binding
    return binding


def verify(state, args):
    item = entry(state, args.repo)
    require_upstreams(state, args.repo)
    observed = inspect_release(state, args.repo)
    if observed['phase'] not in ('verify-packages', 'verified'):
        raise ValueError(f"Cannot verify packages yet: {observed}")
    binding = item['binding']
    if digest(binding['manifest']) != binding['manifest_sha256']:
        raise ValueError('Manifest changed since source binding')
    report = args.state.parent / f'{args.repo}-packages.json'
    result = subprocess.run([sys.executable, str(HERE/'verify_packages.py'), '--manifest', binding['manifest'], '--artifacts', str(args.artifacts.resolve()), '--output', str(report)], check=False)
    if result.returncode:
        item.pop('verification', None)
        return {'phase': 'wait-for-packages', 'report': str(report), 'exit_code': result.returncode}
    data = read(report)
    if not data.get('verified') or data.get('source_commit') != binding['commit'] or data.get('version') != state['version']:
        raise ValueError('Verifier returned inconsistent evidence')
    item['verification'] = {
        'commit': binding['commit'],
        'report': str(report),
        'report_sha256': digest(report),
        'verified_at': datetime.now(timezone.utc).isoformat(),
        'run_id': observed.get('recovery_run_id', observed['run_id']),
    }
    return {'phase': 'verified', 'report': str(report)}


def status(state):
    if not containers_configured(state):
        return {
            'next': 'adopt-containers',
            'reason': 'This checkpoint predates the required container publication gate; adopt it explicitly.',
        }
    if not post_refresh_configured(state):
        return {
            'next': 'adopt-post-refresh',
            'reason': 'This checkpoint predates the required post-release website/documentation gate; adopt it explicitly.',
        }
    check_prerequisites(state)
    results = {}
    for name in state['repositories']:
        stage_after = [stage for stage in config(state, name).get('stage_after', []) if stage in results]
        if any(results[stage]['phase'] != 'verified' for stage in stage_after):
            results[name] = {'phase': 'wait-for-stage', 'stages': stage_after}
            continue
        upstreams = config(state, name)['dependencies']
        if any(results[u]['phase'] != 'verified' for u in upstreams):
            results[name] = {'phase': 'wait-for-upstream', 'upstreams': upstreams}
        else:
            results[name] = inspect_release(state, name)
    ready = all(x['phase'] == 'verified' for x in results.values())
    containers = state['containers']
    containers_verified = not containers['enabled'] or container_receipt_valid(state)
    sites = state['post_refresh']
    missing_sites = []
    if sites['enabled']:
        for target in post_refresh_targets(state):
            receipt = sites['receipts'].get(target)
            if not site_receipt_valid(state, target, receipt):
                missing_sites.append(target)
    required = state['profile']['announcements']['platforms'] if state['announce'] else []
    missing = []
    for platform in required:
        receipt = state['announcements'].get(platform)
        if not receipt or not Path(receipt['receipt']).is_file() or digest(receipt['receipt']) != receipt['sha256']:
            missing.append(platform)
    next_phase = 'repositories'
    container_phase = 'not-required' if not containers['enabled'] else 'waiting-for-packages' if not ready else 'verified' if containers_verified else 'bind'
    if containers['enabled'] and ready and not containers_verified and containers.get('binding'):
        container_phase = 'verify' if (containers.get('dispatch') or {}).get('run_id') else 'dispatch'
    if ready:
        next_phase = (
            'containers' if containers['enabled'] and not containers_verified
            else 'sites' if sites['enabled'] and missing_sites
            else 'announcements' if missing
            else 'complete'
        )
    return {
        'repositories': results,
        'containers': {
            'enabled': containers['enabled'],
            'images': containers['images'],
            'verified': containers_verified,
            'phase': container_phase,
            'reason': containers.get('reason'),
        },
        'sites': {'enabled': sites['enabled'], 'missing': missing_sites},
        'next': next_phase,
        'missing_announcements': missing if ready and containers_verified and not missing_sites else [],
    }


def adopt_containers(state, args):
    """Explicitly upgrade a legacy checkpoint to the current container artifact gate."""

    if containers_configured(state):
        return state['containers']
    current_profile = read(DEFAULT_PROFILE)
    state.setdefault('profile', {})['container_release'] = current_profile['container_release']
    selected = [name for name, item in state['repositories'].items() if item.get('publish')]
    state['containers'] = make_container_state(
        state['profile'], state['version'], selected,
        getattr(args, 'no_containers', False),
        state['repositories'].keys(),
    )
    return state['containers']


def adopt_post_refresh(state, args):
    """Explicitly upgrade a legacy checkpoint without claiming site work is complete."""

    if 'post_release_sites' not in state.get('profile', {}):
        current_profile = read(DEFAULT_PROFILE)
        state.setdefault('profile', {})['post_release_sites'] = current_profile['post_release_sites']
    if post_refresh_configured(state):
        return state['post_refresh']
    state['schema'] = 2
    targets = getattr(args, 'targets', None) or list(SITE_TARGETS)
    if getattr(args, 'website_only', False):
        targets = ['website']
    if not set(targets) <= set(SITE_TARGETS) or len(set(targets)) != len(targets):
        raise ValueError('Post-refresh targets must be unique website/documentation entries')
    state['post_refresh'] = {
        'enabled': not getattr(args, 'no_post_refresh', False),
        'targets': targets,
        'receipts': {},
    }
    return state['post_refresh']


def record_site(state, args):
    if not post_refresh_configured(state):
        raise ValueError('Adopt the post-release phase before recording site evidence')
    if not state['post_refresh']['enabled']:
        raise ValueError('Post-release website/documentation refresh was explicitly disabled')
    if args.target not in post_refresh_targets(state):
        raise ValueError(f'Post-release target is outside this checkpoint scope: {args.target}')
    observed = status(state)
    if observed['next'] not in ('sites', 'announcements', 'complete'):
        raise ValueError('All selected packages and required Apps containers must be verified before site refresh')
    receipt = read(args.receipt)
    validate_site_receipt(state, args.target, receipt)
    value = {'receipt': str(args.receipt.resolve()), 'sha256': digest(args.receipt), 'id': receipt['id']}
    prior = state['post_refresh']['receipts'].get(args.target)
    if prior and prior != value and not getattr(args, 'replace', False):
        raise ValueError('A different site receipt is already recorded; inspect before changing it')
    state['post_refresh']['receipts'][args.target] = value
    return value


def record_recovery(state, args):
    item = entry(state, args.repo)
    if not item['publish']:
        raise ValueError('Cannot record recovery for a verification-only repository')
    require_upstreams(state, args.repo)
    evidence_archive = getattr(args, 'evidence_archive', None)
    if evidence_archive is None:
        raise ValueError('Recovery requires the downloaded machine-readable workflow evidence archive')
    evidence_archive = evidence_archive.resolve()
    receipt = read(args.receipt)
    expected_digest = receipt.get('evidence', {}).get('digest')
    evidence = read_recovery_evidence_archive(evidence_archive, expected_digest)
    validate_recovery_receipt(state, args.repo, receipt, evidence=evidence)
    value = {
        'receipt': str(args.receipt.resolve()),
        'sha256': digest(args.receipt),
        'original_run_id': receipt['original_release_run']['id'],
        'recovery_run_id': receipt['recovery_run']['id'],
        'evidence': {'archive': str(evidence_archive), 'sha256': digest(evidence_archive)},
    }
    prior = item.get('recovery')
    if prior and prior != value:
        raise ValueError('A different recovery receipt is already recorded; preserve the original failure history')
    item['recovery'] = value
    return value


def record_announcement(state, args):
    if status(state)['next'] not in ('announcements', 'complete'):
        raise ValueError('All selected packages, required Apps containers, and post-release sites must be verified before announcements')
    if not state['announce']:
        raise ValueError('Announcements were explicitly disabled for this release')
    receipt = read(args.receipt)
    required = ['id', 'url', 'text', 'status']
    if any(not receipt.get(k) for k in required) or receipt['status'] not in ('sent','published') or receipt.get('error'):
        raise ValueError('Receipt must come from verified publication, not a draft or queue acknowledgment')
    if receipt['text'].strip() != args.message_file.read_text().strip():
        raise ValueError('Published text differs from the intended announcement')
    if args.platform == 'discord' and receipt.get('crossposted') is not True:
        raise ValueError('Discord announcement is not crossposted')
    if state['version'] not in receipt['text']:
        raise ValueError('Announcement does not identify this release version')
    value = {'receipt': str(args.receipt.resolve()), 'sha256': digest(args.receipt), 'url': receipt['url'], 'id': receipt['id']}
    prior = state['announcements'].get(args.platform)
    if prior and prior != value:
        raise ValueError('A different announcement is already recorded; inspect before changing it')
    state['announcements'][args.platform] = value
    return value


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--state', type=Path, help='Release checkpoint; not required for standalone container verification')
    sub = parser.add_subparsers(dest='action', required=True)
    p = sub.add_parser('init')
    p.add_argument('--version', required=True)
    p.add_argument('--kind', choices=['stable','rc','preview'])
    p.add_argument('--profile', type=Path, default=DEFAULT_PROFILE)
    p.add_argument('--repos-root', required=True)
    p.add_argument('--repositories', nargs='+', help='Repositories to publish; upstream dependencies are verification-only')
    p.add_argument('--source', action='append', help='Explicit source override, e.g. core=3.9.0-rc1')
    p.add_argument('--pr', action='append', help='Explicit release prerequisite PR URL; never discovers arbitrary open PRs')
    p.add_argument('--no-announcements', action='store_true')
    p.add_argument('--no-post-refresh', action='store_true', help='Skip the website/documentation refresh gate')
    p.add_argument('--no-containers', action='store_true', help='Explicitly omit container publication from this release')
    sub.add_parser('status')
    p = sub.add_parser('adopt-containers')
    p.add_argument('--no-containers', action='store_true', help='Explicitly adopt the legacy checkpoint with containers disabled')
    sub.add_parser('prepare-containers')
    p = sub.add_parser('bind-containers')
    p.add_argument('--source-ref', help='Apps branch or tag to bind; defaults to the profile source ref')
    p.add_argument('--commit', required=True, help='Reviewed full Apps commit SHA')
    p.add_argument('--package-version', action='append', help='Explicit external family version, e.g. extensions=3.8.4')
    p.add_argument('--replace', action='store_true', help='Replace a reviewed binding only before workflow dispatch')
    sub.add_parser('dispatch-containers')
    p = sub.add_parser('record-containers')
    p.add_argument('--receipt', required=True, type=Path)
    p.add_argument('--artifact-archive', required=True, type=Path, help='Downloaded exact GitHub receipt artifact ZIP')
    p.add_argument('--replace', action='store_true', help='Replace a prior image receipt after fresh live verification')
    p = sub.add_parser('verify-containers')
    p.add_argument('--version', required=True)
    p.add_argument('--receipt', required=True, type=Path)
    p.add_argument('--artifact-archive', required=True, type=Path, help='Downloaded exact GitHub receipt artifact ZIP')
    p.add_argument('--profile', type=Path, default=DEFAULT_PROFILE)
    p.add_argument('--images', nargs='+', help='Configured image names; defaults to the complete inventory')
    p.add_argument('--package-version', action='append', help='Explicit expected family version, e.g. extensions=3.8.4')
    p.add_argument('--output', type=Path, help='Write the live registry verification report')
    p = sub.add_parser('adopt-post-refresh')
    p.add_argument('--no-post-refresh', action='store_true', help='Explicitly adopt the legacy checkpoint while keeping site refresh disabled')
    p.add_argument('--targets', nargs='+', choices=SITE_TARGETS, help='Site targets to adopt; use website for a website-only follow-up')
    p.add_argument('--website-only', action='store_true', help='Alias for --targets website')
    p = sub.add_parser('align')
    p.add_argument('--repo', required=True)
    p.add_argument('--repo-path', required=True, type=Path)
    p.add_argument('--execute', action='store_true')
    p = sub.add_parser('bind')
    p.add_argument('--repo', required=True)
    p.add_argument('--repo-path', required=True, type=Path)
    p.add_argument('--commit', required=True)
    p.add_argument('--manifest', required=True, type=Path)
    p.add_argument('--notes-file', required=True, type=Path)
    p.add_argument('--replace', action='store_true', help='Replace a reviewed binding only before any remote tag/release exists')
    p = sub.add_parser('verify')
    p.add_argument('--repo', required=True)
    p.add_argument('--artifacts', required=True, type=Path)
    p = sub.add_parser('record-announcement')
    p.add_argument('--platform', required=True, choices=['discord','linkedin','x'])
    p.add_argument('--receipt', required=True, type=Path)
    p.add_argument('--message-file', required=True, type=Path)
    p = sub.add_parser('record-site')
    p.add_argument('--target', required=True, choices=SITE_TARGETS)
    p.add_argument('--receipt', required=True, type=Path)
    p.add_argument('--replace', '--replace-site-receipt', dest='replace', action='store_true', help='Replace a stale or tampered site receipt after reviewing new live evidence')
    p = sub.add_parser('record-recovery')
    p.add_argument('--repo', required=True)
    p.add_argument('--receipt', required=True, type=Path)
    p.add_argument('--evidence-archive', required=True, type=Path, help='Downloaded ZIP for the recovery evidence artifact')
    args = parser.parse_args()
    try:
        if args.action == 'verify-containers':
            args.profile = args.profile.expanduser().resolve()
            args.receipt = args.receipt.expanduser().resolve()
            args.artifact_archive = args.artifact_archive.expanduser().resolve()
            if args.output:
                args.output = args.output.expanduser().resolve()
            result = verify_containers(args)
            print(json.dumps(result, indent=2, ensure_ascii=False))
            return 0
        if args.state is None:
            raise ValueError('--state is required for release checkpoint actions')
        args.state = args.state.expanduser().resolve()
        if args.action == 'record-containers':
            args.receipt = args.receipt.expanduser().resolve()
            args.artifact_archive = args.artifact_archive.expanduser().resolve()
        with locked(args.state):
            if args.action == 'init':
                result = init(args)
            else:
                state = read(args.state)
                result = status(state) if args.action == 'status' else globals()[args.action.replace('-','_')](state, args)
                if args.action != 'status':
                    save(args.state, state)
            print(json.dumps(result, indent=2, ensure_ascii=False))
            return 1 if isinstance(result,dict) and result.get('phase') == 'wait-for-packages' else 0
    except (ValueError, OSError, KeyError, subprocess.TimeoutExpired) as e:
        print(f'error: {e}', file=sys.stderr)
        return 1


if __name__ == '__main__':
    raise SystemExit(main())
