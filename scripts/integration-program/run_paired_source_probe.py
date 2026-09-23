#!/usr/bin/env python3
"""Build and exercise the real paired WorkflowContexts modules in disposable local clones."""
import argparse
import hashlib
import json
import os
import signal
from pathlib import Path
import shutil
import subprocess

PINS = {
    'core': '22f479d423975cf5d35f12fa5204b6597764d06b',
    'extensions': '33fa0bfd28c7585240e3d4f665058c067b17e287',
    'studio': '9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822',
}
FIXTURE = Path(__file__).resolve().parent / 'paired-source-probe'
IMPORTS = 'src/modules/workflows/Elsa.Studio.WorkflowContexts/_Imports.razor'
UNUSED_IMPORT = b'@using Blazored.FluentValidation'


def git(root, *args):
    return subprocess.check_output(['git', '-C', str(root), *args], text=True).strip()


def patch_import(path):
    before = path.read_bytes()
    lines = before.splitlines(keepends=True)
    matches = [line for line in lines if line.rstrip(b'\r\n') == UNUSED_IMPORT]
    if len(matches) != 1:
        raise ValueError('Pinned Razor unused-import precondition changed')
    after = b''.join(line for line in lines if line not in matches)
    path.write_bytes(after)
    return {'path': IMPORTS, 'removedLine': UNUSED_IMPORT.decode(),
            'beforeSha256': hashlib.sha256(before).hexdigest(),
            'afterSha256': hashlib.sha256(after).hexdigest()}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in PINS:
        parser.add_argument(f'--{name}-source', required=True, type=Path)
    parser.add_argument('--output', required=True, type=Path, help='New disposable directory; never reused or deleted')
    args = parser.parse_args()
    if os.name != "posix":
        raise RuntimeError("This local probe requires POSIX process-group cleanup (macOS or Linux)")
    sources = {name: getattr(args, name + '_source').resolve(strict=True) for name in PINS}
    for name, source in sources.items():
        if git(source, 'rev-parse', PINS[name] + '^{commit}') != PINS[name]:
            raise ValueError(f'{name} pinned commit unavailable')
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=False)
    with (output / 'build.log').open('w') as log:
        for name, source in sources.items():
            clone = output / ('elsa-' + name)
            subprocess.run(['git', 'clone', '--shared', '--no-checkout', str(source), str(clone)], check=True, stdout=log, stderr=subprocess.STDOUT)
            subprocess.run(['git', '-C', str(clone), 'checkout', '--detach', PINS[name]], check=True, stdout=log, stderr=subprocess.STDOUT)
        patch = patch_import(output / 'elsa-extensions' / IMPORTS)
        shutil.copytree(FIXTURE, output / 'ContractProbe', ignore=shutil.ignore_patterns('bin', 'obj'))
        process = subprocess.Popen(['dotnet', 'run', '--project', 'ContractProbe.csproj', '-p:UseProjectReferences=true', '--verbosity', 'quiet'],
                                   cwd=output / 'ContractProbe', stdout=log, stderr=subprocess.STDOUT, start_new_session=True)
        try:
            if process.wait(timeout=900):
                raise RuntimeError('Paired source build/probe failed; inspect build.log')
        finally:
            # dotnet run may own a child host. End this probe's process group,
            # including after timeout/interruption; never target other dotnet hosts.
            try:
                os.killpg(process.pid, signal.SIGKILL)
            except ProcessLookupError:
                # Normal completion may already have removed the entire group.
                pass
            process.wait()

    results = [json.loads(line[len('PAIR_PROOF='):]) for line in (output / 'build.log').read_text().splitlines() if line.startswith('PAIR_PROOF=')]
    if len(results) != 1 or not results[0]['featureMatches'] or not results[0]['httpRoundtrip'] or results[0]['descriptorName'] != 'Synthetic' or results[0]['descriptorCount'] != 1 or not results[0]['providerTypeResolves']:
        raise RuntimeError('Missing or unexpected paired contract receipt')
    if results[0]['descriptorType'] != 'SyntheticWorkflowContextProvider, ContractProbe':
        raise RuntimeError('Missing or unexpected paired contract receipt')
    for name in PINS:
        clone = output / ('elsa-' + name)
        if git(clone, 'rev-parse', 'HEAD') != PINS[name]:
            raise RuntimeError(f'{name} source pin changed during build')
        expected = f'M {IMPORTS}' if name == 'extensions' else ''
        if git(clone, 'status', '--porcelain', '--untracked-files=all') != expected:
            raise RuntimeError(f'{name} source changed beyond declared import patch')
    if hashlib.sha256((output / 'elsa-extensions' / IMPORTS).read_bytes()).hexdigest() != patch['afterSha256']:
        raise RuntimeError('Declared import patch changed during execution')
    receipt = {'sourceCommits': PINS, 'targetFramework': 'net10.0', 'sourcePatch': patch,
               'result': results[0], 'publicationPerformed': False,
               'limitations': ['Synthetic loopback identity only; production authorization not proved.',
                               'Studio provider service executes without interactive Blazor UI or debugger.',
                               'Pinned pre-consolidation sibling source layout; not a completed root solution migration.']}
    (output / 'evidence.json').write_text(json.dumps(receipt, indent=2) + '\n')
    print(json.dumps(receipt, indent=2))


if __name__ == '__main__':
    main()
