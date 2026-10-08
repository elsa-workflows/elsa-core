"""Cheap policy contracts; fake dotnet/Docker observations are never runtime acceptance."""
import copy
import json
import os
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch
import xml.etree.ElementTree as ET

import run_admission_proof as proof

HEAD = 'a' * 40
HASH = 'b' * 64
CONTAINER = 'c' * 64
IMAGE = 'sha256:' + 'd' * 64
PROJECT = 'test/integration/Fixture/Fixture.csproj'
WORKER = 'test/workers/Worker/bin/Release/net10.0/Worker.dll'
HOSTED = {'GITHUB_REPOSITORY': 'elsa-workflows/elsa-core', 'GITHUB_RUN_ID': '123',
          'GITHUB_RUN_ATTEMPT': '1', 'GITHUB_EVENT_NAME': 'workflow_dispatch'}


def manifest():
    cases = []
    for family in proof.FAMILIES:
        case_id = 'case-' + family
        two_process = family == 'capacity'
        cases.append({'caseId': case_id, 'family': family, 'project': PROJECT,
                      'method': 'Elsa.Tests.Admission.' + family.title(), 'parameterId': case_id,
                      'assertions': ['durablePredicateVerified'],
                      'facts': {'count': {'kind': 'integer', 'minimum': 1, 'maximum': 1}},
                      'topology': 'postgresql-two-process' if two_process else 'in-process',
                      'processRoles': ['primary', 'competitor'] if two_process else [],
                      'minimumProcesses': 2 if two_process else 0, 'restartRequired': two_process})
    return {'schemaVersion': 1, 'complete': True, 'families': list(proof.FAMILIES),
            'testProjects': [{'project': PROJECT, 'filter': 'FullyQualifiedName~Admission', 'observations': True}],
            'buildProjects': ['src/Production/Production.csproj'],
            'workerAssemblies': {'primary': WORKER, 'competitor': WORKER}, 'cases': cases}


def observation(case, head=HEAD, assembly_hash=HASH):
    record = {'schemaVersion': 1, 'caseId': case['caseId'], 'method': case['method'],
              'parameterId': case['parameterId'], 'sourceRevision': head,
              'assertions': {'durablePredicateVerified': True}, 'facts': {'count': 1},
              'fixtureProcess': {'pid': 12, 'startIdentitySha256': HASH, 'assemblySha256': assembly_hash},
              'services': [], 'processes': [],
              'cleanup': {'ownedProcessesStopped': True, 'ownedListenersStopped': True}, 'liveProviderCalls': 0}
    if case['topology'] != 'in-process':
        record['services'] = [{'kind': 'postgresql', 'containerId': CONTAINER, 'imageId': IMAGE,
                               'databaseIdentitySha256': HASH, 'serverVersion': '160013'}]
    if case['topology'] == 'postgresql-two-process':
        record['processes'] = [{'pid': 20 + i, 'role': role, 'generation': i + 1,
                               'startIdentitySha256': HASH, 'assemblySha256': assembly_hash,
                               'exitCode': 0, 'exited': True} for i, role in enumerate(case['processRoles'])]
    return record


class AdmissionProofTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.directory = Path(temporary.name).resolve()
        self.manifest = manifest()

    def write_json(self, path, data):
        path.write_text(json.dumps(data))

    def write_trx(self, path, cases, *, outcome='Passed', duplicate=False, displays=None):
        rows = cases + cases[:1] if duplicate else cases
        root = ET.Element('TestRun')
        summary = ET.SubElement(root, 'ResultSummary')
        passed = len(rows) if outcome == 'Passed' else 0
        ET.SubElement(summary, 'Counters', total=str(len(rows)), executed=str(passed), passed=str(passed),
                      failed=str(len(rows) if outcome == 'Failed' else 0),
                      notExecuted=str(len(rows) if outcome == 'NotExecuted' else 0))
        definitions = ET.SubElement(root, 'TestDefinitions')
        results = ET.SubElement(root, 'Results')
        for index, case in enumerate(rows):
            test = ET.SubElement(definitions, 'UnitTest', id=str(index))
            class_name, method = case['method'].rsplit('.', 1)
            ET.SubElement(test, 'TestMethod', className=class_name, name=method)
            display = case['method'] if case['parameterId'] == 'default' else (
                f'{case["method"]}(caseId: "{case["parameterId"]}")')
            ET.SubElement(results, 'UnitTestResult', testId=str(index), outcome=outcome,
                          testName=displays[index] if displays else display)
        ET.ElementTree(root).write(path)

    def test_manifest_cannot_self_declare_complete_or_omit_families_and_topology(self):
        mutations = [lambda d: d.update(complete=False), lambda d: d.update(cases=[]),
                     lambda d: d['cases'].pop(), lambda d: d['cases'].append(copy.deepcopy(d['cases'][0])),
                     lambda d: d['cases'][0].update(project='test/Unknown.csproj'),
                     lambda d: d['cases'][0].update(facts={}),
                     lambda d: d['cases'][5].update(topology='postgresql-two-process')]
        for mutate in mutations:
            data = copy.deepcopy(self.manifest); mutate(data)
            with self.subTest(data=data), self.assertRaises(ValueError): proof.validate_manifest(data)
        proof.validate_manifest(self.manifest)
        labeled = copy.deepcopy(self.manifest)
        labeled['cases'][0]['parameterId'] = 'shared-parameter'
        labeled['cases'][1]['parameterId'] = 'shared-parameter'
        proof.validate_manifest(labeled)

    def test_trx_bijection_handles_real_named_theory_shape_and_unique_fact(self):
        cases = copy.deepcopy(self.manifest['cases'][:2]); cases[1]['parameterId'] = 'default'
        path = self.directory / 'result.trx'
        self.write_trx(path, cases)
        summary, identities = proof.trx_identities(path, 0)
        self.assertEqual('passed', summary['status'])
        self.assertEqual([(c['method'], c['parameterId']) for c in cases], identities)
        for display in (cases[0]['method'] + '(value: "case-identity")',
                        cases[0]['method'] + '(caseId: "case-identity", caseId: "case-admission")',
                        cases[0]['method'] + '(caseId: "private secret")'):
            self.write_trx(path, cases[:1], displays=[display])
            with self.subTest(display=display), self.assertRaises(ValueError): proof.trx_identities(path, 0)
        for outcome, rows, code, duplicate in (('Passed', [], 0, False), ('NotExecuted', cases, 0, False),
                ('Failed', cases, 1, False), ('Passed', cases, 1, False), ('Passed', cases, 0, True)):
            self.write_trx(path, rows, outcome=outcome, duplicate=duplicate)
            with self.subTest(outcome=outcome, duplicate=duplicate), self.assertRaises(ValueError):
                proof.trx_identities(path, code)
        data = copy.deepcopy(self.manifest)
        data['cases'][0]['parameterId'] = 'default'
        data['cases'][1]['method'] = data['cases'][0]['method']
        with self.assertRaises(ValueError): proof.validate_manifest(data)

    def test_single_process_runtime_can_use_postgresql_without_losing_service_evidence(self):
        data = copy.deepcopy(self.manifest)
        entry = next(c for c in data['cases'] if c['family'] == 'entry')
        entry['topology'] = 'postgresql'
        proof.validate_manifest(data)
        record = observation(entry)
        proof.validate_observation(record, entry, HEAD)
        record['services'] = []
        with self.assertRaises(ValueError): proof.validate_observation(record, entry, HEAD)
        entry.update(topology='postgresql-two-process', minimumProcesses=2,
                     processRoles=['primary', 'competitor'])
        # Other in-process provider cases cannot stand in for runtime entry proof.
        with self.assertRaises(ValueError): proof.validate_manifest(data)
        data = copy.deepcopy(self.manifest)
        for case in data['cases']:
            case.update(topology='postgresql', processRoles=[], minimumProcesses=0, restartRequired=False)
        with self.assertRaises(ValueError): proof.validate_manifest(data)

    def test_observations_reject_source_predicate_cleanup_service_and_process_mismatch(self):
        case = next(c for c in self.manifest['cases'] if c['topology'] == 'postgresql-two-process')
        mutations = [lambda d: d.update(sourceRevision='f' * 40), lambda d: d.update(parameterId='other'),
                     lambda d: d['facts'].update(count=True), lambda d: d['facts'].update(count=2),
                     lambda d: d['assertions'].update(durablePredicateVerified=False),
                     lambda d: d['cleanup'].update(ownedProcessesStopped=False),
                     lambda d: d.update(liveProviderCalls=1), lambda d: d.update(password='synthetic-secret'),
                     lambda d: d['services'][0].update(imageId='postgres:16'),
                     lambda d: d['processes'][1].update(pid=20),
                     lambda d: d['processes'][1].update(generation=1),
                     lambda d: d['processes'][1].update(exited=False),
                     lambda d: d['fixtureProcess'].update(assemblySha256='not-a-hash')]
        proof.validate_observation(observation(case), case, HEAD)
        for mutate in mutations:
            data = observation(case); mutate(data)
            with self.subTest(data=data), self.assertRaises(ValueError): proof.validate_observation(data, case, HEAD)
        hashes = {proof.assembly_path(PROJECT): HASH, WORKER: 'e' * 64}
        with self.assertRaises(ValueError): proof.bind_assemblies(observation(case), case, self.manifest, hashes)
        hashes[WORKER] = HASH; hashes[proof.assembly_path(PROJECT)] = 'e' * 64
        with self.assertRaises(ValueError): proof.bind_assemblies(observation(case), case, self.manifest, hashes)

    def test_evidence_layout_and_actual_fixture_disposal_cannot_be_replaced_by_case_flag(self):
        case = next(c for c in self.manifest['cases'] if c['topology'] == 'postgresql-two-process')
        evidence = self.directory / 'observations'; evidence.mkdir()
        self.write_json(evidence / (case['caseId'] + '.json'), observation(case))
        identities = [(case['method'], case['parameterId'])]
        hashes = {proof.assembly_path(PROJECT): HASH, WORKER: HASH}
        with self.assertRaises(ValueError): proof.validate_evidence(evidence, [case], identities, HEAD, self.manifest, hashes)
        fixture = {'schemaVersion': 1, 'sourceRevision': HEAD, 'disposedContainerIds': [CONTAINER]}
        self.write_json(evidence / 'fixture.json', fixture)
        with patch.object(proof.subprocess, 'check_output', return_value=CONTAINER + '\n'):
            with self.assertRaises(ValueError): proof.validate_evidence(evidence, [case], identities, HEAD, self.manifest, hashes)
        with patch.object(proof.subprocess, 'check_output', return_value=''), patch.object(proof, 'image_identity', return_value={}):
            records, cleanup = proof.validate_evidence(evidence, [case], identities, HEAD, self.manifest, hashes)
        self.assertTrue(cleanup['absenceVerified']); self.assertEqual(1, len(records))
        (evidence / 'private.log').write_text('synthetic-secret')
        with self.assertRaises(ValueError): proof.validate_evidence(evidence, [case], identities, HEAD, self.manifest, hashes)
        (evidence / 'private.log').unlink()
        with self.assertRaises(ValueError): proof.validate_evidence(evidence, [case], [], HEAD, self.manifest, hashes)
        (evidence / 'fixture.json').unlink(); (evidence / 'fixture.json').symlink_to(evidence / (case['caseId'] + '.json'))
        with self.assertRaises(ValueError): proof.validate_evidence(evidence, [case], identities, HEAD, self.manifest, hashes)

    def test_image_inspection_requires_observed_immutable_identity_and_platform(self):
        image = {'Id': IMAGE, 'Os': 'linux', 'Architecture': 'amd64',
                 'RepoDigests': ['postgres@sha256:' + HASH]}
        for change in ({}, {'Id': 'sha256:' + HASH}, {'Os': 'windows'}, {'RepoDigests': []}):
            row = {**image, **change}
            with patch.object(proof.subprocess, 'check_output', return_value=json.dumps([row])):
                if change:
                    with self.assertRaises(ValueError): proof.image_identity(IMAGE)
                else: self.assertEqual(IMAGE, proof.image_identity(IMAGE)['id'])

    def fake_run(self, *, fail_compile=False, mutate_source=False):
        repo = self.directory / 'repo'; repo.mkdir()
        (repo / '.gitignore').write_text('**/bin/\n**/obj/\n')
        path = repo / proof.MANIFEST; path.parent.mkdir(parents=True)
        self.write_json(path, self.manifest)
        for project in [*(p['project'] for p in self.manifest['testProjects']), *self.manifest['buildProjects']]:
            path = repo / project; path.parent.mkdir(parents=True, exist_ok=True); path.write_text('<Project />')
        subprocess.run(['git', 'init', '-q', str(repo)], check=True)
        subprocess.run(['git', '-C', str(repo), 'add', '.'], check=True)
        subprocess.run(['git', '-C', str(repo), '-c', 'user.name=Policy Test', '-c', 'user.email=policy@example.invalid',
                        'commit', '-qm', 'fixture'], check=True)
        head = subprocess.check_output(['git', '-C', str(repo), 'rev-parse', 'HEAD'], text=True).strip()
        commands = []
        def execute(command, root, log):
            commands.append(command)
            code = 1 if fail_compile and command[1] == 'build' else 0
            if command[1] == 'build' and command[2] == PROJECT:
                for relative in (proof.assembly_path(PROJECT), WORKER):
                    path = repo / relative; path.parent.mkdir(parents=True, exist_ok=True); path.write_bytes(b'fixture DLL')
            if command[1] == 'test':
                self.write_trx(log.parent / 'test-0.trx', self.manifest['cases'])
                evidence = Path(os.environ['ELSA_ADMISSION_PROOF_DIRECTORY'])
                assembly_hash = proof.digest(repo / WORKER)
                for case in self.manifest['cases']:
                    self.write_json(evidence / (case['caseId'] + '.json'), observation(case, head, assembly_hash))
                self.write_json(evidence / 'fixture.json', {'schemaVersion': 1, 'sourceRevision': head,
                                                          'disposedContainerIds': [CONTAINER]})
                if mutate_source: (repo / PROJECT).write_text('changed')
            return {'exitCode': code, 'status': 'failed' if code else 'passed', 'durationSeconds': 0.1,
                    'logSha256': HASH, 'compilerDiagnostics': []}
        # Only Docker is mocked: real Git verifies source cleanliness and pre/post hashes.
        original = proof.subprocess.check_output
        def output(command, *args, **kwargs):
            return '' if command[0] == 'docker' else original(command, *args, **kwargs)
        image = {'id': IMAGE, 'repoDigests': ['postgres@sha256:' + HASH], 'os': 'linux', 'architecture': 'amd64'}
        with patch.dict(os.environ, HOSTED), patch.object(proof, 'execute', side_effect=execute), \
                patch.object(proof.subprocess, 'check_output', side_effect=output), \
                patch.object(proof, 'image_identity', return_value=image):
            result = proof.run(repo, self.directory / 'output', head)
        return result, commands, repo

    def test_serial_orchestration_requires_exact_bijection_all_frameworks_and_post_source(self):
        receipt, commands, repo = self.fake_run()
        self.assertTrue(receipt['verificationComplete'])
        self.assertEqual(18, len(receipt['cases'])); self.assertEqual(3, len(receipt['builds']))
        self.assertEqual(['build', 'test', 'build', 'build', 'build'], [c[1] for c in commands])
        self.assertTrue(all('--no-build' in c and '--no-restore' in c for c in commands if c[1] == 'test'))
        self.assertEqual(1, len(receipt['serviceCleanup']))
        for mutate in (lambda d: d['cases'].pop(), lambda d: d['builds'].pop(),
                       lambda d: d.update(postSourceVerified=False), lambda d: d['tests'][0]['identities'].pop(),
                       lambda d: d['cases'][0].update(token='synthetic-access-token')):
            bad = copy.deepcopy(receipt); mutate(bad)
            with self.assertRaises(ValueError): proof.validate_receipt(bad, self.manifest, receipt['sourceRevision'])
        retained = self.directory / 'output/retained'
        proof.validate_retained(retained, self.manifest, receipt['sourceRevision'])
        (retained / 'extra.log').write_text('private')
        with self.assertRaises(ValueError): proof.validate_retained(retained, self.manifest, receipt['sourceRevision'])

    def test_compile_failure_collects_fixture_errors_without_tests_or_production_builds(self):
        self.manifest['testProjects'].append({'project': 'test/unit/Supplemental/Supplemental.csproj',
            'filter': 'FullyQualifiedName~Guards', 'observations': False})
        receipt, commands, _ = self.fake_run(fail_compile=True)
        self.assertFalse(receipt['verificationComplete'])
        self.assertEqual('incomplete_or_invalid_proof', receipt['failureCategory'])
        self.assertEqual(['build', 'build'], [c[1] for c in commands]); self.assertEqual([], receipt['tests'])
        self.assertTrue(all(c['status'] == 'not_run' for c in receipt['cases']))
        bad = copy.deepcopy(receipt); bad['verificationComplete'] = True
        with self.assertRaises(ValueError): proof.validate_receipt(bad, self.manifest, receipt['sourceRevision'])

    def test_source_mutation_after_tests_cannot_complete(self):
        receipt, _, _ = self.fake_run(mutate_source=True)
        self.assertFalse(receipt['verificationComplete']); self.assertFalse(receipt['postSourceVerified'])

    def test_environment_restored_and_duplicate_json_fields_rejected(self):
        with patch.dict(os.environ, {'ELSA_ADMISSION_SOURCE_REVISION': 'outer'}):
            with proof.proof_environment(self.directory, HEAD):
                self.assertEqual(HEAD, os.environ['ELSA_ADMISSION_SOURCE_REVISION'])
            self.assertEqual('outer', os.environ['ELSA_ADMISSION_SOURCE_REVISION'])
        path = self.directory / 'duplicate.json'; path.write_text('{"facts": {}, "facts": {"token": "private"}}')
        with self.assertRaises(ValueError): proof.read_json(path)

    def test_upload_boundary_rejects_private_marker_and_source_hash_replacement(self):
        receipt, _, repo = self.fake_run()
        retained = self.directory / 'output/retained'
        proof.validate_retained(retained, self.manifest, receipt['sourceRevision'], repo)
        receipt['inputSha256'][PROJECT] = 'f' * 64
        self.write_json(retained / 'receipt.json', receipt)
        with self.assertRaises(ValueError):
            proof.validate_retained(retained, self.manifest, receipt['sourceRevision'], repo)
        receipt['extra'] = 'synthetic-secret'
        self.write_json(retained / 'receipt.json', receipt)
        with self.assertRaisesRegex(ValueError, 'private_marker'):
            proof.validate_retained(retained, self.manifest, receipt['sourceRevision'])


if __name__ == '__main__': unittest.main()
