#!/usr/bin/env python3
"""Prove the historical-workflow oracle rejects identity and input mutations."""
import argparse
import copy
import json
from pathlib import Path
import subprocess


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--evidence', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    evidence = args.evidence.resolve(strict=True)
    receipt = json.loads((evidence / 'receipt.json').read_text())
    baseline = json.loads((evidence / 'released.json').read_text())
    current = json.loads((evidence / 'consolidated.json').read_text())
    if current['importedPreserved'] is not True or current['importedContractsPreserved'] is not True:
        raise SystemExit('The unmodified historical workflow must pass before negative probes run.')
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=False)
    dll = evidence / 'consolidated/bin/Debug' / receipt['framework'] / 'ActivityContract.dll'
    assembly_list = ','.join(row['assembly'] for row in receipt['matrix'])
    results = []
    for name in ['unknown-type', 'changed-id', 'changed-version', 'changed-literal', 'first-write-id']:
        changed = copy.deepcopy(baseline)
        workflow_field = 'firstWriteWorkflow' if name == 'first-write-id' else 'serializedWorkflow'
        workflow = json.loads(changed[workflow_field])
        activity = workflow['activities'][0]
        if name == 'unknown-type':
            activity['type'] = 'Compatibility.UnknownActivity'
        elif name in {'changed-id', 'first-write-id'}:
            activity['id'] = 'different-id'
        elif name == 'changed-version':
            activity['version'] = 99999
        else:
            candidates = [value['expression'] for item in workflow['activities'] for value in item.values() if isinstance(value, dict) and isinstance(value.get('expression'), dict) and value['expression'].get('type') == 'Literal' and value['expression'].get('value') == 'compatibility-probe']
            if not candidates:
                raise RuntimeError('No configured literal fixture found.')
            candidates[0]['value'] = 'changed-probe'
        changed[workflow_field] = json.dumps(workflow)
        input_path = output / (name + '-input.json')
        result_path = output / (name + '-result.json')
        input_path.write_text(json.dumps(changed))
        with (output / (name + '.log')).open('x') as log:
            completed = subprocess.run(['dotnet', str(dll), assembly_list, str(result_path), str(input_path)], cwd=evidence, stdout=log, stderr=subprocess.STDOUT, timeout=60, check=False)
        result = json.loads(result_path.read_text())
        rejected = result['importedPreserved'] is False and completed.returncode != 0
        results.append({'case': name, 'exitCode': completed.returncode, 'historicalWorkflowPreserved': result['importedPreserved'], 'contractsPreserved': result['importedContractsPreserved'], 'rejected': rejected})
    (output / 'negative-probes.json').write_text(json.dumps(results, indent=2) + '\n')
    print(json.dumps(results, indent=2))
    return 0 if all(result['rejected'] for result in results) else 1


if __name__ == '__main__':
    raise SystemExit(main())
