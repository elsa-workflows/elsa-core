#!/usr/bin/env python3
"""Prepare a focused local Blazor host inside an existing successful paired source proof."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import signal
import shutil
import subprocess

from run_paired_source_probe import PINS, IMPORTS, UNUSED_IMPORT, FIXTURE as CONTRACT_FIXTURE, git, patched_import_bytes

FIXTURE = Path(__file__).resolve().parent / 'paired-blazor-host'
AMBIENT_CONFIG = {
    'directory.build.props', 'directory.build.targets', 'directory.packages.props',
    'directory.solution.props', 'directory.solution.targets', 'global.json',
    'nuget.config', 'msbuild.rsp', 'directory.build.rsp',
}


def validate_build_environment(output):
    for directory in (output, *output.parents):
        for entry in directory.iterdir():
            if entry.name.lower() in AMBIENT_CONFIG:
                raise ValueError(f'Ambient build configuration is not permitted: {entry}')
    shared_source = output / 'ContractProbe' / 'HttpProbe.cs'
    if shared_source.is_symlink() or shared_source.read_bytes() != (CONTRACT_FIXTURE / 'HttpProbe.cs').read_bytes():
        raise ValueError('Shared contract source differs from the reviewed fixture')


def validate_source_proof(output):
    receipt = json.loads((output / 'evidence.json').read_text())
    result = receipt['result']
    if receipt['sourceCommits'] != PINS or not all(result.get(key) is True for key in ('providerTypeResolves', 'featureMatches', 'httpRoundtrip')) or result.get('descriptorCount') != 1 or result.get('descriptorName') != 'Synthetic' or result.get('descriptorType') != 'SyntheticWorkflowContextProvider, ContractProbe':
        raise ValueError('Run the reviewed paired source proof first')
    recorded = [json.loads(line[len('PAIR_PROOF='):]) for line in (output / 'build.log').read_text().splitlines() if line.startswith('PAIR_PROOF=')]
    if recorded != [result]:
        raise ValueError('Contract receipt does not match the retained probe output')
    for name, pin in PINS.items():
        source = output / ('elsa-' + name)
        expected_status = f'M {IMPORTS}' if name == 'extensions' else ''
        if git(source, 'rev-parse', 'HEAD') != pin or git(source, 'status', '--porcelain', '--untracked-files=all') != expected_status:
            raise ValueError(f'{name} source drift since proof')
    baseline = subprocess.check_output(['git', '-C', str(output / 'elsa-extensions'), 'show', PINS['extensions'] + ':' + IMPORTS])
    expected = patched_import_bytes(baseline)
    expected_patch = {'path': IMPORTS, 'removedLine': UNUSED_IMPORT.decode(),
                      'beforeSha256': hashlib.sha256(baseline).hexdigest(),
                      'afterSha256': hashlib.sha256(expected).hexdigest()}
    if receipt['sourcePatch'] != expected_patch or (output / 'elsa-extensions' / IMPORTS).read_bytes() != expected:
        raise ValueError('Declared import patch drift since proof')


def prepare(output):
    if os.name != 'posix':
        raise RuntimeError('This local probe requires POSIX process-group cleanup')
    validate_build_environment(output)
    validate_source_proof(output)
    solution = output / 'PairedDevelopment.sln'
    host = output / 'UiProbe'
    if solution.exists() or solution.is_symlink() or host.exists() or host.is_symlink():
        raise ValueError('Paired UI host already exists; do not overwrite it')
    projects = [
        'UiProbe/UiProbe.csproj',
        'elsa-core/src/modules/Elsa/Elsa.csproj',
        'elsa-extensions/src/modules/workflows/Elsa.WorkflowContexts/Elsa.WorkflowContexts.csproj',
        'elsa-extensions/src/modules/workflows/Elsa.Studio.WorkflowContexts/Elsa.Studio.WorkflowContexts.csproj',
    ]
    host.mkdir()
    try:
        shutil.copytree(FIXTURE, host, dirs_exist_ok=True)
        with (output / 'blazor-build.log').open('a') as log:
            for command in [
            ['dotnet', 'new', 'sln', '--format', 'sln', '--name', 'PairedDevelopment'],
            ['dotnet', 'sln', str(solution), 'add', *projects],
            ['dotnet', 'build', str(solution), '-p:TargetFramework=net10.0', '-p:UseProjectReferences=true', '--verbosity', 'quiet'],
            ]:
                process = subprocess.Popen(command, cwd=output, stdout=log, stderr=subprocess.STDOUT, start_new_session=True)
                try:
                    if process.wait(timeout=900):
                        raise RuntimeError('Paired Blazor preparation failed; inspect blazor-build.log')
                finally:
                    try:
                        os.killpg(process.pid, signal.SIGKILL)
                    except ProcessLookupError:
                        # Normal completion can already have removed the process group.
                        pass
                    process.wait()
        validate_build_environment(output)
        validate_source_proof(output)
    except BaseException:
        # These outputs were created by this invocation; retain diagnostics and source clones.
        shutil.rmtree(host)
        if solution.exists():
            solution.unlink()
        raise
    return solution


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--proof-output', type=Path, required=True)
    args = parser.parse_args()
    output = args.proof_output.resolve(strict=True)
    solution = prepare(output)
    print(json.dumps({'built': True, 'browserVerified': False, 'interactiveDebuggingVerified': False, 'solution': str(solution), 'project': str(output / 'UiProbe/UiProbe.csproj'),
                      'url': 'http://127.0.0.1:6187', 'started': False, 'published': False}, indent=2))


if __name__ == '__main__':
    main()
