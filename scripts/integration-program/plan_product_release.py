#!/usr/bin/env python3
"""Plan one historical product release using SDK metadata and fresh GET-only observations. Never publish."""
from __future__ import annotations

import argparse
import http.client
from concurrent.futures import ThreadPoolExecutor
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import tempfile
import urllib.error
import urllib.parse
import urllib.request

import product_release_metadata as metadata
from product_release_metadata import ROOT, SDK, canonical_hash, sha256
from prove_consolidated_packages import require, run
from release_unit_manifest import _is_semver2

NUGET = 'https://api.nuget.org/v3-flatcontainer/'
NPM = 'https://registry.npmjs.org/'
GITHUB = 'https://api.github.com/repos/elsa-workflows/elsa-core/'
NUGET_INDEX = 'https://api.nuget.org/v3/index.json'
# Exact public origins already present in the six admitted source configurations.
FEED_BASES = {NUGET_INDEX: NUGET, **{
    'https://f.feedz.io/' + path + '/nuget/index.json':
    'https://f.feedz.io/' + path + '/nuget/v3/packages/' for path in (
        'elsa-workflows/elsa-3', 'sfmskywalker/cshells', 'valence-works/consolelogstream',
        'valence-works/loom', 'personal/webhooks-core')}}
NPM_IDS = ('@elsa-workflows/elsa-studio-wasm', '@elsa-workflows/elsa-studio-wasm-react')
MAX_AGE_SECONDS = 3600
ID = re.compile(r'[A-Za-z0-9_][A-Za-z0-9_.-]*\Z')


def workflow_cells(environment: dict) -> list[dict]:
    event = environment['EVENT']
    if event == 'workflow_dispatch':
        require(environment['REF'] == 'refs/heads/main', 'manual_plan_requires_main')
        product, line = environment['PRODUCT'], environment['LINE']
        require(product in ('core', 'studio', 'extensions') and line in ('3.8', '3.9', '3.10'), 'source_selection')
        return [{'product': product, 'line': line, 'version': environment['VERSION']}]
    require(event == 'pull_request', 'unregistered_plan_event')
    # Six explicit hypothetical requests, not a version allocation or inferred source tip.
    return [{'product': product, 'line': line, 'version': version} for product in ('core', 'studio', 'extensions')
            for line, version in (('3.8', '3.8.5'), ('3.9', '3.9.1'))]


def now() -> str:
    return datetime.now(timezone.utc).isoformat()


def read_json(data: bytes) -> object:
    def unique(pairs):
        result = {}
        for key, value in pairs:
            require(key not in result, 'duplicate_json_identity')
            result[key] = value
        return result
    return json.loads(data, object_pairs_hook=unique)


class NoRedirects(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        return None


class Observations:
    """Bounded, unauthenticated requests to closed public origins, without retries."""
    def __init__(self):
        self.opener = urllib.request.build_opener(NoRedirects())

    def get(self, url: str) -> dict:
        require((url in FEED_BASES or url.startswith((NPM, GITHUB, *FEED_BASES.values()))) and
                not urllib.parse.urlsplit(url).fragment,
                'observation_origin')
        result = {'url': url, 'observed_at': now(), 'status': 'unavailable'}
        request = urllib.request.Request(url, headers={'User-Agent': 'Elsa-Product-Release-Plan', 'Accept': 'application/json'})
        try:
            with self.opener.open(request, timeout=30) as response:
                require(response.status == 200 and response.url == url, 'observation_response')
                data = response.read(16 * 1024 * 1024 + 1)
                require(len(data) <= 16 * 1024 * 1024, 'observation_size')
            result.update(status='observed', sha256=sha256(data), bytes=len(data), _body=data)
        except urllib.error.HTTPError as error:
            result['status'] = 'missing' if error.code == 404 else 'unavailable'
            error.close()
        except (OSError, ValueError, http.client.HTTPException):
            pass
        return result


class Semantics:
    def __init__(self, assembly: Path):
        self.assembly = assembly
        self.cache = {}

    def call(self, operation: str, **request):
        payload = json.dumps({'operation': operation, **request}, sort_keys=True)
        if payload in self.cache:
            return self.cache[payload]
        result = subprocess.run(['dotnet', str(self.assembly)], input=payload,
            text=True, capture_output=True, timeout=60, env=metadata.maintenance.build_environment())
        require(result.returncode == 0, 'invalid_nuget_semantics_input')
        self.cache[payload] = json.loads(result.stdout)
        return self.cache[payload]


def public_observation(observation: dict) -> dict:
    return {key: value for key, value in observation.items() if not key.startswith('_')}


def fresh(observation: dict, url: str, checked_at: str) -> str | None:
    if observation.get('url') != url or observation.get('status') not in ('observed', 'missing', 'unavailable'):
        return 'malformed'
    try:
        observed = datetime.fromisoformat(observation['observed_at'])
        checked = datetime.fromisoformat(checked_at)
        if observed.tzinfo is None or checked.tzinfo is None or not 0 <= (checked - observed).total_seconds() <= MAX_AGE_SECONDS:
            return 'stale'
    except (KeyError, TypeError, ValueError):
        return 'malformed'
    if observation['status'] != 'observed':
        return observation['status']
    body = observation.get('_body')
    if not isinstance(body, bytes) or observation.get('sha256') != sha256(body) or observation.get('bytes') != len(body):
        return 'malformed'
    return None


def history_url(identifier: str, npm: bool = False, *, base: str = NUGET) -> str:
    if npm:
        require(identifier in NPM_IDS, 'npm_identity')
        return NPM + urllib.parse.quote(identifier, safe='@')
    require(ID.fullmatch(identifier) is not None, 'package_identity')
    return base + identifier.lower() + '/index.json'


def check_history(identifier: str, requested: str, line: str, observation: dict, semantics: Semantics,
                  checked_at: str, *, npm: bool = False, base: str = NUGET) -> dict:
    result = {'id': identifier, 'observation': public_observation(observation)}
    reason = fresh(observation, history_url(identifier, npm, base=base), checked_at)
    if reason:
        return {**result, 'eligible': False, 'reason': 'history_' + reason}
    try:
        document = read_json(observation['_body'])
        require(isinstance(document, dict), 'history_malformed')
        if npm:
            require(document.get('name') == identifier and isinstance(document['versions'], dict) and
                    isinstance(document.get('time'), dict), 'history_partial')
            times = document['time']
            require(all(value in times for value in document['versions']), 'history_partial')
            require(all(isinstance(times[value], str) for value in document['versions']), 'history_partial')
            versions = sorted(set(document['versions']) | (set(times) - {'created', 'modified', 'unpublished'}))
            unpublished = times.get('unpublished', {})
            if unpublished:
                require(isinstance(unpublished, dict) and isinstance(unpublished.get('versions'), list), 'history_partial')
                versions = sorted(set(versions) | set(unpublished['versions']))
        else:
            versions = document['versions']
        require(isinstance(versions, list) and versions and all(isinstance(value, str) for value in versions), 'history_malformed')
        decision = semantics.call('history', requested=requested, line=line, versions=versions)
        reason = ('history_ambiguous' if decision['duplicate'] else 'version_reused' if decision['reused'] else
                  'version_not_monotonic' if not decision['monotonic'] else None)
        return {**result, 'versions': versions, 'decision': decision, 'eligible': reason is None, 'reason': reason}
    except (ValueError, KeyError, TypeError) as error:
        return {**result, 'eligible': False, 'reason': 'history_partial' if str(error) == 'history_partial' else 'history_malformed'}


def nuspec_url(identifier: str, version: str, *, base: str = NUGET) -> str:
    require(ID.fullmatch(identifier) is not None and re.fullmatch(r'[0-9A-Za-z.+-]+', version) is not None,
            'prerequisite_identity')
    return f'{base}{identifier.lower()}/{version.lower()}/{identifier.lower()}.nuspec'


def check_prerequisite(edge: dict, observation: dict, semantics: Semantics, checked_at: str, *, base: str = NUGET) -> dict:
    result = {**edge, 'observation': public_observation(observation)}
    reason = fresh(observation, nuspec_url(edge['id'], edge['version'], base=base), checked_at)
    if reason:
        return {**result, 'eligible': False, 'reason': 'prerequisite_' + reason}
    try:
        observed = semantics.call('nuspecs', values=[observation['_body'].decode('utf-8-sig')])[0]
        identity = semantics.call('versions', values=[edge['version']])[0]['normalized']
        require(observed['id'].casefold() == edge['id'].casefold() and observed['version'] == identity,
                'prerequisite_wrong_version')
        match = semantics.call('ranges', values=[{'range': edge['range'], 'version': edge['version']}])[0]
        require(match['satisfies'], 'prerequisite_wrong_version')
        # Empty dependency metadata is AnyFramework; this is not an asset/build proof.
        groups = observed['groups'] or [{'framework': 'any', 'dependencies': []}]
        framework = semantics.call('frameworks', values=[{'consumer': edge['framework'],
            'candidates': [group['framework'] for group in groups]}])[0]
        require(framework['compatible'], 'prerequisite_incompatible_framework')
        applicable = next(group for group in groups if group['framework'] == framework['nearest'])
        return {**result, 'metadata': observed, 'applicable_dependency_group': applicable,
                'eligible': True, 'reason': None, 'compatibility_scope': 'dependency-metadata-only'}
    except (ValueError, KeyError, TypeError, StopIteration, UnicodeError) as error:
        reason = str(error) if str(error) in ('prerequisite_wrong_version', 'prerequisite_incompatible_framework') else 'prerequisite_malformed'
        return {**result, 'eligible': False, 'reason': reason}


class FeedMetadata:
    """Apply native source mapping before GETs; never resolve or download package content."""
    def __init__(self, config: Path, identifiers: list[str], semantics: Semantics, observations: Observations):
        self.policy = semantics.call('feeds', config=str(config), ids=sorted(set(identifiers), key=str.casefold))
        self.policy.update(config_sha256=sha256(config.read_bytes()), config_path='NuGet.Config',
                           history_authority='nuget.org-public-plus-applicable-configured-preview-feeds')
        self.sources = {row['name']: row['url'] for row in self.policy['sources']}
        require(all(url in FEED_BASES for url in self.sources.values()), 'unreviewed_feed_origin')
        self.mapping = {row['id'].casefold(): row['sources'] for row in self.policy['packages']}
        self.observations = observations
        self.services = {}
        self.responses = {}

    def indexes(self, identifier: str, *, history: bool = False) -> list[str] | None:
        names = self.mapping[identifier.casefold()]
        if not names or any(name not in self.sources for name in names):
            return None
        indexes = {self.sources[name] for name in names}
        if history:
            # Public publication history remains an authority even when restore maps Elsa to preview.
            indexes.add(NUGET_INDEX)
        return sorted(indexes)

    def service(self, index: str) -> dict:
        if index not in self.services:
            observation = self.observations.get(index)
            reason = fresh(observation, index, now())
            try:
                require(reason is None, 'feed_' + (reason or 'malformed'))
                value = read_json(observation['_body'])
                require(isinstance(value, dict) and isinstance(value.get('resources'), list), 'feed_malformed')
                bases = [row['@id'].rstrip('/') + '/' for row in value['resources']
                         if isinstance(row, dict) and row.get('@type') == 'PackageBaseAddress/3.0.0' and
                         isinstance(row.get('@id'), str)]
                require(bases == [FEED_BASES[index]], 'feed_endpoint_identity')
                self.services[index] = {'eligible': True, 'base': bases[0], 'observation': public_observation(observation)}
            except (ValueError, KeyError, TypeError) as error:
                category = str(error) if str(error) in ('feed_missing', 'feed_unavailable', 'feed_stale',
                    'feed_malformed', 'feed_endpoint_identity') else 'feed_malformed'
                self.services[index] = {'eligible': False, 'reason': category,
                                        'observation': public_observation(observation)}
        return self.services[index]

    def prefetch(self, histories: list[str], edges: list[dict]) -> None:
        urls = set()
        for identifier, version in ([(value, None) for value in histories] + [(row['id'], row['version']) for row in edges]):
            indexes = self.indexes(identifier, history=version is None)
            for index in indexes or []:
                service = self.service(index)
                if service['eligible']:
                    base = service['base']
                    urls.add(history_url(identifier, base=base) if version is None else nuspec_url(identifier, version, base=base))
        with ThreadPoolExecutor(max_workers=4) as pool:
            self.responses = dict(zip(sorted(urls), pool.map(self.observations.get, sorted(urls))))

    def checks(self, identifier: str, semantics: Semantics, checked_at: str, *, version: str | None = None,
               line: str | None = None, edge: dict | None = None) -> list[dict]:
        indexes = self.indexes(identifier, history=edge is None)
        if indexes is None:
            return [{'eligible': False, 'reason': 'feed_mapping_unavailable',
                     'mapped_source_names': self.mapping[identifier.casefold()]}]
        results = []
        for index in indexes:
            service = self.services[index]
            if not service['eligible']:
                results.append({'feed': index, **service})
                continue
            base = service['base']
            url = history_url(identifier, base=base) if edge is None else nuspec_url(identifier, edge['version'], base=base)
            observation = self.responses[url]
            checked = (check_history(identifier, version, line, observation, semantics, checked_at, base=base)
                       if edge is None else check_prerequisite(edge, observation, semantics, checked_at, base=base))
            results.append({'feed': index, **checked})
        return results

    def history(self, identifier: str, version: str, line: str, semantics: Semantics, checked_at: str) -> dict:
        checks = self.checks(identifier, semantics, checked_at, version=version, line=line)
        failures = [row for row in checks if not row['eligible']]
        return {'id': identifier, 'feeds': checks, 'eligible': not failures,
                'reason': failures[0]['reason'] if failures else None}

    def prerequisite(self, edge: dict, semantics: Semantics, checked_at: str) -> dict:
        checks = self.checks(edge['id'], semantics, checked_at, edge=edge)
        available = [row for row in checks if row['eligible']]
        failures = [row for row in checks if not row['eligible'] and row['reason'] != 'prerequisite_missing']
        # Byte-different applicable metadata is explicit ambiguity; no arbitrary feed wins.
        hashes = {row['observation']['sha256'] for row in available}
        reason = ('prerequisite_feed_ambiguity' if len(hashes) > 1 else failures[0]['reason'] if failures else
                  'prerequisite_missing_from_applicable_feeds' if not available else None)
        return {**edge, 'feeds': checks, 'eligible': reason is None, 'reason': reason,
                'compatibility_scope': 'dependency-metadata-only'}


def validate_inventory(inventory: dict, binding: dict, version: str) -> None:
    require((inventory['source_commit'], inventory['source_tree'], inventory['requested_version']) ==
            (binding['commit'], binding['tree'], version), 'inventory_source_identity')
    require(inventory['sha256'] == canonical_hash(metadata.public_inventory(inventory)), 'inventory_identity')
    require(inventory['ownership_policy'] == metadata.ownership_policy() and binding['line'] in ('3.8', '3.9'),
            'ownership_policy_identity')
    projects = inventory['projects']
    metadata.validate_project_references(projects)
    paths = [row['path'] for row in projects]
    selected = inventory['selected']
    selected_paths = [row['project'] for row in selected]
    excluded_paths = [row['project'] for row in inventory['excluded']]
    require(paths and len(set(paths)) == len(paths) and len(set(selected_paths)) == len(selected_paths) and
            len(set(excluded_paths)) == len(excluded_paths) and not set(selected_paths) & set(excluded_paths) and
            set(selected_paths) | set(excluded_paths) == set(paths), 'inventory_membership')
    require(selected and len({row['id'].casefold() for row in selected}) == len(selected), 'inventory_package_identity')
    by_path = {row['path']: row for row in projects}
    recipe = set(inventory['release_recipe']['projects'])
    for row in selected:
        project = by_path[row['project']]
        require(project['package_id'] == row['id'] and metadata.project_scope(project, binding['product'], recipe) is None,
                'inventory_product_scope')
    for row in inventory['excluded']:
        require(metadata.project_scope(by_path[row['project']], binding['product'], recipe) == row['reason'], 'inventory_exclusion')


def outgoing_edges(inventory: dict, semantics: Semantics, version: str) -> tuple[list[dict], list[dict]]:
    selected = {row['id'].casefold() for row in inventory['selected']}
    outgoing, internal = [], []
    for row in inventory['selected']:
        policy = row['metadata']
        if policy['status'] != 'observed':
            continue
        assets = policy['_assets']
        for group in policy['dependency_groups']:
            framework = group['framework']
            require(framework in row['frameworks'], 'source_dependency_framework')
            for dependency in group['dependencies']:
                edge = {'consumer': row['id'], 'project': row['project'], 'framework': framework,
                        'id': dependency['id'], 'range': dependency['version']}
                if dependency['id'].casefold() in selected:
                    check = semantics.call('ranges', values=[{'range': dependency['version'], 'version': version}])[0]
                    internal.append({**edge, 'version': version, 'eligible': check['satisfies']})
                    continue
                target = assets['targets'].get(framework, {})
                matches = [key for key, value in target.items() if key.rsplit('/', 1)[0].casefold() == dependency['id'].casefold()
                           and value.get('type') == 'package']
                require(len(matches) == 1, 'source_resolved_prerequisite_identity')
                outgoing.append({**edge, 'version': matches[0].rsplit('/', 1)[1]})
    return outgoing, internal


def npm_intent(source: Path, binding: dict, version: str) -> dict | None:
    if binding['product'] != 'studio':
        return None
    require(_is_semver2(version), 'npm_requested_version')
    paths = ('src/hosts/Elsa.Studio.Host.CustomElements/npm/package.json',
             'src/wrappers/wrappers/react-wrapper/package.json')
    manifests = [json.loads((source / path).read_text()) for path in paths]
    require([row['name'] for row in manifests] == list(NPM_IDS) and
            NPM_IDS[0] in manifests[1].get('dependencies', {}), 'npm_source_identity')
    workflow = '.github/maintenance-inert-workflows/packages.yml.source'
    data = (source / workflow).read_bytes()
    require(all(token in data for token in (b'npm version $VERSION', b'npm install "$WASM_TGZ"',
            b'--workspace=@elsa-workflows/elsa-studio-wasm-react', b'npm pkg set "version=${VERSION}"',
            b'npm pkg set "dependencies.@elsa-workflows/elsa-studio-wasm=${VERSION}"')), 'npm_workflow_identity')
    return {'atomic': True, 'line': binding['line'], 'source_commit': binding['commit'], 'source_tree': binding['tree'],
            'workflow': {'path': workflow, 'sha256': sha256(data)},
            'manifests': [{'path': path, 'sha256': sha256((source / path).read_bytes()),
                           'checked_in_version': value['version'], 'checked_in_dependencies': value.get('dependencies', {}),
                           'peer_dependencies': value.get('peerDependencies', {})} for path, value in zip(paths, manifests)],
            'packages': [{'id': identifier, 'version': version,
                          'expected_tarball': identifier[1:].replace('/', '-') + '-' + version + '.tgz'} for identifier in NPM_IDS],
            'wrapper_dependency_intent': {NPM_IDS[0]: version},
            'artifact_proof': False, 'historical_workflow_executed': False}


def assemble_plan(binding: dict, inventory: dict, version: str, histories: list[dict], prerequisites: list[dict],
                  internal: list[dict], npm: dict | None, controller: dict, semantics: Semantics) -> dict:
    validate_inventory(inventory, binding, version)
    expected_history = [row['id'] for row in inventory['selected']] + (list(NPM_IDS) if binding['product'] == 'studio' else [])
    require(len(histories) == len(expected_history) and len({row['id'].casefold() for row in histories}) == len(histories) and
            {row['id'].casefold() for row in histories} == {value.casefold() for value in expected_history}, 'history_inventory')
    selected = {row['id'].casefold() for row in inventory['selected']}
    require(not any(row['id'].casefold() in selected for row in prerequisites), 'prerequisite_widens_selection')
    expected_edges, expected_internal = outgoing_edges(inventory, semantics, version)
    def edge_key(row):
        return row['consumer'].casefold(), row['project'], row['framework'], row['id'].casefold(), row['range'], row['version']
    actual = [edge_key(row) for row in prerequisites]
    expected = [edge_key(row) for row in expected_edges]
    require(len(set(actual)) == len(actual) and sorted(actual) == sorted(expected) and internal == expected_internal,
            'prerequisite_inventory')
    require((npm is not None) == (binding['product'] == 'studio') and
            (npm is None or npm['atomic'] is True and [row['id'] for row in npm['packages']] == list(NPM_IDS) and
             all(row['version'] == version for row in npm['packages']) and npm['source_commit'] == binding['commit'] and
             npm['source_tree'] == binding['tree'] and npm['line'] == binding['line']), 'npm_pair_identity')
    reasons = [{'category': row['reason'], 'id': row['id']} for row in histories + prerequisites if not row['eligible']]
    reasons += [{'category': 'source_metadata_unavailable', 'id': row['id']} for row in inventory['selected']
                if row['metadata']['status'] != 'observed']
    reasons += [{'category': 'selected_dependency_version_incompatible', 'id': row['consumer']} for row in internal if not row['eligible']]
    scope = metadata.public_inventory(inventory)
    scope['sha256'] = inventory['sha256']
    return {'schema': 1, 'mode': 'read-only-product-release-plan', 'controller': controller,
            'source': binding, 'product': binding['product'], 'line': binding['line'], 'requested_version': version,
            'inventory': scope, 'expected_artifacts': ([name for row in inventory['selected'] for name in
                ([f"{row['id']}.{version}.nupkg"] + ([f"{row['id']}.{version}.snupkg"] if row['symbols'] else []))] +
                ([row['expected_tarball'] for row in npm['packages']] if npm else [])),
            'excluded_artifacts': [] if npm else [{'id': identifier, 'reason': 'studio_only'} for identifier in NPM_IDS],
            'prerequisites_excluded_from_publication': sorted({row['id'] for row in prerequisites}, key=str.casefold),
            'prerequisites': prerequisites, 'selected_dependency_intent': internal, 'histories': histories, 'npm': npm,
            'eligible': not reasons, 'reasons': reasons, 'published': False, 'version_allocated': False,
            'tag_created': False, 'semantics': {'sdk_version': SDK, 'assemblies': semantics.call('identity')},
            'limits': ['Expected artifacts are names, not produced bytes.',
                'Prerequisite compatibility covers dependency metadata; clean artifact consumers remain required.',
                'No product compilation, package creation, historical workflow or publisher was executed.',
                'npm history covers the observed full packument versions/time/tombstones, not an unknowable lifetime certificate.',
                'Selected artifact builds, clean consumers, publisher/recovery and operational cutover remain separate gates.']}


def observe_core(controller: Path, line: str, observations: Observations) -> dict:
    ref = metadata.CORE_REFS[line]
    response = observations.get(GITHUB + 'git/ref/' + ref.removeprefix('refs/'))
    require(fresh(response, response['url'], now()) is None, 'source_ref_unavailable')
    value = read_json(response['_body'])
    require(isinstance(value, dict) and value.get('ref') == ref and isinstance(value.get('object'), dict) and
            value['object'].get('type') == 'commit' and isinstance(value['object'].get('sha'), str), 'source_ref_identity')
    commit = value['object']['sha']
    require(re.fullmatch(r'[a-f0-9]{40}', commit) is not None, 'source_ref_identity')
    tree = metadata.git(controller, 'rev-parse', commit + '^{tree}')
    # Published source tags are separate observations, never inferred from the branch.
    tags = observations.get(GITHUB + 'git/matching-refs/tags/' + line + '.')
    require(fresh(tags, tags['url'], now()) is None, 'source_tag_history_unavailable')
    tag_history = read_json(tags['_body'])
    require(isinstance(tag_history, list) and tag_history and
            all(isinstance(row, dict) and isinstance(row.get('ref'), str) and
                row['ref'].startswith('refs/tags/' + line + '.') and isinstance(row.get('object'), dict) and
                row['object'].get('type') in ('tag', 'commit') and isinstance(row['object'].get('sha'), str) and
                re.fullmatch(r'[a-f0-9]{40}', row['object']['sha']) for row in tag_history) and
            len({row['ref'] for row in tag_history}) == len(tag_history), 'source_tag_history_identity')
    return {'ref': ref, 'commit': commit, 'tree': tree, 'observed_at': response['observed_at'],
            'branch_observation': public_observation(response), 'tag_history': tag_history,
            'tag_observation': public_observation(tags), 'tag_scope': 'bounded-matching-ref-snapshot-not-complete-version-authority'}


def build_helper(output: Path) -> Semantics:
    output.mkdir(parents=True, exist_ok=True)
    (output / 'global.json').write_text(json.dumps({'sdk': {'version': SDK, 'rollForward': 'disable'}}))
    project = output / 'tooling-source'
    project.mkdir()
    for name in ('ProductReleaseSemantics.csproj', 'Program.cs'):
        shutil.copyfile(ROOT / 'scripts/integration-program/ProductReleaseSemantics' / name, project / name)
    require(run(['dotnet', '--version'], output).strip() == SDK, 'metadata_sdk_identity')
    binaries = output / 'binaries'
    run(['dotnet', 'build', str(project / 'ProductReleaseSemantics.csproj'),
         '--nologo', '-o', str(binaries)], output, env=metadata.maintenance.build_environment())
    return Semantics(binaries / 'ProductReleaseSemantics.dll')


def execute(controller: Path, product: str, line: str, requested: str, output: Path, semantics: Semantics) -> dict:
    controller_identity = {'commit': metadata.git(controller, 'rev-parse', 'HEAD'),
                           'tree': metadata.git(controller, 'rev-parse', 'HEAD^{tree}'),
                           'execution': {key.removeprefix('GITHUB_').lower(): os.environ[key] for key in
                               ('GITHUB_REPOSITORY', 'GITHUB_EVENT_NAME', 'GITHUB_REF', 'GITHUB_RUN_ID', 'GITHUB_RUN_ATTEMPT')
                               if key in os.environ},
                           'input_sha256': {path: sha256((ROOT / path).read_bytes()) for path in (
                               'scripts/integration-program/plan_product_release.py',
                               'scripts/integration-program/product_release_metadata.py',
                               'scripts/integration-program/ProductReleaseSemantics/Program.cs',
                               'scripts/integration-program/ProductReleaseSemantics/ProductReleaseSemantics.csproj')}}
    require(not metadata.git(controller, 'status', '--porcelain'), 'controller_not_clean')
    version = semantics.call('versions', values=[requested])[0]
    require(f"{version['major']}.{version['minor']}" == line, 'requested_version_line')
    version = version['normalized']
    output.mkdir(parents=True, exist_ok=False)
    (output / 'global.json').write_text(json.dumps({'sdk': {'version': SDK, 'rollForward': 'disable'}}))
    if line == '3.10':
        plan = {'schema': 1, 'controller': controller_identity, 'product': product, 'line': line,
                'requested_version': version, 'eligible': False, 'reasons': [{'category': 'aligned_baseline_pending'}],
                'published': False, 'version_allocated': False, 'tag_created': False}
        (output / 'plan.json').write_text(json.dumps(plan, indent=2) + '\n')
        return plan
    observations = Observations()
    observation = observe_core(controller, line, observations) if product == 'core' else None
    commit = observation['commit'] if observation else metadata.DESCENDANTS[(product, line)]
    binding = metadata.bind_source(controller, product, line, commit, observation)
    source = output / 'source.private'
    metadata.checkout_source(controller, binding, source)
    inventory = metadata.evaluate_inventory(source, binding, version, output / 'metadata.private')
    npm = npm_intent(source, binding, version)
    edges, internal = outgoing_edges(inventory, semantics, version)
    history_ids = [row['id'] for row in inventory['selected']]
    feeds = FeedMetadata(source / 'NuGet.Config', history_ids + [edge['id'] for edge in edges], semantics, observations)
    for row in inventory['selected']:
        if row['metadata']['status'] == 'observed':
            scope = row['metadata']['restore_scope']
            require(scope['config_files'] == [{'path': 'NuGet.Config', 'sha256': feeds.policy['config_sha256']}] and
                    set(scope['public_sources']) == set(feeds.sources.values()), 'restore_feed_scope')
    feeds.prefetch(history_ids, edges)
    npm_observations = [observations.get(history_url(identifier, True)) for identifier in NPM_IDS] if npm else []
    checked_at = now()
    histories = [feeds.history(identifier, version, line, semantics, checked_at) for identifier in history_ids]
    histories += [check_history(identifier, version, line, observed, semantics, checked_at, npm=True)
                  for identifier, observed in zip(NPM_IDS, npm_observations)]
    prerequisites = [feeds.prerequisite(edge, semantics, checked_at) for edge in edges]
    plan = assemble_plan(binding, inventory, version, histories, prerequisites, internal, npm, controller_identity, semantics)
    plan.update(requested_version_input=requested, observed_at=checked_at, consumer_feed_policy=feeds.policy,
                feed_service_observations=feeds.services)
    (output / 'plan.json').write_text(json.dumps(plan, indent=2) + '\n')
    return plan


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--controller', type=Path, default=ROOT)
    parser.add_argument('--product', choices=('core', 'studio', 'extensions'), required=True)
    parser.add_argument('--line', choices=('3.8', '3.9', '3.10'), required=True)
    parser.add_argument('--version', required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    if args.output.exists():
        print(json.dumps({'status': 'incomplete', 'category': 'plan_output_exists', 'published': False}))
        return 1
    # Raw logs/checkouts/nuspec file paths stay private; only plan.json is a public artifact.
    with tempfile.TemporaryDirectory(prefix='product-release-semantics-') as temporary:
        try:
            semantics = build_helper(Path(temporary))
            plan = execute(args.controller.resolve(), args.product, args.line, args.version, args.output.resolve(), semantics)
        except (ValueError, OSError, KeyError, subprocess.TimeoutExpired):
            failure = {'schema': 1, 'status': 'incomplete', 'product': args.product, 'line': args.line,
                'requested_version_input': args.version, 'observed_at': now(), 'eligible': False,
                'category': 'plan_input_or_metadata_unavailable', 'published': False,
                'version_allocated': False, 'tag_created': False}
            args.output.mkdir(parents=True, exist_ok=True)
            (args.output / 'plan.json').write_text(json.dumps(failure, indent=2) + '\n')
            print(json.dumps({'status': 'incomplete', 'category': failure['category'], 'published': False}))
            return 1
    print(json.dumps({'eligible': plan['eligible'], 'published': False}))
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
