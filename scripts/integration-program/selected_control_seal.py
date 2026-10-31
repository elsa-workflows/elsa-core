#!/usr/bin/env python3
"""Seal and independently read back six selected-product control cells.

No package/native/product code executes here. The provider projection is an
input from the fixed retrieval job; this module does not fetch or invent it.
"""
from __future__ import annotations

import argparse
import binascii
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import shutil
import sys
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
FAILURE_SCOPE = 'selected-control-seal-rejection-only'
FAILURE_KEYS = {'schema', 'scope', 'phase', 'code', 'plan_sha256',
                'producer_receipt_sha256', 'consumer_receipt_sha256'}
FAILURE_PHASES = frozenset(('input-freeze', 'controller-verification', 'semantic-validation',
                            'input-recheck', 'output-write'))
FAILURE_REASON_CODES = {
    'selected_transport_size': 'input-size-rejected',
    'selected_seal_input_size': 'input-size-rejected',
    'selected_seal_expanded_size': 'input-size-rejected',
    'selected_seal_retained_closure': 'input-closure-rejected',
    'selected_seal_input_hash': 'input-hash-rejected',
}
# Static reviewed source guards only. New guards require explicit review here;
# neither prefixes nor artifact/exception values can extend this vocabulary.
SEMANTIC_FAILURE_GUARDS = frozenset("""
    selected_payload_archive_bijection selected_payload_archive_bytes selected_payload_archive_identity
    selected_payload_archive_inventory selected_payload_archive_missing selected_payload_archive_schema
    selected_payload_array selected_payload_asset_ambiguity selected_payload_asset_bytes selected_payload_asset_kind
    selected_payload_asset_schema selected_payload_cell_bijection selected_payload_cell_not_implemented
    selected_payload_cell_output_policy selected_payload_cell_package selected_payload_cell_schema
    selected_payload_cell_scope selected_payload_consumer_identity selected_payload_consumer_limitations
    selected_payload_consumer_schema selected_payload_consumer_source selected_payload_core_fixture_contract
    selected_payload_current_admission selected_payload_current_admission_schema selected_payload_exact_file_set
    selected_payload_expected_context selected_payload_expected_schema selected_payload_fixture_contract
    selected_payload_flat_build_identity selected_payload_flat_build_incoming selected_payload_flat_build_provenance
    selected_payload_flat_build_provenance_required selected_payload_flat_build_sdk
    selected_payload_flat_build_witness selected_payload_flat_build_witness_archive
    selected_payload_flat_build_witness_marker selected_payload_hashes_schema selected_payload_historical_admission
    selected_payload_input_hash selected_payload_inputs selected_payload_inventory_duplicate
    selected_payload_inventory_schema selected_payload_inventory_size selected_payload_isolation
    selected_payload_loaded_assembly_policy selected_payload_loaded_assembly_schema selected_payload_loaded_payload
    selected_payload_loaded_runtime_asset selected_payload_native_content_hash selected_payload_npm_archive_identity
    selected_payload_npm_archive_schema selected_payload_npm_command selected_payload_npm_command_schema
    selected_payload_npm_commands selected_payload_npm_consumer_failed selected_payload_npm_consumer_pair
    selected_payload_npm_consumer_schema selected_payload_npm_dist_bytes selected_payload_npm_entrypoint
    selected_payload_npm_generated_bytes selected_payload_npm_generated_collision selected_payload_npm_host_assets
    selected_payload_npm_identity selected_payload_npm_integrity selected_payload_npm_inventory
    selected_payload_npm_lifecycle selected_payload_npm_managed_assets selected_payload_npm_manifest_intent
    selected_payload_npm_original_metadata selected_payload_npm_schema selected_payload_npm_separate_receipt
    selected_payload_npm_source selected_payload_nuspec_dependencies selected_payload_nuspec_identity
    selected_payload_original_build_identity selected_payload_original_lib_group
    selected_payload_original_lib_identity selected_payload_package_id selected_payload_packages_schema
    selected_payload_placeholder selected_payload_placeholder_source selected_payload_preflight
    selected_payload_preflight_schema selected_payload_private_output_scope selected_payload_producer_identity
    selected_payload_producer_schema selected_payload_restored_duplicate selected_payload_restored_external_schema
    selected_payload_restored_external_source selected_payload_restored_object
    selected_payload_restored_payload_bijection selected_payload_restored_payload_identity
    selected_payload_restored_payloads_schema selected_payload_restored_root
    selected_payload_restored_selected_bytes selected_payload_restored_selected_schema
    selected_payload_runtime_contract selected_payload_runtime_description selected_payload_runtime_fixture
    selected_payload_runtime_representative selected_payload_runtime_schema selected_payload_sdk_content_hash
    selected_payload_sdk_download_identity selected_payload_sdk_download_schema
    selected_payload_sdk_download_version selected_payload_sdk_original_assets selected_payload_sdk_prune_range
    selected_payload_sdk_pruned_edge_schema selected_payload_sdk_pruned_parent selected_payload_sdk_pruned_scope
    selected_payload_sdk_restore_schema selected_payload_sdk_restore_scope selected_payload_size
    selected_payload_source_contract selected_payload_synthetic_build_group
    selected_payload_synthetic_build_identity selected_payload_synthetic_build_prerequisite
    selected_payload_synthetic_content_group selected_payload_synthetic_content_identity
    selected_payload_synthetic_content_schema selected_payload_test_bijection selected_payload_test_counters_schema
    selected_payload_test_counts selected_payload_test_execution_schema selected_payload_test_failed_or_skipped
    selected_payload_tests_schema selected_payload_text
    core_payload_assembly_identity core_payload_assembly_name core_payload_conditional_skip_gate
    core_payload_contracts_schema core_payload_counters_schema core_payload_declared_skips
    core_payload_dll_inventory core_payload_document_identity core_payload_document_schema core_payload_documents
    core_payload_embedded_document_schema core_payload_embedded_family core_payload_embedded_producer
    core_payload_excluded_identity core_payload_excluded_inventory core_payload_excluded_member_schema
    core_payload_excluded_output_schema core_payload_framework_tool_schema core_payload_generator_content
    core_payload_generator_contents core_payload_generator_hash core_payload_loaded
    core_payload_loaded_assembly_schema core_payload_loaded_duplicate core_payload_loaded_name
    core_payload_loaded_native_identity core_payload_loaded_package core_payload_loaded_required
    core_payload_loaded_restored core_payload_manifest_absent core_payload_manifest_bytes
    core_payload_manifest_catalog core_payload_manifest_hint_identity core_payload_manifest_hint_plan_restore
    core_payload_manifest_hint_producer_schema core_payload_manifest_hint_restore
    core_payload_manifest_hint_target_schema core_payload_manifest_hint_targets core_payload_manifest_identity
    core_payload_manifest_policy core_payload_manifest_required core_payload_manifest_schema core_payload_member
    core_payload_native_file_schema core_payload_native_files core_payload_native_frameworks
    core_payload_native_identity core_payload_native_inventory core_payload_native_nuspec core_payload_native_rows
    core_payload_native_schema core_payload_native_symbol_schema core_payload_npm core_payload_nuget_tool
    core_payload_nuget_tool_schema core_payload_nuspec core_payload_original_source core_payload_output_policy
    core_payload_pdb_archive core_payload_pdb_path core_payload_pdb_size core_payload_private_outputs
    core_payload_private_pdb core_payload_recipe core_payload_runtime core_payload_runtime_cells
    core_payload_runtime_contract core_payload_runtime_packages core_payload_runtime_result_schema
    core_payload_runtime_source core_payload_satellite_identity core_payload_satellite_schema
    core_payload_satellites core_payload_schema core_payload_sdk_asset_schema core_payload_sdk_assets
    core_payload_sdk_framework_schema core_payload_sdk_frameworks core_payload_sdk_inventory
    core_payload_sdk_producer_schema core_payload_sdk_tool_schema core_payload_skip_cell core_payload_skip_contract
    core_payload_skip_duplicate core_payload_skip_schema core_payload_skips core_payload_source_applicability
    core_payload_source_applicability_schema core_payload_sourcelink core_payload_swagger_dependency
    core_payload_swagger_identity core_payload_swagger_restore core_payload_symbol_checksum
    core_payload_symbol_identity core_payload_symbol_inventory core_payload_symbol_schema core_payload_symbols
    core_payload_symbols_partition core_payload_symbols_policy core_payload_test_cells core_payload_test_counts
    core_payload_test_duplicate core_payload_test_inventory core_payload_test_placeholders core_payload_test_rows
    core_payload_test_schema core_payload_tests_schema core_payload_text
    selected_payload_cell_not_implemented selected_plan_array_type selected_plan_artifact_exclusions
    selected_plan_artifact_partition selected_plan_boolean selected_plan_boolean_type
    selected_plan_controller_inputs selected_plan_core_observation selected_plan_exclusion
    selected_plan_feed_ineligible selected_plan_feed_inventory selected_plan_feed_service_inventory
    selected_plan_history_inventory selected_plan_ineligible selected_plan_integer selected_plan_integer_type
    selected_plan_inventory_identity selected_plan_inventory_partition selected_plan_maintenance39_source
    selected_plan_manifest_restore_hash selected_plan_map selected_plan_native_identity selected_plan_npm_scope
    selected_plan_null selected_plan_object_schema selected_plan_object_type selected_plan_observation_inventory
    selected_plan_observation_schema selected_plan_original_empty_url_scope selected_plan_original_net7_scope
    selected_plan_prerequisite_ineligible selected_plan_prerequisite_scope selected_plan_private_string
    selected_plan_registered_source selected_plan_scalar_type selected_plan_selected_scope
    selected_plan_source_policy selected_plan_string selected_plan_test_inventory selected_plan_url
    selected_plan_version
    selected_transport_archive_size selected_transport_bytes selected_transport_cell selected_transport_clock
    selected_transport_context_binding selected_transport_context_run selected_transport_context_schema
    selected_transport_controller_schema selected_transport_digest selected_transport_duplicate
    selected_transport_execution_binding selected_transport_execution_schema selected_transport_execution_uuid
    selected_transport_expanded_size selected_transport_expected_schema selected_transport_file
    selected_transport_json selected_transport_json_constant selected_transport_json_duplicate
    selected_transport_json_nonfinite selected_transport_json_object selected_transport_json_size
    selected_transport_manifest_binding selected_transport_manifest_bytes selected_transport_manifest_closure
    selected_transport_manifest_content selected_transport_manifest_duplicate
    selected_transport_manifest_entry_schema selected_transport_manifest_files selected_transport_manifest_schema
    selected_transport_member_count selected_transport_member_size selected_transport_member_type
    selected_transport_npm_metadata selected_transport_outer_bytes selected_transport_path
    selected_transport_path_collision selected_transport_planner_context selected_transport_provider_artifact
    selected_transport_provider_binding selected_transport_provider_expiry selected_transport_provider_run
    selected_transport_provider_schema selected_transport_size selected_transport_stage_identity
    selected_transport_tar selected_transport_tar_member selected_transport_tar_trailing_members
    selected_transport_time selected_transport_unlisted_path selected_transport_zip
""".split())
# Store the actual code objects and compare by identity, not filenames, function
# names or code-object equality (which can admit structurally equal clones).
SEMANTIC_FAILURE_SITES = tuple((function.__code__, site) for function, site in (
    (studio.sequence, 'shared-sequence'), (studio.hashes, 'shared-hashes'),
    (studio.text, 'shared-text'), (studio.package_id, 'shared-package-id'),
    (studio.inventory, 'shared-inventory'), (studio.archive_record, 'shared-archive'),
    (studio.validate_packages, 'shared-packages'), (studio.validate_tests, 'shared-tests'),
    (studio.validate_npm, 'shared-npm'), (studio.validate_sdk_restore, 'shared-sdk'),
    (studio.validate_cell, 'shared-cell'), (studio.validate_consumers, 'shared-consumers'),
    (studio.validate, 'shared-validate'), (studio.runtime_policy, 'shared-runtime-policy'),
    (studio.runtime_source, 'shared-runtime-source'), (studio.load_contracts, 'shared-contracts'),
    (studio.core_payload._rows, 'core-rows'), (studio.core_payload._text, 'core-text'),
    (studio.core_payload._hash, 'core-hash'), (studio.core_payload._member, 'core-member'),
    (studio.core_payload._source, 'core-source'), (studio.core_payload._skip_groups, 'core-skip-groups'),
    (studio.core_payload._tests, 'core-tests'), (studio.core_payload._producer, 'core-producer'),
    (studio.core_payload._manifest_hint_producer, 'core-manifest-hint'),
    (studio.core_payload._swagger_producer, 'core-swagger'),
    (studio.core_payload._documents, 'core-documents'), (studio.core_payload._symbols, 'core-symbols'),
    (studio.core_payload._manifest, 'core-manifest'), (studio.core_payload._excluded_outputs, 'core-excluded'),
    (studio.core_payload._native, 'core-native'), (studio.core_payload._runtime, 'core-runtime'),
    (studio.core_payload.validate_specialization, 'core-specialization'),
    (studio.core_payload.load_contracts, 'core-contracts'),
    (studio.plan_schema.public_url, 'plan-url'), (studio.plan_schema._walk, 'plan-walk'),
    (studio.plan_schema.validate, 'plan-validate'), (studio.plan_schema.admit, 'plan-admit'),
    (transport.closed, 'transport-schema'), (transport.digest, 'transport-digest'),
    (transport.integer, 'transport-integer'), (transport.safe_name, 'transport-name'),
    (transport.unique_names, 'transport-names'), (transport.strict_json, 'transport-json'),
    (transport.utc, 'transport-time'), (transport.zip_members, 'transport-zip'),
    (transport.tar_members, 'transport-tar'), (transport.file_limit, 'transport-file-limit'),
    (transport.inventory, 'transport-inventory'), (transport.archive_inventory, 'transport-archive'),
    (transport.validate_context, 'transport-context'), (transport.validate_execution, 'transport-execution'),
    (transport.validate_execution_pair, 'transport-execution-pair'),
))
SEMANTIC_EXCEPTION_TYPES = (
    (ValueError, 'value-error'), (KeyError, 'key-error'), (TypeError, 'type-error'),
    (AttributeError, 'attribute-error'), (IndexError, 'index-error'), (OverflowError, 'overflow-error'),
    (binascii.Error, 'base64-error'), (ET.ParseError, 'xml-parse-error'),
)
MAX_DIAGNOSTIC_TRACEBACK_FRAMES = 64
FAILURE_CODES = frozenset(('seal-rejected', *FAILURE_REASON_CODES.values(), *SEMANTIC_FAILURE_GUARDS,
    *(site + '-' + category for _, site in SEMANTIC_FAILURE_SITES for _, category in SEMANTIC_EXCEPTION_TYPES),
    'unclassified'))
MAX_FAILURE_RECORD_BYTES = 4096


class SealFailure(ValueError):
    """A seal rejection with only finite, non-sensitive diagnostic fields."""

    def __init__(self, phase: str, code: str):
        self.phase = phase
        self.code = code


def trusted_failure_site(error: Exception) -> str | None:
    """Return the innermost explicitly reviewed code-object site within a fixed bound."""
    trace, site = error.__traceback__, None
    for _ in range(MAX_DIAGNOSTIC_TRACEBACK_FRAMES):
        if trace is None:
            return site
        for code, candidate in SEMANTIC_FAILURE_SITES:
            if trace.tb_frame.f_code is code:
                site = candidate
                break
        trace = trace.tb_next
    return None


def semantic_exception_code(error: Exception) -> str | None:
    """Classify exact trusted exception types/sites without inspecting their values."""
    category = next((name for kind, name in SEMANTIC_EXCEPTION_TYPES if type(error) is kind), None)
    if category is None:
        return None
    if type(error) is ValueError and (len(error.args) != 1 or type(error.args[0]) is not str):
        return None
    site = trusted_failure_site(error)
    return site + '-' + category if site is not None else None


def rejection_code(error: Exception) -> str:
    """Emit only static guard identifiers or exact trusted site/type categories."""
    if type(error) is ValueError:
        if len(error.args) != 1 or type(error.args[0]) is not str:
            return 'unclassified'
        known = FAILURE_REASON_CODES.get(error.args[0])
        if known is not None:
            return known
        if error.args[0] == 'core_payload_schema' and trusted_failure_site(error) == 'core-specialization':
            # Inspect exactly one explicit cause from the real Core wrapper.
            # Never follow arbitrary cause/context chains or expose arguments.
            detail = semantic_exception_code(error.__cause__)
            if detail is not None:
                return detail
        if error.args[0] in SEMANTIC_FAILURE_GUARDS:
            return error.args[0]
    return semantic_exception_code(error) or 'unclassified'


def failure_record(phase: str, code: str, plan_hash: str, producer_hash: str,
                   consumer_hash: str) -> dict:
    """Return the closed safe CLI record; malformed caller values never pass through."""
    phase = phase if type(phase) is str and phase in FAILURE_PHASES else 'input-freeze'
    code = code if type(code) is str and code in FAILURE_CODES else 'unclassified'
    hashes = [value if type(value) is str and len(value) == 64 and
              all(char in '0123456789abcdef' for char in value) else None
              for value in (plan_hash, producer_hash, consumer_hash)]
    return {'schema': 1, 'scope': FAILURE_SCOPE, 'phase': phase, 'code': code,
            'plan_sha256': hashes[0], 'producer_receipt_sha256': hashes[1],
            'consumer_receipt_sha256': hashes[2]}


def failure_notice(phase: str, code: str) -> dict:
    """Return the value-free stderr notice used when the redirected record is unavailable."""
    phase = phase if type(phase) is str and phase in FAILURE_PHASES else 'input-freeze'
    code = code if type(code) is str and code in FAILURE_CODES else 'unclassified'
    return {'scope': FAILURE_SCOPE, 'phase': phase, 'code': code}


def read_failure_record(path: Path, plan_hash: str, producer_hash: str,
                        consumer_hash: str) -> tuple[dict, str]:
    """Read a bounded regular, no-follow failure record and bind it to exact inputs."""
    path = absolute(path)
    data = metadata.read_regular_file(path, maximum=MAX_FAILURE_RECORD_BYTES)
    record = transport.strict_json(data, MAX_FAILURE_RECORD_BYTES)
    transport.closed(record, FAILURE_KEYS, 'selected_seal_failure')
    require(type(record['schema']) is int and record['schema'] == 1 and record['scope'] == FAILURE_SCOPE and
            type(record['phase']) is str and record['phase'] in FAILURE_PHASES and
            type(record['code']) is str and record['code'] in FAILURE_CODES and
            record['plan_sha256'] == plan_hash and record['producer_receipt_sha256'] == producer_hash and
            record['consumer_receipt_sha256'] == consumer_hash, 'selected_seal_failure_binding')
    return {'phase': record['phase'], 'code': record['code']}, metadata.sha256(data)


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
    phase = 'input-freeze'
    try:
        output = output_location(output, [absolute(plan_path).parent, absolute(retained), absolute(consumer_path).parent])
        files, originals = freeze_inputs(plan_path, plan_hash, retained, producer_hash, consumer_path, consumer_hash)
        plan = transport.strict_json(files['plan.json'], transport.MAX_PLAN_BYTES)
        phase = 'controller-verification'
        controller = producer.verify_controller(producer.ROOT, plan)
        expected = {'context': hosted_context(controller), 'controller': controller,
                    'product': plan['product'], 'line': plan['line']}
        phase = 'semantic-validation'
        sealed = stage(files, expected, (plan_hash, producer_hash, consumer_hash), contracts=studio.load_contracts())
        phase = 'input-recheck'
        require(all(transport.safe_file(path).read_bytes() == data for path, data in originals.items()),
                'selected_seal_input_changed')
        phase = 'output-write'
        write_new(output, sealed)
        return expected | {'manifest_sha256': metadata.sha256(sealed[transport.MANIFEST])}
    except Exception as error:
        raise SealFailure(phase, rejection_code(error)) from None


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
    except SealFailure as error:
        if args.command == 'seal':
            print(json.dumps(failure_record(error.phase, error.code, args.plan_sha256,
                args.producer_receipt_sha256, args.consumer_receipt_sha256), sort_keys=True))
            print(json.dumps(failure_notice(error.phase, error.code), sort_keys=True), file=sys.stderr)
        else:
            print('Selected seal/readback rejected inputs; no selected control acceptance emitted.')
        return 1
    except (ValueError, KeyError, TypeError, OSError, ET.ParseError):
        print('Selected seal/readback rejected inputs; no selected control acceptance emitted.')
        return 1


if __name__ == '__main__':
    raise SystemExit(main())
