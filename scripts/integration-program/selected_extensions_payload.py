"""Pure Extensions manifest specialization over already admitted archive bytes.

The original producer retains manifest/SDK projections, not Core native symbol
rows. Compiler, PDB and SourceLink checks remain inherited producer judgments.
"""
from __future__ import annotations

import hashlib
import io
import zipfile

import prove_consolidated_packages as archives
import selected_extensions_contract as source
import selected_control_transport as transport
from prove_consolidated_packages import require


def validate_specialization(plan: dict, producer: dict, selected: dict) -> None:
    """Validate Extensions manifest and SDK evidence against admitted archive bytes."""
    require(plan['product'] == 'extensions' and plan['line'] in ('3.8', '3.9') and
            'npm' not in producer and 'package_verification' not in producer, 'extensions_payload_scope')
    rows = producer['manifest_verification']
    require(type(rows) is list and len(rows) == len(selected), 'extensions_payload_manifest_inventory')
    seen = set()
    for row in rows:
        transport.closed(row, {'id', 'package_manifest', 'sdk_assets'}, 'extensions_payload_manifest_row')
        require(type(row['id']) is str and bool(row['id']), 'extensions_payload_package_id')
        folded = row['id'].casefold()
        require(folded in selected and folded not in seen and row['id'] == selected[folded]['policy']['id'],
                'extensions_payload_manifest_inventory')
        seen.add(folded)
        item = selected[folded]
        policy = item['policy']
        original = policy['metadata']['original_output_policy']
        projection = {'id': policy['id'], 'frameworks': policy['frameworks'], 'framework_properties': {
            framework: {'manifest_required': values['GenerateElsaPackageManifest'].lower() ==
                        values['ElsaPackageManifestIncludeInPackage'].lower() == 'true',
                        'manifest_path': values['ElsaPackageManifestPackagePath']} for framework, values in original.items()}}
        required = {p['manifest_path'] for p in projection['framework_properties'].values() if p['manifest_required']}
        require(len(required) <= 1, 'extensions_payload_manifest_policy')
        if policy['id'] == 'Elsa.IO.Http':
            source.apply_manifest_contract(projection, plan['source']['commit'])
        for path in required:
            transport.safe_name(path)
            require(path in item['members'], 'extensions_payload_manifest_missing')
            manifest = transport.strict_json(item['members'][path])
            require(type(manifest.get('package')) is dict and type(manifest.get('extensions')) is dict,
                    'extensions_payload_manifest_schema')
            if projection.get('manifest_expectation') or policy['id'] in archives.ADMISSION_SHELL_FEATURES:
                require(type(manifest.get('compatibility')) is dict, 'extensions_payload_manifest_schema')
            if projection.get('manifest_expectation'):
                features = manifest.get('features')
                require(type(features) is list and all(type(feature) is dict and
                        (feature.get('compatibility') is None or type(feature['compatibility']) is dict)
                        for feature in features), 'extensions_payload_manifest_schema')
        try:
            with zipfile.ZipFile(io.BytesIO(item['data'])) as archive:
                expected = archives.verify_package_manifest(archive, projection, plan['requested_version'], require_sdk_metadata=True)
        except (AttributeError, KeyError, TypeError, IndexError):
            raise ValueError('extensions_payload_manifest_schema') from None
        require(row['package_manifest'] == expected, 'extensions_payload_manifest_projection')
        assets = row['sdk_assets']
        require(type(assets) is list, 'extensions_payload_sdk_assets')
        paths = []
        for asset in assets:
            transport.closed(asset, {'path', 'sha256'}, 'extensions_payload_sdk_asset')
            path = transport.safe_name(asset['path'])
            transport.digest(asset['sha256'])
            require(path in item['members'] and hashlib.sha256(item['members'][path]).hexdigest() == asset['sha256'],
                    'extensions_payload_sdk_bytes')
            paths.append(path)
        transport.unique_names(paths)
        actual = {path for path in item['members'] if path.split('/')[0].casefold() in
                  {'build', 'buildtransitive', 'buildmultitargeting'} or path in required or path == 'elsa-package.json'}
        require(set(paths) == actual, 'extensions_payload_sdk_inventory')
    require(seen == set(selected) and {'elsa.io.http', 'elsa.io'} <= seen, 'extensions_payload_runtime_packages')
