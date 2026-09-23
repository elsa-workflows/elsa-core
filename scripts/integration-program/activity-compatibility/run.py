#!/usr/bin/env python3
"""Run nonpublishing activity contract probes; retain failures as evidence."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def source_inputs(source):
    result = []
    for root, directories, files in os.walk(source):
        directories[:] = sorted(d for d in directories if d not in {'.git', '.codebase-memory', 'bin', 'obj', 'node_modules'})
        for name in sorted(files):
            path = Path(root) / name
            if path.suffix in {'.cs', '.csproj', '.props', '.targets'} or name in {'global.json', 'NuGet.Config', 'nuget.config'}:
                result.append({'path': str(path.relative_to(source)), 'sha256': digest(path)})
    return result


def run(command, cwd, log):
    with log.open('x') as output:
        return subprocess.run(command, cwd=cwd, stdout=output, stderr=subprocess.STDOUT, check=False).returncode


def write_project(host, framework, entries, source):
    project = ET.Element('Project', Sdk='Microsoft.NET.Sdk')
    props = ET.SubElement(project, 'PropertyGroup')
    for name, value in {'TargetFramework': framework, 'OutputType': 'Exe', 'ImplicitUsings': 'enable', 'Nullable': 'enable', 'IsPackable': 'false', 'GeneratePackageOnBuild': 'false', 'ManagePackageVersionsCentrally': 'false'}.items():
        ET.SubElement(props, name).text = value
    items = ET.SubElement(project, 'ItemGroup')
    for entry in entries:
        if source:
            ET.SubElement(items, 'ProjectReference', Include=str(source / entry['sourceProject']))
        else:
            ET.SubElement(items, 'PackageReference', Include=entry['assembly'], Version='[' + entry['releasedVersion'] + ']')
    ET.indent(project)
    ET.ElementTree(project).write(host / 'ActivityContract.csproj', encoding='unicode')
    shutil.copyfile(HERE / 'Program.cs', host / 'Program.cs')


def normalized_descriptor(descriptor):
    descriptor = json.loads(json.dumps(descriptor))
    for group in ['Inputs', 'Outputs']:
        for field in descriptor[group]:
            field.pop('Type')  # Raw assembly-qualified types remain in host receipts.
    return descriptor


def descriptor_map(result):
    descriptors = result['descriptors']
    keys = [descriptor['ClrType'] for descriptor in descriptors]
    if len(keys) != len(set(keys)):
        raise ValueError('Duplicate CLR full names across descriptor assemblies; comparison would be ambiguous.')
    return {descriptor['ClrType']: normalized_descriptor(descriptor) for descriptor in descriptors}


def classify_added_descriptors(before, after, matrix):
    source_only = {entry['assembly'] for entry in matrix if entry['releasedVersion'] is None}
    added = sorted(after.keys() - before.keys())
    allowed = [key for key in added if after[key]['Assembly'] in source_only]
    unexpected = [key for key in added if after[key]['Assembly'] not in source_only]
    return allowed, unexpected


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source-tree', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--framework', choices=['net8.0', 'net9.0', 'net10.0'], default='net10.0')
    args = parser.parse_args()
    source = args.source_tree.resolve(strict=True)
    output = args.output.resolve()
    if output.is_relative_to(source) or source.is_relative_to(output):
        parser.error('Output and source tree must be disjoint.')
    entries = json.loads((HERE / 'matrix.json').read_text())
    for entry in entries:
        if not (source / entry['sourceProject']).is_file():
            parser.error('Missing source project: ' + entry['sourceProject'])
    output.mkdir(parents=True, exist_ok=False)
    (output / 'global.json').write_text('{"sdk":{"version":"10.0.300","rollForward":"disable"}}\n')
    (output / 'NuGet.Config').write_text('<configuration><packageSources><clear/><add key="nuget.org" value="https://api.nuget.org/v3/index.json"/></packageSources><packageSourceMapping><clear/><packageSource key="nuget.org"><package pattern="*"/></packageSource></packageSourceMapping></configuration>\n')
    # Stop ambient MSBuild ancestor imports for the generated hosts.
    for name in ['Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props']:
        (output / name).write_text('<Project/>\n')
    before_inputs = source_inputs(source)
    (output / 'source-inputs.json').write_text(json.dumps(before_inputs, indent=2) + '\n')
    receipt = {'framework': args.framework, 'sdk': '10.0.300', 'matrix': entries, 'sourceHead': subprocess.check_output(['git', '-C', str(source), 'rev-parse', 'HEAD'], text=True).strip(), 'sourceStatus': subprocess.check_output(['git', '-C', str(source), 'status', '--porcelain'], text=True), 'harness': [{'path': p.name, 'sha256': digest(p)} for p in [HERE / 'Program.cs', HERE / 'matrix.json', Path(__file__).resolve()]], 'hosts': {}}
    for name in ['released', 'consolidated']:
        host = output / name
        host.mkdir()
        selected = entries if name == 'consolidated' else [e for e in entries if e['releasedVersion']]
        write_project(host, args.framework, selected, source if name == 'consolidated' else None)
        flags = ['-p:UseProjectReferences=true', '-p:IsPackable=false', '-p:GeneratePackageOnBuild=false', '-m:1']
        build = run(['dotnet', 'build', 'ActivityContract.csproj', *flags], host, output / (name + '-build.log'))
        result = {'buildExitCode': build}
        if build == 0:
            baseline = str(output / 'released.json') if name == 'consolidated' else '-'
            if baseline != '-' and not Path(baseline).exists():
                result['notRun'] = 'Released host did not produce evidence.'
            else:
                result['probeExitCode'] = run(['dotnet', 'run', '--no-build', '--no-restore', '--project', 'ActivityContract.csproj', '--', ','.join(e['assembly'] for e in selected), str(output / (name + '.json')), baseline], host, output / (name + '-probe.log'))
        assets = host / 'obj/project.assets.json'
        if assets.exists():
            resolved = json.loads(assets.read_text())
            result['assetsSha256'] = digest(assets)
            result['packages'] = [{'idVersion': key, 'sha512': value.get('sha512')} for key, value in sorted(resolved['libraries'].items()) if value['type'] == 'package']
        receipt['hosts'][name] = result
    paths = [output / (name + '.json') for name in ['released', 'consolidated']]
    if all(path.exists() for path in paths):
        old, new = [json.loads(path.read_text()) for path in paths]
        a, b = [descriptor_map(result) for result in [old, new]]
        allowed_added, unexpected_added = classify_added_descriptors(a, b, entries)
        receipt['comparison'] = {'allowedSourceOnlyAddedDescriptors': allowed_added, 'unexpectedAddedDescriptors': unexpected_added, 'missingDescriptors': sorted(a.keys() - b.keys()), 'addedDescriptors': sorted(b.keys() - a.keys()), 'changedDescriptors': sorted(k for k in a.keys() & b.keys() if a[k] != b[k]), 'releasedFailures': old['failures'], 'consolidatedFailures': new['failures'], 'historicalWorkflowPreserved': new['importedPreserved']}
    receipt['sourceInputsUnchanged'] = before_inputs == source_inputs(source)
    receipt['sourceInputsSha256'] = digest(output / 'source-inputs.json')
    (output / 'receipt.json').write_text(json.dumps(receipt, indent=2) + '\n')
    # Existing baseline failures remain a failing gate, never an allowlist pass.
    comparison = receipt.get('comparison', {})
    comparison_passed = comparison.get('historicalWorkflowPreserved') is True and not comparison.get('missingDescriptors', [None]) and not comparison.get('changedDescriptors', [None]) and not comparison.get('unexpectedAddedDescriptors', [None])
    return 0 if comparison_passed and receipt['sourceInputsUnchanged'] and all(h.get('buildExitCode') == 0 and h.get('probeExitCode') == 0 for h in receipt['hosts'].values()) else 1


if __name__ == '__main__':
    raise SystemExit(main())
