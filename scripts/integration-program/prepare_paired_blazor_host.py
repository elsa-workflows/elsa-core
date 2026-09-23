#!/usr/bin/env python3
"""Prepare a focused local Blazor host inside an existing successful paired source proof."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess

from run_paired_source_probe import PINS, IMPORTS, git

FIXTURE = Path(__file__).resolve().parent / 'paired-blazor-host'


def validate_source_proof(output):
    receipt = json.loads((output / 'evidence.json').read_text())
    if receipt['sourceCommits'] != PINS or not receipt['result']['providerTypeResolves']:
        raise ValueError('Run the reviewed paired source proof first')
    for name, pin in PINS.items():
        source = output / ('elsa-' + name)
        expected_status = f'M {IMPORTS}' if name == 'extensions' else ''
        if git(source, 'rev-parse', 'HEAD') != pin or git(source, 'status', '--porcelain', '--untracked-files=all') != expected_status:
            raise ValueError(f'{name} source drift since proof')
    if hashlib.sha256((output / 'elsa-extensions' / IMPORTS).read_bytes()).hexdigest() != receipt['sourcePatch']['afterSha256']:
        raise ValueError('Declared import patch drift since proof')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--proof-output', type=Path, required=True)
    args = parser.parse_args()
    output = args.proof_output.resolve(strict=True)
    validate_source_proof(output)
    solution = output / 'PairedDevelopment.sln'
    if solution.exists() or (output / 'UiProbe').exists():
        raise ValueError('Paired UI host already exists; do not overwrite it')
    shutil.copytree(FIXTURE, output / 'UiProbe')
    projects = [
        'UiProbe/UiProbe.csproj',
        'elsa-core/src/modules/Elsa/Elsa.csproj',
        'elsa-extensions/src/modules/workflows/Elsa.WorkflowContexts/Elsa.WorkflowContexts.csproj',
        'elsa-extensions/src/modules/workflows/Elsa.Studio.WorkflowContexts/Elsa.Studio.WorkflowContexts.csproj',
    ]
    with (output / 'blazor-build.log').open('w') as log:
        for command in [
            ['dotnet', 'new', 'sln', '--format', 'sln', '--name', 'PairedDevelopment'],
            ['dotnet', 'sln', str(solution), 'add', *projects],
            ['dotnet', 'build', str(solution), '-p:TargetFramework=net10.0', '-p:UseProjectReferences=true', '--verbosity', 'quiet'],
        ]:
            subprocess.run(command, cwd=output, check=True, stdout=log, stderr=subprocess.STDOUT, timeout=900)
    validate_source_proof(output)
    print(json.dumps({'built': True, 'browserVerified': False, 'interactiveDebuggingVerified': False, 'solution': str(solution), 'project': str(output / 'UiProbe/UiProbe.csproj'),
                      'url': 'http://127.0.0.1:6187', 'started': False, 'published': False}, indent=2))


if __name__ == '__main__':
    main()
