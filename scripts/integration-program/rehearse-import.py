#!/usr/bin/env python3
"""Rehearse source relocation with original histories in a NEW disposable repository.

No source checkout, branch, remote or publisher is modified. The resulting commit
is an evidence artifact, not a buildable consolidation or a publishable branch.
"""
import argparse
import json
import os
from pathlib import Path, PurePosixPath
import subprocess

PINS = {
    'extensions': '33fa0bfd28c7585240e3d4f665058c067b17e287',
    'studio': '9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822',
}
# Preserve competing implementations as inert evidence until compatibility review.
DUPLICATES = (
    'src/modules/secrets/Elsa.Secrets.Persistence.EFCore',
    'src/modules/secrets/Elsa.Secrets.Persistence.EFCore.PostgreSql',
    'src/modules/secrets/Elsa.Secrets.Persistence.EFCore.SqlServer',
    'src/modules/secrets/Elsa.Secrets.Persistence.EFCore.Sqlite',
    'src/modules/secrets/Elsa.Studio.Secrets',
)
RULES = {
    'extensions': (
        ('src/modules/', 'src/extensions/'),
        ('src/workbench/', 'samples/extensions/workbench/'),
        ('test/', 'test/extensions/'),
        ('doc/', 'doc/extensions/'),
    ),
    'studio': (
        ('src/', 'src/studio/'),
        ('tests/', 'test/studio/'),
        ('samples/', 'samples/studio/'),
        ('doc/', 'doc/studio/'),
        ('docs/', 'doc/studio/docs/'),
        ('specs/', 'specs/studio/'),
        ('artwork/', 'doc/studio/artwork/'),
        ('postman/', 'doc/studio/postman/'),
    ),
}


def git(repo, *args, data=None):
    return subprocess.check_output(['git', '-C', str(repo), *args], input=data)


def tree(repo, ref):
    entries = {}
    for row in git(repo, 'ls-tree', '-r', '-z', ref).split(b'\0'):
        if not row:
            continue
        meta, path = row.split(b'\t', 1)
        mode, kind, oid = meta.decode().split()
        if kind != 'blob':
            raise ValueError(f'Unsupported tree entry {kind}: {path!r}')
        entries[path.decode()] = (mode, oid)
    return entries


def destination(product, path):
    if PurePosixPath(path).is_absolute() or '..' in PurePosixPath(path).parts:
        raise ValueError(f'Unsafe source path: {path}')
    if product == 'extensions' and any(path.startswith(p + '/') for p in DUPLICATES):
        return f'doc/integration-program/legacy/{product}/{path}.source'
    for source, target in RULES[product]:
        if path.startswith(source):
            return target + path[len(source):]
    # Root props, central versions, workflows, tooling, instructions and build
    # assets must be integrated deliberately; merely importing cannot activate them.
    return f'doc/integration-program/legacy/{product}/{path}.source'


def relocation_plan(core, sources):
    destinations = dict(core)
    records = []
    for product, entries in sources.items():
        for source, (mode, oid) in sorted(entries.items()):
            target = destination(product, source)
            if target in destinations:
                raise ValueError(f'Destination collision: {product}:{source} -> {target}')
            destinations[target] = (mode, oid)
            records.append(dict(repository=product, source=source, destination=target,
                                mode=mode, blob=oid))
    # File/directory collisions cannot be detected by exact path equality alone.
    for path in destinations:
        if any(str(parent) in destinations for parent in PurePosixPath(path).parents):
            raise ValueError(f'File/directory collision: {path}')
    return destinations, records


def rehearse(core, sources, output):
    repositories = {'core': Path(core).resolve(), **{k: Path(v).resolve() for k, v in sources.items()}}
    repositories = {k: Path(git(v, 'rev-parse', '--show-toplevel').decode().strip()).resolve()
                    for k, v in repositories.items()}
    protected = list(repositories.values())
    for repo in repositories.values():
        common = git(repo, 'rev-parse', '--git-common-dir').decode().strip()
        protected.append((repo / common).resolve())
    refs = {}
    for product, repo in repositories.items():
        if git(repo, 'rev-parse', '--is-shallow-repository').strip() != b'false':
            raise ValueError(f'Full history required: {product}; fetch --unshallow first')
        ref = 'HEAD' if product == 'core' else PINS[product]
        refs[product] = git(repo, 'rev-parse', '--verify', ref + '^{commit}').decode().strip()
    core_tree = tree(repositories['core'], refs['core'])
    sources_tree = {k: tree(repositories[k], refs[k]) for k in sources}
    expected, records = relocation_plan(core_tree, sources_tree)
    output = Path(output).resolve()
    if any(output == repo or repo in output.parents for repo in protected):
        raise ValueError('Output must be outside every source repository')
    if output.exists():
        raise ValueError('Output must not exist; choose a new disposable directory')
    output.mkdir(parents=True)
    git(output, 'init', '--quiet')
    for product, repo in repositories.items():
        git(output, 'fetch', '--quiet', '--no-tags', str(repo),
            refs[product] + ':refs/heads/source-' + product)
    git(output, 'read-tree', '--empty')
    index = b''.join(f'{mode} {oid}\t{path}'.encode() + b'\0'
                     for path, (mode, oid) in sorted(expected.items()))
    git(output, 'update-index', '-z', '--index-info', data=index)
    tree_id = git(output, 'write-tree').decode().strip()
    command = ['git', '-C', str(output), '-c', 'commit.gpgsign=false',
               'commit-tree', tree_id]
    for ref in refs.values():
        command.extend(['-p', ref])
    env = dict(os.environ, GIT_AUTHOR_NAME='Elsa import rehearsal',
               GIT_AUTHOR_EMAIL='rehearsal@example.invalid',
               GIT_COMMITTER_NAME='Elsa import rehearsal',
               GIT_COMMITTER_EMAIL='rehearsal@example.invalid')
    commit = subprocess.check_output(command, input=b'Disposable integration history rehearsal\n',
                                     env=env).decode().strip()
    git(output, 'update-ref', 'refs/heads/rehearsal', commit)
    if tree(output, commit) != expected:
        raise ValueError('Rehearsal tree differs from exact source blob/mode mapping')
    for ref in refs.values():
        git(output, 'merge-base', '--is-ancestor', ref, commit)
    # Check all reachable history and blobs, not just three tips.
    git(output, 'fsck', '--full', '--no-dangling', commit)
    report = dict(sourceCommits=refs, rehearsalCommit=commit,
                  sourceFileCounts={k: len(v) for k, v in sources_tree.items()},
                  coreFiles=len(core_tree), finalFiles=len(expected),
                  originalHistoriesReachable=True, exactBlobAndModeMapping=True,
                  buildCompatibilityVerified=False, publicationAuthorized=False,
                  mapping=records)
    (output / 'import-receipt.json').write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps({k: v for k, v in report.items() if k != 'mapping'}, indent=2))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--core', required=True)
    parser.add_argument('--extensions', required=True)
    parser.add_argument('--studio', required=True)
    parser.add_argument('--output', required=True)
    args = parser.parse_args()
    rehearse(args.core, {'extensions': args.extensions, 'studio': args.studio}, args.output)


if __name__ == '__main__':
    main()
