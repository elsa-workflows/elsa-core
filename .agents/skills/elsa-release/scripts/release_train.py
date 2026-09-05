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

from release_support import parse_version

HERE = Path(__file__).resolve().parent
DEFAULT_PROFILE = HERE.parent / 'references' / 'elsa-profile.json'


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
        'schema': 1, 'version': version.version, 'kind': version.kind,
        'profile': profile, 'prerequisites': args.pr or [],
        'announce': not args.no_announcements, 'announcements': {},
        'repositories': {r['name']: {
            'path': str((Path(args.repos_root).expanduser().resolve() / r['directory'])),
            'publish': r['name'] in selected,
            'source_ref': sources.get(r['name'], f'origin/release/{version.base}'),
        } for r in profile['repositories'] if r['name'] in needed},
    }
    if args.state.exists():
        existing = read(args.state)
        # Never erase a partially completed train by repeating init.
        for field in ['version', 'kind', 'profile', 'prerequisites', 'announce']:
            if existing[field] != state[field]:
                raise ValueError(f'Existing state has a different {field}; use its recorded inputs')
        if {k: (v['publish'], v['path'], v['source_ref']) for k,v in existing['repositories'].items()} != {k: (v['publish'], v['path'], v['source_ref']) for k,v in state['repositories'].items()}:
            raise ValueError('Existing state has different repository scope or paths')
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
    for upstream in config(state, name)['dependencies']:
        observed = inspect_release(state, upstream)
        if observed['phase'] != 'verified':
            raise ValueError(f"{name} waits for {upstream}: {observed['phase']}")


def aligned_text(text, rule, version):
    if 'property' in rule:
        tag = re.escape(rule['property'])
        pattern = rf'(<{tag}>)([^<]*)(</{tag}>)'
    else:
        package = re.escape(rule['package'])
        pattern = rf'(<PackageVersion\b[^>]*\bInclude=[\"\']{package}[\"\'][^>]*\bVersion=[\"\'])([^\"\']*)([\"\'])'
    value, count = re.subn(pattern, lambda m: m[1] + version + m[3], text)
    if count != 1:
        raise ValueError(f'Expected one dependency declaration for {rule}, found {count}; inspect changed repository structure')
    return value


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
    files = {}
    for rule in config(state, args.repo)['alignment']:
        file = path / rule['file']
        files[file] = aligned_text(files.get(file, file.read_text()), rule, state['version'])
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
    for package in manifest['nuget']:
        if package.get('version', state['version']) != state['version'] or package.get('verify_published') is False:
            exception = cfg['fixed_packages'].get(package['id'])
            if not exception or any(package.get(k) != v for k,v in exception.items()):
                raise ValueError('Unconfigured fixed-version/package-verification exception')


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
    for rule in cfg['alignment']:
        text = (path / rule['file']).read_text()
        if aligned_text(text, rule, state['version']) != text:
            raise ValueError('Downstream dependency references are not aligned to this release')
    for url in state['prerequisites']:
        pr = gh('pr', 'view', url, '--json', 'state,mergeCommit,url')
        if f"github.com/{cfg['github']}/pull/" in pr['url']:
            command(['git', 'merge-base', '--is-ancestor', pr['mergeCommit']['oid'], sha], path)
    manifest = read(args.manifest)
    validate_manifest(state, args.repo, manifest, sha)
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
    check_prerequisites(state)
    results = {}
    for name in state['repositories']:
        upstreams = config(state, name)['dependencies']
        if any(results[u]['phase'] != 'verified' for u in upstreams):
            results[name] = {'phase': 'wait-for-upstream', 'upstreams': upstreams}
        else:
            results[name] = inspect_release(state, name)
    ready = all(x['phase'] == 'verified' for x in results.values())
    required = state['profile']['announcements']['platforms'] if state['announce'] else []
    missing = []
    for platform in required:
        receipt = state['announcements'].get(platform)
        if not receipt or not Path(receipt['receipt']).is_file() or digest(receipt['receipt']) != receipt['sha256']:
            missing.append(platform)
    return {'repositories': results, 'next': 'complete' if ready and not missing else 'announcements' if ready else 'repositories', 'missing_announcements': missing if ready else []}


def record_announcement(state, args):
    if status(state)['next'] not in ('announcements', 'complete'):
        raise ValueError('All selected repositories and upstream packages must be verified before announcements')
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
    p.add_argument('--repositories', nargs='+', choices=['core','studio','extensions'])
    p.add_argument('--source', action='append', help='Explicit source override, e.g. core=3.9.0-rc1')
    p.add_argument('--pr', action='append', help='Explicit release prerequisite PR URL; never discovers arbitrary open PRs')
    p.add_argument('--no-announcements', action='store_true')
    sub.add_parser('status')
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
