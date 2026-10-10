"""Synthetic package shapes and complete source-pinned test cells; no release proof."""
from copy import deepcopy
import base64
import io
from pathlib import PurePosixPath
import unittest
from unittest.mock import patch
import zipfile

import selected_core_payload as core


def archive_record(identifier, version, suffix, members):
    """Create package ZIP bytes and their exact archive inventory record."""
    stream = io.BytesIO()
    with zipfile.ZipFile(stream, 'w') as archive:
        for path, payload in members.items():
            archive.writestr(path, payload)
    data = stream.getvalue()
    return {'file': f'{identifier}.{version}.{suffix}', 'id': identifier, 'version': version,
            'sha256': core._hash(data), 'size': len(data), 'inventory': [
                {'path': p, 'size': len(b), 'sha256': core._hash(b)} for p, b in members.items()]}, data


def fixture(line='3.8', *, conditional_skips=True):
    """Build synthetic Core plan, producer, consumer, and archive evidence for a release line."""
    contracts = core.load_contracts()
    contract = contracts['producer']['sources'][line]
    source = {'product': 'core', 'line': line, 'kind': 'observed-core-release-branch',
              'commit': contract['commit'], 'tree': contract['tree']}
    plan = {'product': 'core', 'line': line, 'source': source, 'npm': None, 'requested_version': line + '.999',
            'inventory': {'release_recipe': {'solution': 'Elsa.sln', 'workflow': '.github/workflows/packages.yml'},
                'projects': [{'path': p, 'is_test_project': True} for p in contract['test_projects']], 'selected': [],
                'excluded': [{'id': 'Elsa.ModularServer.Web', 'project': 'src/apps/Elsa.ModularServer.Web/Elsa.ModularServer.Web.csproj',
                              'reason': 'source_nonpackable'}]}}
    producer = {'packages': {'selected': [], 'private_recipe_only_outputs': []}, 'package_verification': [],
                'product_tests': {'executions': [], 'inherited_skipped_placeholders': []}}
    skipped = {}
    for group in core._skip_groups(contract)[:None if conditional_skips else 1]:
        skipped.update(group)
    for project in contract['test_projects']:
        prefix = PurePosixPath(project).stem + '.'
        cases = [{'identity': k, 'reason': v} for k, v in skipped.items() if k.startswith(prefix)]
        counters = dict.fromkeys(core.COUNTERS, 0) | {'total': 2 + len(cases), 'passed': 2, 'executed': 2, 'notExecuted': len(cases)}
        producer['product_tests']['executions'].append({'project': project, 'framework': 'net10.0',
            'counters': counters, 'sha256': 'e'*64, 'expected_skips': cases})
    selected = {}
    for identifier in ('Elsa', 'Elsa.Workflows.Core', 'Elsa.Workflows.Runtime', 'Elsa.SamplePackage'):
        sample = identifier == 'Elsa.SamplePackage'
        frameworks = ['net10.0'] if sample else list(core.FRAMEWORKS)
        policy = {'id': identifier, 'project': core.SAMPLE_PROJECT if sample else f'src/modules/{identifier}/{identifier}.csproj',
                  'symbols': not sample, 'frameworks': frameworks, 'include_build_output': True,
                  'metadata': {'original_output_policy': {f: {'IncludeBuildOutput': 'true', 'GenerateElsaPackageManifest': '',
                    'ElsaPackageManifestIncludeInPackage': '', 'ElsaPackageManifestPackagePath': ''} for f in frameworks}}}
        plan['inventory']['selected'].append(policy)
        version = plan['requested_version']
        repository = {'type': 'git', 'url': 'https://github.com/elsa-workflows/elsa-core.git', 'commit': source['commit']}
        nuspec = (f'<package><metadata><id>{identifier}</id><version>{version}</version>'
                  f'<repository type="git" url="{repository["url"]}" commit="{source["commit"]}" />'
                  '</metadata></package>').encode()
        members = {identifier + '.nuspec': nuspec} | {f'lib/{f}/{identifier}.dll': (identifier + f).encode() for f in frameworks}
        symbol_members = {identifier + '.nuspec': nuspec} | {f'lib/{f}/{identifier}.pdb': (identifier + f + '.pdb').encode() for f in frameworks}
        record, data = archive_record(identifier, version, 'nupkg', members)
        producer['packages']['selected'].append(record)
        records = [record]
        if not sample:
            symbol_record, _ = archive_record(identifier, version, 'snupkg', symbol_members)
            producer['packages']['selected'].append(symbol_record); records.append(symbol_record)
        native = {'id': identifier, 'version': version, 'frameworks': frameworks, 'assembly_name': identifier,
                  'include_build_output': True, 'satellites': [], 'dependencies': [], 'repository': repository,
                  'symbols': [], 'package_manifest': None, 'sdk_assets': [], 'files': [
                      {'name': r['file'], 'sha256': r['sha256'], 'size': r['size']} for r in records]}
        for framework in frameworks:
            assembly, pdb = f'lib/{framework}/{identifier}.dll', f'lib/{framework}/{identifier}.pdb'
            checksum = core._hash(symbol_members[pdb])
            guid, name = '12345678-1234-1234-1234-123456789abc', identifier.lower() + '.pdb'
            native['symbols'].append({'assembly': assembly, 'assembly_sha256': core._hash(members[assembly]),
                'pdb': pdb, 'pdb_sha256': checksum, 'assembly_version': '1.0.1.0',
                'informational_version': '1.0.1+' + source['commit'], 'documents': [
                    {'path': f'src/modules/{identifier}/Source.cs', 'source': 'original-git',
                     'url': f'https://raw.githubusercontent.com/elsa-workflows/elsa-core/{source["commit"]}/src/modules/{identifier}/Source.cs',
                     'algorithm': 'sha256', 'checksum': 'f'*64}],
                'symbol': {'key': f"{name}/{guid.replace('-', '')}FFFFFFFF/{name}", 'pdb_name': name, 'guid': guid, 'stamp': 42,
                    'checksum_algorithm': 'SHA256', 'declared_checksum': 'a'*64, 'normalized_checksum': 'a'*64,
                    'pdb_sha256': checksum, 'pdb_size': len(symbol_members[pdb])}, **({'symbol_package': False} if sample else {})})
        producer['package_verification'].append(native)
        selected[identifier.casefold()] = {'record': record, 'members': members, 'policy': policy, 'data': data}
    consumer = {'runtime_contract_source': contracts['consumer']['sources'][line], 'runtime': []}
    for framework in core.FRAMEWORKS:
        assemblies, payloads = [], []
        for identifier in sorted(core.REQUIRED_ASSEMBLIES):
            item = selected[identifier.casefold()]
            path = f'lib/{framework}/{identifier}.dll'
            checksum = core._hash(item['members'][path])
            assemblies.append({'name': identifier, 'version': '1.0.1.0', 'informationalVersion': '1.0.1+' + source['commit'],
                'sha256': checksum, 'package_id': identifier, 'package_version': plan['requested_version'], 'package_asset': path})
            payloads.append({'id': identifier, 'version': plan['requested_version'], 'payloads': [
                {'path': path, 'kind': 'runtime', 'sha256': checksum, 'size': len(item['members'][path])}]})
        consumer['runtime'].append({'id': 'Elsa', 'framework': framework, 'restored_payloads': payloads,
            'runtime': {'contract': core.RUNTIME_DESCRIPTION, 'loaded_assemblies': assemblies}})
    return plan, producer, consumer, selected, contracts


class CorePayloadTests(unittest.TestCase):
    def validate(self, values):
        """Run pure Core specialization validation on the supplied fixture components."""
        plan, producer, consumer, selected, contracts = values
        core.validate_specialization(plan, producer, consumer, selected, contracts=contracts)

    def rejected(self, mutate, *, line='3.8'):
        """Mutate a fresh Core payload fixture and require validation to reject it."""
        values = fixture(line)
        mutate(*values)
        with self.assertRaises(ValueError):
            self.validate(values)

    def test_both_complete_source_test_censuses_and_provider_gate_states(self):
        """Verify both complete source test censuses and provider gate states."""
        for line, count in (('3.8', 44), ('3.9', 61)):
            for skips in (True, False):
                values = fixture(line, conditional_skips=skips)
                self.assertEqual(count, len(values[1]['product_tests']['executions']))
                self.validate(values)

    def test_pure_validation_does_not_load_files_or_environment(self):
        """Verify pure validation does not load files or environment."""
        values = fixture()
        with patch('pathlib.Path.read_bytes', side_effect=AssertionError('file read')), \
                patch.dict('os.environ', {'ELSA_USERTASKS_TEST_SQLSERVER': 'not-consulted'}, clear=True):
            self.validate(values)

    def test_source_plan_and_partition_tampering(self):
        """Verify source plan and partition tampering."""
        mutations = [lambda p, *_: p['source'].update(kind='maintenance'),
            lambda p, *_: p['source'].update(commit='0'*40), lambda p, *_: p.update(npm={}),
            lambda p, *_: p['inventory']['projects'].pop(),
            lambda p, *_: p['inventory']['selected'][0].update(symbols=False),
            lambda p, r, *_: r['packages']['private_recipe_only_outputs'].append({}),
            lambda p, r, *_: r['package_verification'].pop(),
            lambda p, r, *_: r.update(npm=None),
            lambda p, r, c, *_: c.update(npm=None)]
        for mutate in mutations:
            with self.subTest(mutation=mutate): self.rejected(mutate)

    def test_plan_bound_excluded_output_metadata_is_preserved_without_archive_bytes(self):
        """Verify plan bound excluded output metadata is preserved without archive bytes."""
        for line in ('3.8', '3.9'):
            values = fixture(line); plan, producer, _, selected, _ = values
            identifier, version = plan['inventory']['excluded'][0]['id'], plan['requested_version']
            members = {identifier + '.nuspec': b'<package />', 'lib/net10.0/' + identifier + '.dll': b'recipe-only'}
            record, _ = archive_record(identifier, version, 'nupkg', members)
            symbols, _ = archive_record(identifier, version, 'snupkg', {identifier + '.pdb': b'private-pdb'})
            producer['packages']['private_recipe_only_outputs'] = [record, symbols]
            before = deepcopy(values)
            with self.subTest(line=line): self.validate(values)
            self.assertEqual(before, values)
            self.assertNotIn(identifier.casefold(), selected)
            self.assertNotIn('data', record)
            self.assertNotIn('members', record)
            # The writer records actual safe filenames; it does not require an
            # excluded output to have the selected id.version naming convention.
            record['file'] = 'recipe-only.nupkg'
            self.validate(values)

    def test_excluded_metadata_rejects_unknown_identity_private_paths_and_bytes(self):
        """Verify excluded metadata rejects unknown identity private paths and bytes."""
        values = fixture(); plan, producer, _, _, _ = values
        record, _ = archive_record(plan['inventory']['excluded'][0]['id'], plan['requested_version'], 'nupkg',
                                   {'output.nuspec': b'<package />'})
        producer['packages']['private_recipe_only_outputs'] = [record]
        mutations = [lambda r: r.update(id='Unknown.Package'), lambda r: r.update(id='Elsa'),
            lambda r: r.update(version='3.8.1'), lambda r: r.update(file='/tmp/output.nupkg'),
            lambda r: r.update(file='../output.nupkg'), lambda r: r.update(file='output.log'),
            lambda r: r.update(file='Elsa.3.8.999.nupkg'),
            lambda r: r.update(data=b'excluded archive bytes'), lambda r: r.update(log='/tmp/private.log'),
            lambda r: r.update(cache_path='/tmp/cache'), lambda r: r.update(sha256='bad'),
            lambda r: r.update(size=True), lambda r: r.update(size=0),
            lambda r: r.update(size=core.MAX_PACKAGE_BYTES + 1), lambda r: r.update(inventory=[]),
            lambda r: r['inventory'][0].update(path='/tmp/private.dll'),
            lambda r: r['inventory'][0].update(path='../private.dll'),
            lambda r: r['inventory'][0].update(data=b'excluded member bytes'),
            lambda r: r['inventory'][0].update(size=-1),
            lambda r: r['inventory'][0].update(sha256='bad'),
            lambda r: r['inventory'].append(deepcopy(r['inventory'][0]))]
        for mutate in mutations:
            changed = deepcopy(values); mutate(changed[1]['packages']['private_recipe_only_outputs'][0])
            with self.subTest(mutation=mutate), self.assertRaises(ValueError): self.validate(changed)
        duplicate = deepcopy(values)
        duplicate[1]['packages']['private_recipe_only_outputs'].append(deepcopy(record))
        with self.assertRaises(ValueError): self.validate(duplicate)
        oversized = deepcopy(values)
        oversized[1]['packages']['private_recipe_only_outputs'][0]['inventory'] = [
            {'path': name, 'sha256': 'a'*64, 'size': core.MAX_PACKAGE_BYTES // 2 + 1}
            for name in ('a.nuspec', 'b.dll')]
        with self.assertRaises(ValueError): self.validate(oversized)
        leaked = deepcopy(values)
        leaked[3][record['id'].casefold()] = {'record': record, 'members': {'output.nuspec': b'<package />'},
                                           'policy': {'id': record['id']}, 'data': b'excluded archive bytes'}
        with self.assertRaises(ValueError): self.validate(leaked)

    def test_closed_nested_native_and_symbol_schema(self):
        """Verify closed nested native and symbol schema."""
        mutations = [lambda n: n.update(private_path='/tmp/secret'), lambda n: n['files'][0].update(argv=[]),
            lambda n: n['symbols'][0].update(extra=True), lambda n: n['symbols'][0]['symbol'].update(extra=True),
            lambda n: n['symbols'][0]['documents'][0].update(extra=True),
            lambda n: n['symbols'][0]['symbol'].update(pdb_size=True),
            lambda n: n['symbols'][0]['symbol'].update(guid='invalid'),
            lambda n: n['symbols'][0]['symbol'].update(normalized_checksum='b'*64),
            lambda n: n['symbols'][0].update(assembly_sha256='0'*64),
            lambda n: n['symbols'][0].update(informational_version='1.0.1+'+'0'*40),
            lambda n: n['symbols'][0].update(assembly_version='3.8.999'),
            lambda n: n['symbols'][0]['documents'][0].update(path='../Source.cs'),
            lambda n: n['symbols'][0]['documents'][0].update(source='core-git', remote_fetched=True)]
        for mutate in mutations:
            with self.subTest(mutation=mutate): self.rejected(lambda p, r, *_: mutate(r['package_verification'][0]))

    def test_missing_duplicate_dll_files_and_pdb_join(self):
        """Verify missing duplicate DLL files and PDB join."""
        mutations = [lambda n: n['symbols'].pop(), lambda n: n['symbols'].append(deepcopy(n['symbols'][0])),
            lambda n: n['symbols'][0].update(pdb_sha256='0'*64), lambda n: n['files'][0].update(size=999),
            lambda n: n.update(repository=n['repository'] | {'commit': '0'*40}),
            lambda n: n.update(dependencies=[{'framework': 'net10.0', 'dependencies': []}])]
        for mutate in mutations:
            with self.subTest(mutation=mutate): self.rejected(lambda p, r, *_: mutate(r['package_verification'][0]))
        self.rejected(lambda p, r, c, s, _: s['elsa']['members'].update({'lib/net10.0/Extra.dll': b'extra'}))

    def test_private_pdb_exception_is_exact_and_inherited(self):
        """Verify private PDB exception is exact and inherited."""
        values = fixture(); self.validate(values)
        self.rejected(lambda p, r, *_: r['package_verification'][-1]['symbols'][0].pop('symbol_package'))
        self.rejected(lambda p, r, *_: r['package_verification'][-1]['symbols'][0].update(symbol_package=True))
        self.rejected(lambda p, *_: p['inventory']['selected'][-1].update(project='src/Fake.csproj'))

    def test_no_documents_branch_has_exact_native_counts(self):
        """Verify no documents branch has exact native counts."""
        values = fixture()
        symbol = values[1]['package_verification'][0]['symbols'][0]
        symbol['documents'] = []
        symbol['source_applicability'] = {'classification': 'no-documents-no-executable-method-bodies',
            'document_count': 0, 'executable_method_bodies': 0, 'nonabstract_methods_without_body': 0,
            'native_or_external_methods': 0, 'nonmodule_types': 1, 'reference_assembly': False}
        self.validate(values)
        for key, value in (('document_count', 1), ('executable_method_bodies', True), ('nonmodule_types', 0), ('reference_assembly', True)):
            changed = deepcopy(values); changed[1]['package_verification'][0]['symbols'][0]['source_applicability'][key] = value
            with self.subTest(key=key), self.assertRaises(ValueError): self.validate(changed)

    def test_embedded_sdk_document_branch_is_closed(self):
        """Verify embedded SDK document branch is closed."""
        values = fixture()
        doc = {'path': '[embedded]/document-2', 'source': 'embedded', 'family': 'sdk', 'algorithm': 'sha256', 'checksum': 'f'*64,
            'producer': {'kind': 'sdk', 'sdk_version': '10.0.100', 'compiler_sha256': 'a'*64,
                'frameworks': [{'Identity': 'Microsoft.NETCore.App', 'TargetingPackName': 'Microsoft.NETCore.App.Ref', 'TargetingPackVersion': '10.0.0'}]}}
        values[1]['package_verification'][0]['symbols'][0]['documents'].append(doc)
        self.validate(values)
        doc['producer']['raw_path'] = '/tmp/sdk'
        with self.assertRaises(ValueError): self.validate(values)

    def test_generated_framework_nuget_and_razor_producer_branches(self):
        """Verify generated framework NuGet and razor producer branches."""
        common = {'kind': 'framework', 'package_id': 'Microsoft.NETCore.App.Ref',
            'package_version': '10.0.0', 'content_sha256': 'a'*64}
        nuget = common | {'kind': 'nuget', 'archive_sha256': 'b'*64,
            'restore_sha512': base64.b64encode(b'x'*64).decode(), 'nuget_content_hash': base64.b64encode(b'x'*64).decode(),
            'signed': True, 'archive_entry': 'analyzers/dotnet/cs/System.Text.RegularExpressions.Generator.dll',
            'targeting_pack': True, 'checked_archive_contents': {'analyzers/dotnet/cs/System.Text.RegularExpressions.Generator.dll': 'a'*64}}
        razor = {'kind': 'sdk', 'sdk_version': '10.0.300', 'content_sha256': 'a'*64, 'source_emitting_targets_sha256': 'b'*64}
        for family, producer in (('regex', common), ('regex', nuget), ('razor', razor)):
            values = fixture()
            document = {'path': '[embedded]/document-2', 'source': 'embedded', 'family': family,
                'algorithm': 'sha1', 'checksum': 'f'*40, 'producer': producer}
            values[1]['package_verification'][0]['symbols'][0]['documents'].append(document)
            with self.subTest(family=family, kind=producer['kind']): self.validate(values)
            changed = deepcopy(values)
            changed[1]['package_verification'][0]['symbols'][0]['documents'][-1]['producer']['cwd'] = '/tmp/private'
            with self.assertRaises(ValueError): self.validate(changed)
        nuget['nuget_content_hash'] = 'bad-base64'
        with self.assertRaises(ValueError): core._producer(nuget)

    def test_manifest_and_sdk_assets_bind_exact_archive_members(self):
        """Verify manifest and SDK assets bind exact archive members."""
        values = fixture(); plan, producer, _, selected, _ = values
        policy, native, members = plan['inventory']['selected'][0], producer['package_verification'][0], selected['elsa']['members']
        for output in policy['metadata']['original_output_policy'].values():
            output.update(GenerateElsaPackageManifest='true', ElsaPackageManifestIncludeInPackage='true', ElsaPackageManifestPackagePath='elsa-package.json')
        import json
        manifest = {'package': {'id': 'Elsa', 'version': plan['requested_version']},
            'extensions': {'targetFrameworks': list(core.FRAMEWORKS), 'repositoryUrl': 'https://github.com/elsa-workflows/elsa-core'}}
        members['elsa-package.json'] = json.dumps(manifest).encode()
        members['build/Elsa.targets'] = b'<Project />'
        native['package_manifest'] = {'path': 'elsa-package.json', 'sha256': core._hash(members['elsa-package.json']),
            'id': 'Elsa', 'version': plan['requested_version'], 'frameworks': list(core.FRAMEWORKS)}
        native['sdk_assets'] = [{'path': p, 'sha256': core._hash(members[p])} for p in ('elsa-package.json', 'build/Elsa.targets')]
        self.validate(values)
        mutations = [lambda n: n['package_manifest'].update(sha256='0'*64),
            lambda n: n['package_manifest'].update(private_path='/tmp/private'),
            lambda n: n['package_manifest'].update(selectable_features=[]), lambda n: n['sdk_assets'].pop(),
            lambda n: n['sdk_assets'][0].update(path='../elsa-package.json'),
            lambda n: n['sdk_assets'][0].update(sha256='0'*64)]
        for mutate in mutations:
            changed = deepcopy(values); mutate(changed[1]['package_verification'][0])
            with self.subTest(mutation=mutate), self.assertRaises(ValueError): self.validate(changed)

    def test_admission_manifest_conditional_catalog_and_empty_base(self):
        """Verify admission manifest conditional catalog and empty base."""
        import json
        for identifier, expected_types in core.archives.ADMISSION_SHELL_FEATURES.items():
            policy = {'id': identifier, 'frameworks': ['net10.0'], 'metadata': {'original_output_policy': {'net10.0': {
                'GenerateElsaPackageManifest': 'true', 'ElsaPackageManifestIncludeInPackage': 'true',
                'ElsaPackageManifestPackagePath': 'elsa-package.json'}}}}
            features = [{'id': identifier + '.Feature', 'typeName': name} for name in expected_types]
            data = {'schemaVersion': '1.0', 'package': {'id': identifier, 'version': '3.9.999'},
                'extensions': {'targetFrameworks': ['net10.0'], 'repositoryUrl': 'https://github.com/elsa-workflows/elsa-core'},
                'compatibility': {'runtimeKinds': ['elsa.server']}, 'features': features}
            members = {'elsa-package.json': json.dumps(data).encode()}; checksum = core._hash(members['elsa-package.json'])
            native = {'package_manifest': {'path': 'elsa-package.json', 'sha256': checksum, 'id': identifier,
                'version': '3.9.999', 'frameworks': ['net10.0'], 'selectable_features': [
                    {'id': f['id'], 'type_name': f['typeName']} for f in features], 'runtime_kinds': ['elsa.server']},
                'sdk_assets': [{'path': 'elsa-package.json', 'sha256': checksum}]}
            with self.subTest(identifier=identifier): core._manifest(native, policy, members, '3.9.999')
            native['package_manifest']['runtime_kinds'] = ['invented']
            with self.assertRaises(ValueError): core._manifest(native, policy, members, '3.9.999')

    def test_output_only_package_keeps_archive_without_native_assembly(self):
        """Verify output only package keeps archive without native assembly."""
        values = fixture(); plan, producer, _, selected, _ = values
        policy = plan['inventory']['selected'][-1]; native = producer['package_verification'][-1]; item = selected['elsa.samplepackage']
        policy['include_build_output'] = False
        policy['metadata']['original_output_policy']['net10.0']['IncludeBuildOutput'] = 'false'
        item['members'] = {k: v for k, v in item['members'].items() if not k.endswith('.dll')}
        native.update(include_build_output=False, frameworks=[], symbols=[])
        self.validate(values)
        native['frameworks'] = ['net10.0']
        with self.assertRaises(ValueError): self.validate(values)

    def test_satellite_exact_archive_join_and_safe_source_path(self):
        """Verify satellite exact archive join and safe source path."""
        values = fixture(); native = values[1]['package_verification'][0]; members = values[3]['elsa']['members']
        path = 'lib/net10.0/nl/Elsa.resources.dll'; members[path] = b'satellite'
        satellite = {'framework': 'net10.0', 'culture': 'nl', 'target_path': 'nl/Elsa.resources.dll',
            'final_output_path': 'src/modules/Elsa/bin/Release/net10.0/nl/Elsa.resources.dll',
            'package_path': path, 'sha256': core._hash(members[path])}
        native['satellites'].append(satellite); self.validate(values)
        for key, value in (('sha256', '0'*64), ('culture', '../nl'), ('final_output_path', '/tmp/Satellite.dll'), ('framework', 'net7.0')):
            changed = deepcopy(values); changed[1]['package_verification'][0]['satellites'][0][key] = value
            with self.subTest(key=key), self.assertRaises(ValueError): self.validate(changed)

    def test_source_bound_skips_counters_and_full_cells(self):
        """Verify source bound skips counters and full cells."""
        mutations = [lambda t: t['executions'].pop(), lambda t: t['executions'].append(deepcopy(t['executions'][0])),
            lambda t: t['executions'][0]['counters'].update(failed=1), lambda t: t['executions'][0]['counters'].update(passed=True),
            lambda t: t['executions'][0]['expected_skips'][0].update(reason='changed'),
            lambda t: t['executions'][0]['expected_skips'].pop(),
            lambda t: t['executions'][0]['expected_skips'].append({'identity': 'Unexpected.Skip', 'reason': 'unknown'}),
            lambda t: t.update(inherited_skipped_placeholders=[{}])]
        for mutate in mutations:
            with self.subTest(mutation=mutate): self.rejected(lambda p, r, *_: mutate(r['product_tests']))
        values = fixture('3.9')
        row = next(r for r in values[1]['product_tests']['executions'] if 'ConformanceTests' in r['project'] and r['expected_skips'])
        row['expected_skips'].pop(); row['counters']['total'] -= 1; row['counters']['notExecuted'] -= 1
        with self.assertRaisesRegex(ValueError, 'conditional_skip_gate'): self.validate(values)

    def test_loaded_asset_framework_comes_from_native_restored_selection(self):
        """Verify loaded asset framework comes from native restored selection."""
        values = fixture(); row = values[2]['runtime'][-1]
        loaded = next(r for r in row['runtime']['loaded_assemblies'] if r['name'] == 'Elsa.Workflows.Runtime')
        asset = 'lib/net9.0/Elsa.Workflows.Runtime.dll'
        loaded.update(package_asset=asset, sha256=core._hash(values[3]['elsa.workflows.runtime']['members'][asset]))
        payload = next(r for r in row['restored_payloads'] if r['id'] == 'Elsa.Workflows.Runtime')['payloads'][0]
        payload.update(path=asset, sha256=loaded['sha256'])
        self.validate(values)

    def test_runtime_source_asset_native_identity_and_closed_shape(self):
        """Verify runtime source asset native identity and closed shape."""
        mutations = [lambda c: c.update(runtime_contract_source={}), lambda c: c['runtime'].pop(),
            lambda c: c['runtime'][0]['runtime'].update(contract='different workflow'),
            lambda c: c['runtime'][0]['runtime'].update(outputLines=['invented']),
            lambda c: c['runtime'][0]['runtime']['loaded_assemblies'].pop(),
            lambda c: c['runtime'][0]['runtime']['loaded_assemblies'][0].update(version='3.8.999.0'),
            lambda c: c['runtime'][0]['runtime']['loaded_assemblies'][0].update(sha256='0'*64),
            lambda c: c['runtime'][0]['runtime']['loaded_assemblies'][0].update(package_asset='lib/net7.0/Elsa.dll'),
            lambda c: c['runtime'][0]['runtime']['loaded_assemblies'][0].update(location='/tmp/private'),
            lambda c: c['runtime'][0]['restored_payloads'][0]['payloads'][0].update(kind='compile')]
        for mutate in mutations:
            with self.subTest(mutation=mutate): self.rejected(lambda p, r, c, *_: mutate(c))


if __name__ == '__main__':
    unittest.main()
