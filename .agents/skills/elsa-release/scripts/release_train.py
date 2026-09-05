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
from datetime import datetime, timezone
from urllib.parse import urlparse

from release_support import parse_version

HERE = Path(__file__).resolve().parent
DEFAULT_PROFILE = HERE.parent / 'references' / 'elsa-profile.json'
SITE_TARGETS = ('website', 'documentation')
VALID_SITE_STATUSES = {'completed', 'published', 'verified'}


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
    old_rest = {key: value for key, value in existing.items() if key != 'repositories'}
    new_rest = {key: value for key, value in current.items() if key != 'repositories'}
    return old_rest == new_rest


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
        return digest(receipt['report']) == receipt['report_sha256'] and report.get('verified') is True and report.get('source_commit') == binding['commit'] and report.get('version') == read(binding['manifest'])['version']
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
    item['verification'] = {'commit': binding['commit'], 'report': str(report), 'report_sha256': digest(report), 'verified_at': datetime.now(timezone.utc).isoformat(), 'run_id': observed['run_id']}
    return {'phase': 'verified', 'report': str(report)}


def status(state):
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
    if ready:
        next_phase = 'sites' if sites['enabled'] and missing_sites else 'announcements' if missing else 'complete'
    return {
        'repositories': results,
        'sites': {'enabled': sites['enabled'], 'missing': missing_sites},
        'next': next_phase,
        'missing_announcements': missing if ready and not missing_sites else [],
    }


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
        raise ValueError('All selected repositories and upstream packages must be verified before site refresh')
    receipt = read(args.receipt)
    validate_site_receipt(state, args.target, receipt)
    value = {'receipt': str(args.receipt.resolve()), 'sha256': digest(args.receipt), 'id': receipt['id']}
    prior = state['post_refresh']['receipts'].get(args.target)
    if prior and prior != value and not getattr(args, 'replace', False):
        raise ValueError('A different site receipt is already recorded; inspect before changing it')
    state['post_refresh']['receipts'][args.target] = value
    return value


def record_announcement(state, args):
    if status(state)['next'] not in ('announcements', 'complete'):
        raise ValueError('All selected repositories, post-release sites, and upstream packages must be verified before announcements')
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
    parser.add_argument('--state', required=True, type=Path)
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
    sub.add_parser('status')
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
    args = parser.parse_args()
    args.state = args.state.expanduser().resolve()
    try:
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
