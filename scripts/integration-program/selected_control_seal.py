#!/usr/bin/env python3
"""Seal and independently read back six selected-product control cells.

No package/native/product code executes here. The provider projection is an
input from the fixed retrieval job; this module does not fetch or invent it.
"""
from __future__ import annotations

import argparse
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import shutil
import xml.etree.ElementTree as ET

import product_release_metadata as metadata
from product_artifact_execution import hosted_context
import prove_product_release_artifacts as producer
from prove_consolidated_packages import require
import selected_control_transport as transport
import selected_studio_payload as studio

SCOPE = 'selected-product-preupload-byte-and-receipt-seal'
INHERITED = ['original compiler and native SDK package checks', 'original product tests',
             'clean consumer restore/build and representative runtime execution', 'original npm lifecycle/import/Vite execution']
MANIFEST_KEYS = {'schema', 'scope', 'context', 'controller', 'product', 'line', 'source', 'version',
                 'plan_sha256', 'producer_receipt_sha256', 'consumer_receipt_sha256',
                 'artifact_execution', 'consumer_execution', 'files', 'inherited_judgments'}


def inherited_judgments(product: str) -> list[str]:
    """Return producer judgments inherited by the selected product's seal."""
    return INHERITED if product == 'studio' else INHERITED[:-1]


def encoded(value: dict) -> bytes:
    """Encode sorted, indented JSON bytes while rejecting nonfinite values."""
    return (json.dumps(value, indent=2, sort_keys=True, allow_nan=False) + '\n').encode()


def absolute(path: Path) -> Path:
    """Return an absolute path after rejecting traversal and symlink components."""
    require('..' not in path.parts, 'selected_seal_path')
    result = Path(os.path.abspath(path))
    require(not any(part.is_symlink() for part in (result, *result.parents)), 'selected_seal_path')
    return result


def output_location(output: Path, inputs: list[Path]) -> Path:
    """Require a new seal destination disjoint from the controller and all input paths."""
    output = absolute(output)
    require(not output.exists() and not output.is_relative_to(producer.ROOT) and
            not producer.ROOT.is_relative_to(output) and all(not output.is_relative_to(path) and
            not path.is_relative_to(output) for path in inputs), 'selected_seal_output_location')
    return output


def retained_files(folder: Path) -> set[str]:
    """Inventory safe retained files and reject symlinks and name collisions."""
    folder = absolute(folder)
    require(folder.is_dir(), 'selected_seal_retained_folder')
    files = set()
    for path in folder.rglob('*'):
        require(not path.is_symlink(), 'selected_seal_retained_path')
        name = transport.safe_name(path.relative_to(folder).as_posix())
        if path.is_dir():
            require(name in {'nuget', 'npm'}, 'selected_seal_retained_directory')
        else:
            require(path.is_file(), 'selected_seal_retained_file')
            files.add(name)
    transport.unique_names(list(files))
    return files


def freeze_inputs(plan_path: Path, plan_hash: str, retained: Path, producer_hash: str,
                  consumer_path: Path, consumer_hash: str) -> tuple[dict[str, bytes], dict[Path, bytes]]:
    """Freeze byte copies before parsing nested archives or producing any output."""
    plan_path, retained, consumer_path = (absolute(path) for path in (plan_path, retained, consumer_path))
    plan_data = transport.read_bound(plan_path, plan_hash, transport.MAX_PLAN_BYTES)
    producer_data = transport.read_bound(retained / 'receipt.json', producer_hash, transport.MAX_RECEIPT_BYTES)
    consumer_data = transport.read_bound(consumer_path, consumer_hash, transport.MAX_RECEIPT_BYTES)
    plan, receipt = transport.strict_json(plan_data, transport.MAX_PLAN_BYTES), transport.strict_json(producer_data)
    require(plan.get('product') in ('core', 'studio', 'extensions') and plan.get('line') in ('3.8', '3.9'),
            'selected_payload_cell_not_implemented')
    files = {'plan.json': plan_data, 'producer/receipt.json': producer_data, 'consumer/receipt.json': consumer_data}
    originals = {plan_path: plan_data, retained / 'receipt.json': producer_data, consumer_path: consumer_data}
    expected = {'receipt.json'} | ({'npm/receipt.json'} if plan['product'] == 'studio' else set())
    records = receipt['packages']['selected']
    archive_hashes = {'nuget/' + row['file']: row['sha256'] for row in records}
    for key in (('wasm', 'react') if plan['product'] == 'studio' else ()):
        row = receipt['npm'][key]
        name = 'npm/' + transport.safe_name(row['file'])
        require('/' not in row['file'] and name not in archive_hashes, 'selected_seal_archive_name')
        archive_hashes[name] = None  # SRI is checked against the frozen tar by the semantic validator.
    expected |= set(archive_hashes)
    require(retained_files(retained) == expected, 'selected_seal_retained_closure')
    total = sum(map(len, files.values()))
    for name in sorted(expected - {'receipt.json'}):
        target = 'producer/' + transport.safe_name(name)
        path, limit = retained / name, transport.file_limit(target)
        transport.safe_file(path)
        transport.integer(path.stat().st_size, limit)
        with path.open('rb') as stream:
            data = stream.read(limit + 1)
        require(len(data) <= limit, 'selected_seal_input_size')
        if name in archive_hashes and archive_hashes[name] is not None:
            require(metadata.sha256(data) == transport.digest(archive_hashes[name]), 'selected_seal_input_hash')
        total += len(data)
        require(total <= transport.MAX_EXPANDED_BYTES, 'selected_seal_expanded_size')
        files[target], originals[path] = data, data
    return files, originals


def make_manifest(files: dict[str, bytes], expected: dict, result: dict) -> dict:
    """Build a manifest binding the admitted files, executions, plan, and inherited judgments."""
    return {'schema': 1, 'scope': SCOPE, **expected, 'source': result['plan']['source'],
        'version': result['plan']['requested_version'], 'plan_sha256': metadata.sha256(files['plan.json']),
        'producer_receipt_sha256': metadata.sha256(files['producer/receipt.json']),
        'consumer_receipt_sha256': metadata.sha256(files['consumer/receipt.json']),
        'artifact_execution': result['producer']['execution'], 'consumer_execution': result['consumer']['execution'],
        'files': transport.inventory(files), 'inherited_judgments': inherited_judgments(result['plan']['product'])}


def stage(files: dict[str, bytes], expected: dict, hashes: tuple[str, str, str], *, contracts: dict,
          now: datetime | None = None) -> dict[str, bytes]:
    """Pure semantic seal; the returned mapping is the complete upload allowlist."""
    result = studio.validate(files, expected, *hashes, contracts=contracts, now=now)
    manifest = encoded(make_manifest(files, expected, result))
    require(len(manifest) <= transport.MAX_RECEIPT_BYTES, 'selected_seal_manifest_size')
    return files | {transport.MANIFEST: manifest}


def write_new(output: Path, files: dict[str, bytes]) -> None:
    """Write only an already validated closed mapping into a new directory."""
    transport.unique_names(list(files))
    output.mkdir(parents=True, exist_ok=False)
    try:
        for name, data in sorted(files.items()):
            path = output / transport.safe_name(name)
            path.parent.mkdir(parents=True, exist_ok=True)
            require(not any(part.is_symlink() for part in (path, *path.parents)), 'selected_seal_output_path')
            with path.open('xb') as stream:
                stream.write(data)
            require(path.read_bytes() == data, 'selected_seal_written_bytes')
    except BaseException:
        shutil.rmtree(output)
        raise


def seal(plan_path: Path, plan_hash: str, retained: Path, producer_hash: str,
         consumer_path: Path, consumer_hash: str, output: Path) -> dict:
    """Freeze inputs, validate their complete control evidence, and write a new sealed payload."""
    output = output_location(output, [absolute(plan_path).parent, absolute(retained), absolute(consumer_path).parent])
    files, originals = freeze_inputs(plan_path, plan_hash, retained, producer_hash, consumer_path, consumer_hash)
    plan = transport.strict_json(files['plan.json'], transport.MAX_PLAN_BYTES)
    controller = producer.verify_controller(producer.ROOT, plan)
    expected = {'context': hosted_context(controller), 'controller': controller, 'product': plan['product'], 'line': plan['line']}
    sealed = stage(files, expected, (plan_hash, producer_hash, consumer_hash), contracts=studio.load_contracts())
    require(all(transport.safe_file(path).read_bytes() == data for path, data in originals.items()),
            'selected_seal_input_changed')
    write_new(output, sealed)
    return expected | {'manifest_sha256': metadata.sha256(sealed[transport.MANIFEST])}


def readback(data: bytes, provider: dict, expected: dict, *, contracts: dict,
             now: datetime | None = None) -> dict:
    """Check original provider ZIP and repeat all semantic joins, without extraction."""
    now = now or datetime.now(timezone.utc)
    transport.validate_provider(provider, expected, now=now)
    require(len(data) == provider['archive_size'] and metadata.sha256(data) == provider['archive_sha256'],
            'selected_transport_outer_bytes')
    files = transport.zip_members(data)
    require(transport.MANIFEST in files and metadata.sha256(files[transport.MANIFEST]) == expected['manifest_sha256'],
            'selected_transport_manifest_bytes')
    manifest = transport.closed(transport.strict_json(files.pop(transport.MANIFEST)), MANIFEST_KEYS, 'selected_seal_manifest')
    require(type(manifest['schema']) is int and manifest['schema'] == 1 and manifest['scope'] == SCOPE and
            all(manifest[key] == expected[key] for key in ('context', 'controller', 'product', 'line')) and
            manifest['files'] == transport.inventory(files), 'selected_seal_manifest_binding')
    hashes = tuple(transport.digest(manifest[key]) for key in ('plan_sha256', 'producer_receipt_sha256', 'consumer_receipt_sha256'))
    context = {key: expected[key] for key in ('context', 'controller', 'product', 'line')}
    result = studio.validate(files, context, *hashes, contracts=contracts, now=now)
    require(manifest == make_manifest(files, context, result), 'selected_seal_manifest_semantics')
    return {'schema': 1, 'scope': 'independently-verified-original-provider-zip', 'success': True,
        'consumer_execution_scope': 'preupload-exact-local-archives', 'readback_scope': 'independently-verified-original-provider-zip',
        'provider': provider, **context, 'source': result['plan']['source'], 'version': result['plan']['requested_version'],
        'manifest_sha256': expected['manifest_sha256'], 'plan_sha256': hashes[0], 'producer_receipt_sha256': hashes[1],
        'consumer_receipt_sha256': hashes[2], 'artifact_execution_id': result['producer']['execution']['id'],
        'consumer_execution_id': result['consumer']['execution']['id'], 'files': manifest['files'],
        'inherited_judgments': inherited_judgments(result['plan']['product']), 'product_code_executed_during_readback': False,
        'current_provider_availability_requires_acceptance_recheck': True}


def main() -> int:
    """Run seal or independent readback and return a generic rejection on invalid inputs."""
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest='command', required=True)
    sealing = commands.add_parser('seal')
    for name in ('plan', 'producer-retained', 'consumer-receipt', 'output'):
        sealing.add_argument('--' + name, type=Path, required=True)
    for name in ('plan-sha256', 'producer-receipt-sha256', 'consumer-receipt-sha256'):
        sealing.add_argument('--' + name, required=True)
    reading = commands.add_parser('readback')
    for name in ('original-zip', 'provider', 'expected', 'output'):
        reading.add_argument('--' + name, type=Path, required=True)
    for name in ('archive-sha256', 'provider-sha256', 'expected-sha256'):
        reading.add_argument('--' + name, required=True)
    args = parser.parse_args()
    try:
        if args.command == 'seal':
            result = seal(args.plan, args.plan_sha256, args.producer_retained, args.producer_receipt_sha256,
                          args.consumer_receipt, args.consumer_receipt_sha256, args.output)
        else:
            require(not any(os.environ.get(key) for key in ('GH_TOKEN', 'GITHUB_TOKEN', 'ACTIONS_READ_TOKEN')),
                    'selected_readback_token_present')
            expected = transport.strict_json(transport.read_bound(absolute(args.expected), args.expected_sha256, transport.MAX_RECEIPT_BYTES))
            provider = transport.strict_json(transport.read_bound(absolute(args.provider), args.provider_sha256, transport.MAX_RECEIPT_BYTES))
            data = transport.read_bound(absolute(args.original_zip), args.archive_sha256, transport.MAX_ARCHIVE_BYTES)
            result = readback(data, provider, expected, contracts=studio.load_contracts())
            plan_controller = {'commit': metadata.git(producer.ROOT, 'rev-parse', 'HEAD'),
                               'tree': metadata.git(producer.ROOT, 'rev-parse', 'HEAD^{tree}')}
            require(not metadata.git(producer.ROOT, 'status', '--porcelain') and expected['controller'] == plan_controller,
                    'selected_readback_controller')
            output = output_location(args.output, [absolute(path).parent for path in (args.original_zip, args.provider, args.expected)])
            write_new(output, {'readback.json': encoded(result)})
        print(json.dumps(result if args.command == 'seal' else {'success': True, 'scope': result['scope']}, sort_keys=True))
        return 0
    except (ValueError, KeyError, TypeError, OSError, ET.ParseError):
        print('Selected seal/readback rejected inputs; no selected control acceptance emitted.')
        return 1


if __name__ == '__main__':
    raise SystemExit(main())
