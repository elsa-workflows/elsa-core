import copy
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import tempfile
from datetime import datetime, timezone, timedelta
from types import SimpleNamespace
import unittest
from unittest.mock import patch
import zipfile

sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'scripts'))
import release_train as train
import package_manifest
import release_notes
from release_support import parse_version


class TrainTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root=Path(self.temp.name)
        self.args=SimpleNamespace(version='3.9.0',kind=None,profile=train.DEFAULT_PROFILE,repositories=None,repos_root=str(self.root),pr=None,source=None,no_announcements=False,no_containers=True,state=self.root/'state.json')
        self.state=train.init(self.args)
        self.remote_existing=False

    def test_candidate_profile_cannot_initialize_publication_train(self):
        self.args.version = '3.10.0'
        self.args.profile = train.DEFAULT_PROFILE.with_name('consolidated-feedz-profile.json')
        with self.assertRaisesRegex(ValueError, 'candidate profiles cannot initialize'):
            train.init(self.args)

    def bind_fixture(self,name):
        path=self.root/name;path.mkdir(exist_ok=True)
        manifest=path/'manifest.json';notes=path/'notes.md';report=path/'report.json'
        train.save(manifest,{'version':'3.9.0','source_commit':'a'*40})
        notes.write_text('Release 3.9.0\n')
        train.save(report,{'verified':True,'source_commit':'a'*40,'version':'3.9.0'})
        self.state['repositories'][name].update(binding={'commit':'a'*40,'manifest':str(manifest),'notes':str(notes),'manifest_sha256':train.digest(manifest),'notes_sha256':train.digest(notes)},verification={'commit':'a'*40,'report':str(report),'report_sha256':train.digest(report)})

    def site_fixture(self, target, *, version='3.9.0', status='published'):
        timestamp = '2026-09-04T10:00:00+00:00'
        config = self.state['profile']['post_release_sites'][target]
        receipt = {
            'id': target + '-operation',
            'target': target,
            'status': status,
            'version': version,
            'scope': sorted(name for name, item in self.state['repositories'].items() if item['publish']),
            'changed_urls': [config['production_urls'][0] + '/release-notes'],
            'deployment_or_commit': target + '-deployment',
            'evidence_at': timestamp,
            'production_verification': {
                'verified': True,
                'url': config['production_urls'][0],
                'version': version,
                'evidence_at': timestamp,
            },
            'content_label': 'stable',
            'updates_current_stable': True,
            'replaces_latest_stable': True,
        }
        if target == 'website':
            receipt.update(project_id=config['project_id'], workspace_name=config['workspace_name'])
        else:
            receipt.update(repository=config['repository'], branch=config['branch'])
        path = self.root / f'{target}-receipt.json'
        train.save(path, receipt)
        self.state['post_refresh']['receipts'][target] = {
            'receipt': str(path), 'sha256': train.digest(path), 'id': receipt['id']
        }

    def container_fixture(self, state, *, version=None, image_names=None, source_commit='a' * 40, external_package_version=None, event='workflow_dispatch'):
        version = version or state['version']
        inventory = train.configured_container_release(state['profile'])
        image_names = image_names or [image['name'] for image in inventory['images']]
        images = [image for image in inventory['images'] if image['name'] in image_names]
        packages = {package for image in images for package in image['packages']}
        package_versions = {family: version for family in ('core', 'studio', 'extensions')}
        if external_package_version:
            package_versions['extensions'] = external_package_version
        digest_by_name = {}
        for index, image in enumerate(images, start=1):
            if not image.get('alias_of'):
                digest_by_name[image['name']] = 'sha256:' + f'{index:064x}'
        for image in images:
            if image.get('alias_of'):
                digest_by_name[image['name']] = digest_by_name[image['alias_of']]
        rows = []
        registry = {}
        resolved = {family: [] for family in packages}
        for image in images:
            package_versions_for_image = image['packages']
            source_image = next((candidate for candidate in inventory['images'] if candidate['name'] == image.get('alias_of')), image)
            image_resolved = {family: [] for family in package_versions_for_image}
            for family in package_versions_for_image:
                package_id = state['profile']['container_release']['package_probe_ids'][family][0]
                candidate = {'id': package_id, 'version': package_versions[family]}
                if candidate not in resolved[family]:
                    resolved[family].append(candidate)
                image_resolved[family].append(candidate)
            platform_digests = {
                platform: 'sha256:' + f'{len(rows) + platform_index + 100:064x}'
                for platform_index, platform in enumerate(image['platforms'], start=1)
            }
            if image.get('alias_of'):
                alias_source = next(row for row in rows if row['name'] == image['alias_of'])
                platform_digests = {item['platform']: item['digest'] for item in alias_source['platforms']}
            digest_value = digest_by_name[image['name']]
            row = {
                'name': image['name'],
                'repository': image['repository'],
                'tag': image['tag'].format(version=version),
                'sourceRef': source_image['repository'] + ':' + (version if event == 'release' else version + '-sha-' + source_commit),
                'digest': digest_value,
                'packageVersions': {family: package_versions[family] for family in package_versions_for_image},
                'resolvedPackages': image_resolved,
                'platforms': [{'platform': name, 'digest': value} for name, value in platform_digests.items()],
                'registryVerified': True,
                'smoke': {
                    'success': True,
                    'imageDigest': digest_value,
                    'platforms': [
                        {
                            'platform': platform,
                            'imageDigest': platform_digests[platform],
                            'status': 'success',
                            'endpoint': 'http://localhost/health',
                            'httpStatus': 200,
                            'dashboardApi': {
                                'runtimeStatus': 'AcceptingWork',
                                'isAcceptingWork': True,
                                'workflowMetricsValid': True,
                                'running': 0,
                            },
                        }
                        for platform in image['platforms']
                    ],
                },
            }
            rows.append(row)
            registry[(row['repository'], row['tag'])] = {'digest': digest_value, 'platforms': platform_digests}
        run_id = 44
        run_url = f"https://github.com/{inventory['repository']}/actions/runs/{run_id}"
        source_ref = f'refs/tags/{version}' if event == 'release' else 'refs/heads/main'
        workflow_inputs = {
            inventory['workflow_inputs']['version']: '' if event == 'release' else version,
            inventory['workflow_inputs']['publish']: '' if event == 'release' else True,
            inventory['workflow_inputs']['images']: '' if event == 'release' else ','.join(image['name'] for image in images if not image.get('alias_of')),
            inventory['workflow_inputs']['expected_commit']: '' if event == 'release' else source_commit,
        }
        selected_families = set(packages)
        for family in ('core', 'studio', 'extensions'):
            workflow_inputs[inventory['workflow_inputs'][family]] = '' if event == 'release' or family not in selected_families else package_versions[family]
        receipt = {
            'schemaVersion': 1,
            'releaseVersion': version,
            'appsRepository': inventory['repository'],
            'appsSource': {'ref': source_ref, 'commit': source_commit},
            'appsSourceCommit': source_commit,
            'workflowRun': {
                'id': run_id,
                'url': run_url,
                'repository': inventory['repository'],
                'workflow': inventory['workflow'],
                'runAttempt': 1,
                'event': event,
                'ref': source_ref,
                'headSha': source_commit,
                'conclusion': 'success',
            },
            'publication': 'published',
            'packageVersions': package_versions,
            'resolvedPackages': resolved,
            'workflowInputs': workflow_inputs,
            'registryVerifiedAt': datetime.now(timezone.utc).isoformat(),
            'images': rows,
            'smoke': {'success': True, 'results': [{'name': 'smoke', 'success': True}]},
        }
        receipt_path = self.root / f'container-receipt-{version}.json'
        train.save(receipt_path, receipt)
        archive_path = self.root / f'container-artifact-{version}.zip'
        with zipfile.ZipFile(archive_path, 'w', zipfile.ZIP_DEFLATED) as archive:
            archive.writestr('container-release-receipt.json', receipt_path.read_bytes())
        artifact = {
            'id': 123,
            'name': train.container_artifact_name(version, run_id, 1),
            'digest': 'sha256:' + train.digest(archive_path),
            'expired': False,
        }
        live_run = {
            'id': run_id,
            'run_attempt': 1,
            'html_url': run_url,
            'path': inventory['workflow'],
            'head_sha': source_commit,
            'head_branch': version if event == 'release' else 'main',
            'event': event,
            'status': 'completed',
            'conclusion': 'success',
            'repository': {'full_name': inventory['repository']},
        }
        return receipt, receipt_path, archive_path, artifact, live_run, registry

    def container_github(self, artifact, live_run, *args):
        url = args[-1]
        if f"/actions/runs/{live_run['id']}/artifacts?" in url:
            return [{'artifacts': [artifact]}]
        if url.endswith(f"/actions/runs/{live_run['id']}"):
            return live_run
        if '/compare/' in url:
            return {'status': 'identical'}
        return self.github(*args)

    def refresh_container_artifact(self, fixture):
        receipt, receipt_path, archive_path, artifact, *_ = fixture
        train.save(receipt_path, receipt)
        with zipfile.ZipFile(archive_path, 'w', zipfile.ZIP_DEFLATED) as archive:
            archive.writestr('container-release-receipt.json', receipt_path.read_bytes())
        artifact['digest'] = 'sha256:' + train.digest(archive_path)

    def validate_container_fixture(self, state, fixture, *, binding=None, image_names=None):
        receipt, receipt_path, archive_path, artifact, live_run, registry = fixture
        with patch.object(train, 'gh', side_effect=lambda *args: self.container_github(artifact, live_run, *args)), \
             patch.object(train, 'dockerhub_manifest', side_effect=lambda repository, tag: registry[(repository, tag)]), \
             patch.object(train, 'package_feed_available', return_value=True):
            return train.validate_container_receipt(
                state['profile'], state['version'], receipt,
                image_names=image_names, binding=binding,
                artifact_archive=archive_path, receipt_path=receipt_path,
            )

    def test_container_inventory_is_versioned_and_scoped_to_selected_repositories(self):
        profile = train.read(train.DEFAULT_PROFILE)
        core = train.make_container_state(profile, '3.9.0', ['core'], available_repositories=['core'])
        self.assertEqual(['server', 'server-alias'], core['images'])
        self.assertEqual({'core', 'extensions'}, set(core['package_versions']))
        studio = train.make_container_state(profile, '3.9.0', ['studio'], available_repositories=['core', 'studio'])
        self.assertEqual(6, len(studio['images']))
        self.assertEqual({'core', 'studio', 'extensions'}, set(studio['package_versions']))
        extensions = train.make_container_state(profile, '3.9.0', ['extensions'], available_repositories=['core', 'studio', 'extensions'])
        self.assertEqual(8, len(extensions['images']))
        templates = train.make_container_state(profile, '3.9.0', ['templates'], available_repositories=['core', 'studio'])
        self.assertFalse(templates['enabled'])
        self.assertEqual([], templates['images'])
        explicitly_disabled = train.make_container_state(profile, '3.9.0', None, no_containers=True)
        self.assertFalse(explicitly_disabled['enabled'])
        self.assertIn('explicitly disabled', explicitly_disabled['reason'])
        profile_without_container_images = copy.deepcopy(profile)
        profile_without_container_images.pop('container_release')
        disabled_custom_profile = train.make_container_state(profile_without_container_images, '3.9.0', no_containers=True)
        self.assertFalse(disabled_custom_profile['enabled'])
        self.assertIn('explicitly disabled', disabled_custom_profile['reason'])
        self.assertTrue(all(image['tag'] == '{version}' for image in profile['container_release']['images']))
        self.assertEqual(['server', 'server-alias'], train.expand_container_image_selection(profile, ['server']))
        self.assertEqual(['studio-wasm', 'studio-wasm-alias'], train.expand_container_image_selection(profile, ['studio-wasm']))
        with self.assertRaisesRegex(ValueError, 'Unknown configured container image'):
            train.expand_container_image_selection(profile, ['not-configured'])

    def test_container_receipt_matches_producer_artifact_live_registry_and_release_event(self):
        state = self.ready_container_state(no_containers=False)
        fixture = self.container_fixture(state)
        report = self.validate_container_fixture(state, fixture)
        self.assertTrue(report['verified'])
        self.assertEqual(8, len(report['images']))
        self.assertEqual(44, report['workflow_run_id'])

        release_fixture = self.container_fixture(state, event='release')
        release_report = self.validate_container_fixture(state, release_fixture)
        self.assertTrue(release_report['verified'])

    def test_container_receipt_accepts_runtime_dashboard_package_in_core_assets(self):
        state = self.ready_container_state(repositories=['core'], no_containers=False)
        fixture = self.container_fixture(state, image_names=state['containers']['images'])
        receipt = fixture[0]
        dashboard_package = {'id': 'Elsa.Workflows.Runtime.Dashboard', 'version': state['version']}
        receipt['resolvedPackages']['core'].append(dashboard_package)
        for image in receipt['images']:
            if image['name'] in ('server', 'server-alias'):
                image['resolvedPackages']['core'].append(dashboard_package)
        self.refresh_container_artifact(fixture)

        report = self.validate_container_fixture(state, fixture, image_names=state['containers']['images'])
        self.assertTrue(report['verified'])
        self.assertEqual(2, len(report['images']))

    def test_container_receipt_requires_authenticated_dashboard_runtime_semantics(self):
        state = self.ready_container_state(no_containers=False)
        mutations = (
            ('missing dashboard evidence', lambda row: row.pop('dashboardApi')),
            ('runtime not accepting work', lambda row: row['dashboardApi'].update(runtimeStatus='Unavailable')),
            ('runtime acceptance false', lambda row: row['dashboardApi'].update(isAcceptingWork=False)),
            ('workflow metrics invalid', lambda row: row['dashboardApi'].update(workflowMetricsValid=False)),
            ('invalid running count', lambda row: row['dashboardApi'].update(running=True)),
        )
        for label, mutate in mutations:
            with self.subTest(case=label):
                fixture = self.container_fixture(state)
                receipt = fixture[0]
                mutate(receipt['images'][0]['smoke']['platforms'][0])
                self.refresh_container_artifact(fixture)
                with self.assertRaisesRegex(ValueError, 'healthy authenticated dashboard runtime evidence'):
                    self.validate_container_fixture(state, fixture)

    def test_container_receipt_accepts_producer_decimal_run_id_and_rejects_malformed_values(self):
        state = self.ready_container_state(no_containers=False)
        fixture = self.container_fixture(state)
        receipt, _, _, artifact, live_run, _ = fixture
        run_id = 37736455508
        run_url = f"https://github.com/{state['profile']['container_release']['repository']}/actions/runs/{run_id}"
        receipt['workflowRun']['id'] = str(run_id)
        receipt['workflowRun']['url'] = run_url
        artifact['name'] = train.container_artifact_name(state['version'], run_id, 1)
        live_run.update(id=run_id, html_url=run_url)
        self.refresh_container_artifact(fixture)

        report = self.validate_container_fixture(state, fixture)
        self.assertEqual(run_id, report['workflow_run_id'])

        for malformed in ('037736455508', '37736455508x', '', ' 44', '+44', '0', '-44', '４４', True, 0, -37736455508):
            with self.subTest(value=malformed), self.assertRaisesRegex(ValueError, 'canonical decimal string workflowRun.id'):
                train.validate_container_receipt(state['profile'], state['version'], {**receipt, 'workflowRun': {**receipt['workflowRun'], 'id': malformed}})

        with self.assertRaisesRegex(ValueError, 'Recovery receipt requires positive integer'):
            train.positive_int(str(run_id), 'original_release_run.id')

    def test_forged_image_digest_and_receipt_bytes_do_not_verify(self):
        state = self.ready_container_state(no_containers=False)
        fixture = self.container_fixture(state)
        receipt, receipt_path, archive_path, artifact, live_run, registry = fixture
        receipt['images'][0]['digest'] = 'sha256:' + 'f' * 64
        receipt['images'][0]['smoke']['imageDigest'] = receipt['images'][0]['digest']
        self.refresh_container_artifact(fixture)
        with patch.object(train, 'gh', side_effect=lambda *args: self.container_github(artifact, live_run, *args)), \
             patch.object(train, 'dockerhub_manifest', side_effect=lambda repository, tag: registry[(repository, tag)]), \
             patch.object(train, 'package_feed_available', return_value=True), \
             self.assertRaisesRegex(ValueError, 'differs from the receipt'):
            train.validate_container_receipt(state['profile'], state['version'], receipt, artifact_archive=archive_path, receipt_path=receipt_path)

        receipt['images'][0]['digest'] = registry[(receipt['images'][0]['repository'], receipt['images'][0]['tag'])]['digest']
        receipt['images'][0]['smoke']['imageDigest'] = receipt['images'][0]['digest']
        platform = receipt['images'][0]['platforms'][0]['platform']
        receipt['images'][0]['platforms'][0]['digest'] = 'sha256:' + 'e' * 64
        receipt['images'][0]['smoke']['platforms'][0]['imageDigest'] = 'sha256:' + 'e' * 64
        self.refresh_container_artifact(fixture)
        with patch.object(train, 'gh', side_effect=lambda *args: self.container_github(artifact, live_run, *args)), \
             patch.object(train, 'dockerhub_manifest', side_effect=lambda repository, tag: registry[(repository, tag)]), \
             patch.object(train, 'package_feed_available', return_value=True), \
             self.assertRaisesRegex(ValueError, 'differs from the receipt'):
            train.validate_container_receipt(state['profile'], state['version'], receipt, artifact_archive=archive_path, receipt_path=receipt_path)

        receipt['images'][0]['platforms'][0]['digest'] = registry[(receipt['images'][0]['repository'], receipt['images'][0]['tag'])]['platforms'][platform]
        receipt['images'][0]['smoke']['platforms'][0]['imageDigest'] = receipt['images'][0]['platforms'][0]['digest']
        self.refresh_container_artifact(fixture)
        receipt_path.write_text(receipt_path.read_text() + ' ')
        with patch.object(train, 'gh', side_effect=lambda *args: self.container_github(artifact, live_run, *args)), \
             self.assertRaisesRegex(ValueError, 'differs from the exact successful-run artifact'):
            train.validate_container_receipt(state['profile'], state['version'], receipt, artifact_archive=archive_path, receipt_path=receipt_path)

    def test_full_release_cannot_complete_without_fresh_container_receipt(self):
        state = self.ready_container_state(no_containers=False)
        fixture = self.container_fixture(state)
        receipt, receipt_path, archive_path, artifact, live_run, registry = fixture
        receipt['workflowRun']['id'] = '44'
        self.refresh_container_artifact(fixture)
        selected = train.container_plan(state)
        state['containers']['binding'] = {
            'source_ref': 'main',
            'commit': 'a' * 40,
            'packages': dict(state['containers']['package_versions']),
            'images': [image['name'] for image in selected['images']],
        }
        state['containers']['dispatch'] = {'run_id': 44, 'source_commit': 'a' * 40}
        state_path = self.root / 'containers-state.json'
        train.save(state_path, state)
        with patch.object(train, 'gh', side_effect=lambda *args: self.container_github(artifact, live_run, *args)), \
             patch.object(train, 'dockerhub_manifest', side_effect=lambda repository, tag: registry[(repository, tag)]), \
             patch.object(train, 'package_feed_available', return_value=True):
            self.assertEqual('containers', train.status(state)['next'])
            args = SimpleNamespace(state=state_path, receipt=receipt_path, artifact_archive=archive_path, replace=False)
            result = train.record_containers(state, args)
            self.assertEqual(44, result['workflow_run_id'])
            self.assertEqual(44, state['containers']['receipt']['run_id'])
            self.assertEqual('complete', train.status(state)['next'])

            receipt_path.write_text(receipt_path.read_text() + ' ')
            self.assertEqual('containers', train.status(state)['next'])
            train.save(receipt_path, receipt)
            report_path = Path(state['containers']['verification']['report'])
            report = train.read(report_path)
            report['verified_at'] = (datetime.now(timezone.utc) - timedelta(hours=25)).isoformat()
            train.save(report_path, report)
            state['containers']['verification']['sha256'] = train.digest(report_path)
            self.assertEqual('containers', train.status(state)['next'])

    def test_record_containers_rejects_malformed_or_mismatched_run_id(self):
        state = self.ready_container_state(no_containers=False)
        fixture = self.container_fixture(state)
        receipt, receipt_path, archive_path, *_ = fixture
        selected = train.container_plan(state)
        state['containers']['binding'] = {
            'source_ref': 'main',
            'commit': 'a' * 40,
            'packages': dict(state['containers']['package_versions']),
            'images': [image['name'] for image in selected['images']],
        }
        state['containers']['dispatch'] = {'run_id': 44, 'source_commit': 'a' * 40}
        args = SimpleNamespace(state=self.root / 'malformed-run-id-state.json', receipt=receipt_path, artifact_archive=archive_path, replace=False)
        train.save(args.state, state)

        for value, message in (('044', 'canonical decimal string workflowRun.id'), (True, 'canonical decimal string workflowRun.id'), (-44, 'canonical decimal string workflowRun.id'), ('45', 'differs from the release checkpoint dispatch')):
            with self.subTest(value=value):
                receipt['workflowRun']['id'] = value
                train.save(receipt_path, receipt)
                with patch.object(train, 'status', return_value={'next': 'containers'}), self.assertRaisesRegex(ValueError, message):
                    train.record_containers(state, args)
                self.assertIsNone(state['containers'].get('receipt'))

    def test_core_subset_requires_external_extension_version_and_pins_dispatch_sha(self):
        state = self.ready_container_state(repositories=['core'], no_containers=False)
        commit = 'a' * 40

        def github(*args):
            url = args[-1]
            if url.endswith('/commits/main'):
                return {'sha': commit}
            return self.github(*args)

        args = SimpleNamespace(state=state['repositories']['core']['path'], source_ref=None, commit='a' * 40, package_version=[], replace=False)
        args.state = self.root / 'containers-core.json'
        train.save(args.state, state)
        with patch.object(train, 'gh', side_effect=github), self.assertRaisesRegex(ValueError, 'explicit published extensions package version'):
            train.bind_containers(state, args)

        args.package_version = ['extensions=3.8.4']
        with patch.object(train, 'gh', side_effect=github), patch.object(train, 'package_feed_available', return_value=True):
            binding = train.bind_containers(state, args)
        self.assertEqual({'core', 'extensions'}, set(binding['packages']))
        self.assertEqual('3.8.4', binding['packages']['extensions'])

        fixture = self.container_fixture(state, image_names=state['containers']['images'], external_package_version='3.8.4')
        report = self.validate_container_fixture(state, fixture, binding=binding, image_names=state['containers']['images'])
        self.assertEqual(2, len(report['images']))

        def reconcile(inventory, dispatch, binding):
            dispatch.update(run_id=44, url='https://github.com/run', run_attempt=1)
            return {'id': 44, 'html_url': 'https://github.com/run'}

        with patch.object(train, 'gh', side_effect=github), patch.object(train, 'command') as run_command, patch.object(train, 'reconcile_container_dispatch', side_effect=reconcile):
            result = train.dispatch_containers(state, args)
        self.assertEqual('wait-for-container-run', result['phase'])
        command_args = run_command.call_args.args[0]
        self.assertIn('--field', command_args)
        self.assertIn('expected_commit=' + commit, command_args)
        self.assertIn('images=server', command_args)

    def test_dispatch_clears_only_definite_github_rejections(self):
        failures = (
            ('rejected', ValueError('HTTP 422: workflow input is invalid'), False),
            ('timeout', subprocess.TimeoutExpired(['gh', 'workflow', 'run'], 120), True),
            ('eof', ValueError('unexpected EOF while reading response'), True),
        )
        for suffix, failure, pending in failures:
            with self.subTest(suffix=suffix):
                state = self.ready_container_state(repositories=['core'], no_containers=False)
                commit = 'a' * 40

                def github(*args):
                    if args[-1].endswith('/commits/main'):
                        return {'sha': commit}
                    return self.github(*args)

                args = SimpleNamespace(
                    state=self.root / f'dispatch-{suffix}.json', source_ref=None,
                    commit='a' * 40, package_version=['extensions=3.8.4'], replace=False,
                )
                train.save(args.state, state)
                with patch.object(train, 'gh', side_effect=github), patch.object(train, 'package_feed_available', return_value=True):
                    train.bind_containers(state, args)

                with patch.object(train, 'gh', side_effect=github), patch.object(train, 'command', side_effect=failure), self.assertRaises(type(failure)):
                    train.dispatch_containers(state, args)

                saved = train.read(args.state)['containers']['dispatch']
                if pending:
                    self.assertIsNotNone(saved)
                    self.assertEqual(commit, saved['source_commit'])
                else:
                    self.assertIsNone(saved)
                    self.assertIsNone(state['containers']['dispatch'])

    def test_container_source_requires_canonical_ref_and_main_ancestry_before_dispatch(self):
        state = self.ready_container_state(repositories=['core'], no_containers=False)
        args = SimpleNamespace(
            state=self.root / 'untrusted-apps-source.json', source_ref='feature/untrusted',
            commit='a' * 40, package_version=['extensions=3.8.4'], replace=False,
        )
        with patch.object(train, 'gh', side_effect=self.github), self.assertRaisesRegex(ValueError, 'canonical branch or the exact release-version tag'):
            train.bind_containers(state, args)

        commit = 'b' * 40
        state['containers']['package_versions']['extensions'] = '3.8.4'
        state['containers']['binding'] = {
            'source_ref': 'main',
            'commit': commit,
            'packages': {'core': '3.9.0', 'extensions': '3.8.4'},
            'images': state['containers']['images'],
        }
        train.save(args.state, state)

        def uncontained_github(*call_args):
            url = call_args[-1]
            if url.endswith('/commits/main'):
                return {'sha': commit}
            if '/compare/' in url:
                return {'status': 'behind'}
            return self.github(*call_args)

        with patch.object(train, 'gh', side_effect=uncontained_github), patch.object(train, 'command') as run_command, \
             self.assertRaisesRegex(ValueError, 'not in the canonical main branch history'):
            train.dispatch_containers(state, args)
        run_command.assert_not_called()
        self.assertIsNone(state['containers']['dispatch'])

    def test_container_source_accepts_canonical_branch_and_exact_version_tag(self):
        inventory = train.configured_container_release(self.state['profile'])
        commit = 'a' * 40

        def github(*args):
            url = args[-1]
            if '/commits/' in url:
                return {'sha': commit}
            if '/compare/' in url:
                return {'status': 'ahead'}
            raise AssertionError(f'Unexpected GitHub call {args}')

        with patch.object(train, 'gh', side_effect=github):
            self.assertEqual({'source_ref': 'main', 'commit': commit}, train.validate_container_source(inventory, 'main', '3.9.0'))
            self.assertEqual({'source_ref': '3.9.0', 'commit': commit}, train.validate_container_source(inventory, 'refs/tags/3.9.0', '3.9.0'))

    def test_bind_containers_cli_requires_full_reviewed_apps_commit(self):
        state_path = self.root / 'empty-state.json'
        train.save(state_path, {})
        script = str(Path(train.__file__))

        missing = subprocess.run(
            [sys.executable, script, '--state', str(state_path), 'bind-containers'],
            text=True, capture_output=True,
        )
        self.assertEqual(2, missing.returncode)
        self.assertIn('--commit', missing.stderr)

        malformed = subprocess.run(
            [sys.executable, script, '--state', str(state_path), 'bind-containers', '--commit', 'abc123'],
            text=True, capture_output=True,
        )
        self.assertEqual(1, malformed.returncode)
        self.assertIn('full 40-character SHA', malformed.stderr)

    def test_legacy_container_checkpoint_requires_explicit_adoption(self):
        old_profile = copy.deepcopy(self.state['profile'])
        old_profile.pop('container_release')
        self.assertTrue(train.compatible_profile(old_profile, self.state['profile']))
        self.state.pop('containers')
        self.state['profile'].pop('container_release')
        self.assertEqual('adopt-containers', train.status(self.state)['next'])
        adopted = train.adopt_containers(self.state, SimpleNamespace(no_containers=False))
        self.assertTrue(adopted['enabled'])
        self.assertEqual(8, len(adopted['images']))

    def test_prerelease_container_aliases_keep_exact_release_version_tags(self):
        args = SimpleNamespace(**{
            **vars(self.args), 'version': '3.9.0-rc1', 'kind': 'rc', 'no_containers': False,
            'no_post_refresh': True, 'state': self.root / 'rc-containers.json',
        })
        state = train.init(args)
        plan = train.container_plan(state)
        self.assertTrue(all(image['tag'] == '3.9.0-rc1' for image in plan['images']))
        self.assertEqual({'server', 'studio-wasm'}, {image['alias_of'] for image in plan['images'] if image['alias_of']})

    def ready_container_state(self, *, repositories=None, version='3.9.0', kind=None, no_containers=False):
        state_path = self.root / f'containers-{version}-{repositories or "all"}.json'
        args = SimpleNamespace(**{
            **vars(self.args),
            'version': version,
            'kind': kind,
            'repositories': repositories,
            'no_containers': no_containers,
            'no_announcements': True,
            'no_post_refresh': True,
            'state': state_path,
        })
        state = train.init(args)
        self.state = state
        for name in state['repositories']:
            self.bind_fixture(name)
        return state

    def github(self,*args):
        url=args[-1]
        if '/releases?' in url:
            name=next(r['name'] for r in self.state['profile']['repositories'] if r['github'] in url)
            if not self.remote_existing and 'binding' not in self.state['repositories'][name]:
                return [[]]
            return [[{'tag_name':'3.9.0','draft':False,'prerelease':False,'html_url':'https://github.com/release'}]]
        if '/git/ref/' in url:
            return {'object':{'type':'tag','sha':'tag-object'}}
        if '/git/tags/' in url:
            return {'object':{'type':'commit','sha':'a'*40}}
        if '/compare/' in url:
            return {'status':'identical'}
        if '/workflows/' in url:
            return [{'workflow_runs':[{'id':42,'head_sha':'a'*40,'head_branch':'3.9.0','event':'release','run_number':42,'run_attempt':1,'status':'completed','conclusion':'success','html_url':'https://github.com/run'}]}]
        if '/jobs?' in url:
            cfg=next(r for r in self.state['profile']['repositories'] if r['github'] in url)
            return [{'jobs':[{'name':n,'conclusion':'success'} for n in cfg['required_jobs']]}]
        self.fail(f'Unexpected GitHub call {args}')

    def prepare_template_recovery(self):
        self.bind_fixture('templates')
        self.state['repositories'] = {'templates': self.state['repositories']['templates']}
        self.state['repositories']['templates'].pop('verification', None)

    def template_recovery_receipt(self):
        artifact_digest = 'sha256:' + 'd' * 64
        evidence_digest = 'sha256:' + 'f' * 64
        return {
            'repository': 'elsa-workflows/elsa-templates',
            'version': '3.9.0',
            'tag': '3.9.0',
            'source_commit': 'a' * 40,
            'approved_source_commit': 'b' * 40,
            'original_release_run': {
                'id': 33977531328,
                'failed_jobs': ['Publish to nuget.org'],
            },
            'artifact': {
                'id': 777,
                'name': 'elsa-template-packages',
                'run_id': 33977531328,
                'digest': artifact_digest,
                'size_in_bytes': 1234,
            },
            'recovery_run': {
                'id': 33977531329,
                'event': 'workflow_dispatch',
                'publish_job': 'Publish to nuget.org',
                'artifact_id': 777,
                'artifact_digest': artifact_digest,
                'workflow_sha': 'e' * 40,
            },
            'evidence': {
                'id': 778,
                'name': 'elsa-template-recovery-evidence',
                'run_id': 33977531329,
                'digest': evidence_digest,
                'size_in_bytes': 567,
            },
            'target': {
                'registry': 'nuget.org',
                'package_ids': ['Elsa.Templates'],
                'version': '3.9.0',
            },
        }

    def template_recovery_evidence(self):
        return {
            'schema': 1,
            'repository': 'elsa-workflows/elsa-templates',
            'version': '3.9.0',
            'recovery_run_id': 33977531329,
            'recovery_workflow_sha': 'e' * 40,
            'original_release_run_id': 33977531328,
            'original_source_commit': 'a' * 40,
            'original_artifact': {
                'id': 777,
                'name': 'elsa-template-packages',
                'run_id': 33977531328,
                'digest': 'sha256:' + 'd' * 64,
                'size_in_bytes': 1234,
            },
            'target': {
                'registry': 'nuget.org',
                'package_ids': ['Elsa.Templates'],
                'version': '3.9.0',
            },
        }

    def template_recovery_files(self):
        evidence = self.template_recovery_evidence()
        archive = self.root / 'recovery-evidence.zip'
        payload = json.dumps(evidence, separators=(',', ':'), sort_keys=True).encode()
        with zipfile.ZipFile(archive, 'w', compression=zipfile.ZIP_DEFLATED) as output:
            output.writestr('recovery-receipt.json', payload)
        archive_digest = 'sha256:' + hashlib.sha256(archive.read_bytes()).hexdigest()
        self.recovery_evidence_digest = archive_digest
        self.recovery_evidence_size = archive.stat().st_size
        receipt = self.template_recovery_receipt()
        receipt['evidence']['digest'] = archive_digest
        receipt['evidence']['size_in_bytes'] = self.recovery_evidence_size
        return receipt, archive

    def recovery_github(self, *args):
        url = args[-1]
        if '/releases?' in url:
            return [[{'tag_name': '3.9.0', 'draft': False, 'prerelease': False, 'html_url': 'https://github.com/release'}]]
        if '/git/ref/' in url:
            return {'object': {'type': 'tag', 'sha': 'tag-object'}}
        if '/git/tags/' in url:
            return {'object': {'type': 'commit', 'sha': 'a' * 40}}
        if url == 'repos/elsa-workflows/elsa-templates':
            return {'full_name': 'elsa-workflows/elsa-templates', 'default_branch': 'main'}
        if '/compare/' in url:
            approved = url.split('/compare/', 1)[1].split('...', 1)[0]
            return {'status': 'identical' if approved == 'b' * 40 else 'behind'}
        if '/commits/' in url:
            sha = url.rsplit('/', 1)[-1]
            return {'sha': sha, 'commit': {'tree': {'sha': 'reviewed-tree'}}}
        if '/workflows/' in url:
            return [{'workflow_runs': [{
                'id': 33977531328,
                'head_sha': 'a' * 40,
                'head_branch': '3.9.0',
                'event': 'release',
                'run_number': 42,
                'run_attempt': 1,
                'status': 'completed',
                'conclusion': 'failure',
                'html_url': 'https://github.com/original-run',
            }]}]
        if url.endswith('/actions/runs/33977531328'):
            return {
                'id': 33977531328,
                'event': 'release',
                'status': 'completed',
                'conclusion': 'failure',
                'head_sha': 'a' * 40,
                'head_branch': '3.9.0',
            }
        if url.endswith('/actions/runs/33977531329'):
            return {
                'id': 33977531329,
                'event': 'workflow_dispatch',
                'status': 'completed',
                'conclusion': 'success',
                'head_sha': 'e' * 40,
            }
        if '/actions/runs/33977531328/jobs?' in url:
            return [{'jobs': [
                {'name': 'Build packages', 'conclusion': 'success'},
                {'name': 'Publish to feedz.io', 'conclusion': 'success'},
                {'name': 'Publish to nuget.org', 'conclusion': 'failure'},
            ]}]
        if '/actions/runs/33977531329/jobs?' in url:
            return [{'jobs': [
                {'name': 'Build packages', 'conclusion': 'skipped'},
                {'name': 'Publish to feedz.io', 'conclusion': 'skipped'},
                {'name': 'Publish to nuget.org', 'conclusion': 'success'},
            ]}]
        if '/actions/runs/33977531328/artifacts?' in url:
            return [{'artifacts': [{
                'id': 777,
                'name': 'elsa-template-packages',
                'size_in_bytes': 1234,
                'digest': 'sha256:' + 'd' * 64,
                'expired': False,
                'workflow_run': {'id': 33977531328},
            }]}]
        if '/actions/runs/33977531329/artifacts?' in url:
            return [{'artifacts': [{
                'id': 778,
                'name': 'elsa-template-recovery-evidence',
                'size_in_bytes': getattr(self, 'recovery_evidence_size', 567),
                'digest': getattr(self, 'recovery_evidence_digest', 'sha256:' + 'f' * 64),
                'expired': False,
                'workflow_run': {'id': 33977531329},
            }]}]
        raise AssertionError(f'Unhandled GitHub URL in recovery_github: {url}')

    def test_nuget_recovery_binds_failed_release_and_original_artifact(self):
        self.prepare_template_recovery()
        receipt = self.root / 'recovery.json'
        receipt_value, evidence_archive = self.template_recovery_files()
        train.save(receipt, receipt_value)
        with patch.object(train, 'gh', side_effect=self.recovery_github):
            value = train.record_recovery(self.state, SimpleNamespace(repo='templates', receipt=receipt, evidence_archive=evidence_archive))
            observed = train.inspect_release(self.state, 'templates')
        self.assertEqual(33977531328, value['original_run_id'])
        self.assertEqual(33977531329, value['recovery_run_id'])
        self.assertEqual('verify-packages', observed['phase'])
        self.assertEqual(33977531329, observed['recovery_run_id'])

    def test_nuget_recovery_rejects_non_nuget_original_failure(self):
        self.prepare_template_recovery()
        receipt = self.template_recovery_receipt()
        receipt['original_release_run']['failed_jobs'] = ['Build packages']
        evidence = self.template_recovery_evidence()
        with patch.object(train, 'gh', side_effect=self.recovery_github):
            with self.assertRaisesRegex(ValueError, 'failed NuGet publishing job'):
                train.validate_recovery_receipt(self.state, 'templates', receipt, evidence=evidence)

    def test_nuget_recovery_rejects_rebuild_in_recovery_run(self):
        self.prepare_template_recovery()
        receipt = self.template_recovery_receipt()
        evidence = self.template_recovery_evidence()

        def rebuilt(*args):
            value = self.recovery_github(*args)
            if '/actions/runs/33977531329/jobs?' in args[-1]:
                value[0]['jobs'][0]['conclusion'] = 'success'
            return value

        with patch.object(train, 'gh', side_effect=rebuilt):
            with self.assertRaisesRegex(ValueError, 'non-NuGet work'):
                train.validate_recovery_receipt(self.state, 'templates', receipt, evidence=evidence)

    def test_nuget_recovery_rejects_unreviewed_workflow_sha(self):
        self.prepare_template_recovery()
        receipt = self.template_recovery_receipt()
        receipt['recovery_run']['workflow_sha'] = 'a' * 40
        with patch.object(train, 'gh', side_effect=self.recovery_github):
            with self.assertRaisesRegex(ValueError, 'reviewed recovery workflow SHA'):
                train.validate_recovery_receipt(self.state, 'templates', receipt, evidence=self.template_recovery_evidence())

    def test_nuget_recovery_rejects_forged_machine_evidence_linkage(self):
        self.prepare_template_recovery()
        receipt = self.template_recovery_receipt()
        evidence = self.template_recovery_evidence()
        evidence['original_artifact']['digest'] = 'sha256:' + '0' * 64
        with patch.object(train, 'gh', side_effect=self.recovery_github):
            with self.assertRaisesRegex(ValueError, 'does not bind the original artifact payload'):
                train.validate_recovery_receipt(self.state, 'templates', receipt, evidence=evidence)

    def test_nuget_recovery_rejects_unanchored_approved_source(self):
        self.prepare_template_recovery()
        receipt = self.template_recovery_receipt()
        receipt['approved_source_commit'] = 'c' * 40
        with patch.object(train, 'gh', side_effect=self.recovery_github):
            with self.assertRaisesRegex(ValueError, 'canonical default-branch history'):
                train.validate_recovery_receipt(self.state, 'templates', receipt, evidence=self.template_recovery_evidence())

    def test_nuget_recovery_rejects_approved_tree_mismatch(self):
        self.prepare_template_recovery()
        receipt = self.template_recovery_receipt()

        def mismatched(*args):
            value = self.recovery_github(*args)
            if '/commits/e' + 'e' * 39 in args[-1]:
                value['commit']['tree']['sha'] = 'different-tree'
            return value

        with patch.object(train, 'gh', side_effect=mismatched):
            with self.assertRaisesRegex(ValueError, 'tree differs'):
                train.validate_recovery_receipt(self.state, 'templates', receipt, evidence=self.template_recovery_evidence())

    def test_nuget_recovery_rejects_extra_failed_original_job(self):
        self.prepare_template_recovery()
        receipt = self.template_recovery_receipt()

        def extra_failure(*args):
            value = self.recovery_github(*args)
            if '/actions/runs/33977531328/jobs?' in args[-1]:
                value[0]['jobs'].append({'name': 'Upload diagnostics', 'conclusion': 'failure'})
            return value

        with patch.object(train, 'gh', side_effect=extra_failure):
            with self.assertRaisesRegex(ValueError, 'unknown non-success job'):
                train.validate_recovery_receipt(self.state, 'templates', receipt, evidence=self.template_recovery_evidence())

    def test_nuget_recovery_rejects_renamed_rebuild_job(self):
        self.prepare_template_recovery()
        receipt = self.template_recovery_receipt()

        def renamed(*args):
            value = self.recovery_github(*args)
            if '/actions/runs/33977531329/jobs?' in args[-1]:
                value[0]['jobs'][0]['name'] = 'Build packages (rebuild)'
            return value

        with patch.object(train, 'gh', side_effect=renamed):
            with self.assertRaisesRegex(ValueError, 'exactly the configured required jobs'):
                train.validate_recovery_receipt(self.state, 'templates', receipt, evidence=self.template_recovery_evidence())

    def test_nuget_recovery_rejects_duplicate_job_names(self):
        self.prepare_template_recovery()
        receipt = self.template_recovery_receipt()

        def duplicate(*args):
            value = self.recovery_github(*args)
            if '/actions/runs/33977531329/jobs?' in args[-1]:
                value[0]['jobs'].append({'name': 'Build packages', 'conclusion': 'skipped'})
            return value

        with patch.object(train, 'gh', side_effect=duplicate):
            with self.assertRaisesRegex(ValueError, 'duplicate job'):
                train.validate_recovery_receipt(self.state, 'templates', receipt, evidence=self.template_recovery_evidence())

    def test_nuget_recovery_rejects_tampered_evidence_archive(self):
        self.prepare_template_recovery()
        receipt_value, evidence_archive = self.template_recovery_files()
        receipt = self.root / 'recovery.json'
        train.save(receipt, receipt_value)
        evidence_archive.write_bytes(evidence_archive.read_bytes() + b'tampered')
        with patch.object(train, 'gh', side_effect=self.recovery_github):
            with self.assertRaisesRegex(ValueError, 'archive hash differs'):
                train.record_recovery(self.state, SimpleNamespace(repo='templates', receipt=receipt, evidence_archive=evidence_archive))

    def test_unnumbered_rc_and_preview_need_a_resolved_version(self):
        for value in ['3.9.0-rc','3.9.0-preview']:
            with self.assertRaisesRegex(ValueError,'explicit unused'):
                parse_version(value)

    def test_init_resumes_without_erasing_progress_and_rejects_scope_change(self):
        self.state['repositories']['core']['marker']='preserve'
        self.state['post_refresh']['receipts']['website']={'id':'preserve'}
        train.save(self.args.state,self.state)
        self.assertEqual('preserve',train.init(self.args)['repositories']['core']['marker'])
        self.assertEqual('preserve',train.init(self.args)['post_refresh']['receipts']['website']['id'])
        self.args.no_announcements=True
        with self.assertRaisesRegex(ValueError,'different announce'):
            train.init(self.args)

    def test_init_preserves_a_legacy_three_repository_checkpoint_after_templates_is_added(self):
        legacy = copy.deepcopy(self.state)
        legacy['profile']['repositories'] = [
            repository for repository in legacy['profile']['repositories'] if repository['name'] != 'templates'
        ]
        legacy['repositories'].pop('templates')
        train.save(self.args.state, legacy)
        resumed = train.init(self.args)
        self.assertNotIn('templates', resumed['repositories'])
        self.assertEqual(legacy['profile'], resumed['profile'])

    def test_init_rejects_changed_implicit_paths_or_explicit_sources(self):
        changed_root = self.root / 'other-root'
        changed = SimpleNamespace(**{**vars(self.args), 'repos_root': str(changed_root)})
        with self.assertRaisesRegex(ValueError, 'repository paths'):
            train.init(changed)
        changed_source = SimpleNamespace(**{**vars(self.args), 'source': ['core=origin/main']})
        with self.assertRaisesRegex(ValueError, 'repository scope'):
            train.init(changed_source)

    def test_wrong_release_line_source_is_rejected_before_checkpoint(self):
        self.args.state=self.root/'wrong-source.json';self.args.source=['core=3.8.0-rc1']
        with self.assertRaisesRegex(ValueError,'different release line'):
            train.init(self.args)
        self.assertFalse(self.args.state.exists())

    def test_fresh_state_adopts_existing_tag_instead_of_republishing(self):
        self.remote_existing=True
        with patch.object(train,'gh',side_effect=self.github):
            observed=train.status(self.state)
        self.assertEqual('adopt-existing',observed['repositories']['core']['phase'])
        self.assertEqual('a'*40,observed['repositories']['core']['commit'])
        self.assertEqual('wait-for-upstream',observed['repositories']['studio']['phase'])

    def test_subset_adds_verification_only_upstreams(self):
        self.args.state=self.root/'subset.json';self.args.repositories=['extensions']
        state=train.init(self.args)
        self.assertFalse(state['repositories']['core']['publish'])
        self.assertFalse(state['repositories']['studio']['publish'])
        self.assertTrue(state['repositories']['extensions']['publish'])

    def test_templates_are_fourth_stage_with_core_and_studio_upstreams(self):
        self.args.state = self.root / 'templates.json'
        self.args.repositories = ['templates']
        state = train.init(self.args)
        self.assertEqual(['core', 'studio', 'templates'], list(state['repositories']))
        self.assertFalse(state['repositories']['core']['publish'])
        self.assertFalse(state['repositories']['studio']['publish'])
        self.assertTrue(state['repositories']['templates']['publish'])
        self.assertEqual('origin/main', state['repositories']['templates']['source_ref'])

        self.args.state = self.root / 'templates-preview.json'
        self.args.version = '3.9.0-preview.1'
        self.args.kind = 'preview'
        self.args.repositories = ['templates']
        preview = train.init(self.args)
        self.assertEqual('origin/release/3.9.0', preview['repositories']['templates']['source_ref'])

    def test_templates_can_select_an_explicit_preview_source(self):
        self.args.state = self.root / 'templates-source.json'
        self.args.repositories = ['templates']
        self.args.source = ['templates=origin/3.9.0-preview.2']
        state = train.init(self.args)
        self.assertEqual('origin/3.9.0-preview.2', state['repositories']['templates']['source_ref'])

    def test_full_train_keeps_templates_after_extensions_even_without_an_extension_reference(self):
        self.bind_fixture('core')
        self.bind_fixture('studio')
        with patch.object(train, 'gh', side_effect=self.github):
            observed = train.status(self.state)
        self.assertEqual('wait-for-stage', observed['repositories']['templates']['phase'])
        self.assertEqual(['extensions'], observed['repositories']['templates']['stages'])

    def test_dependency_order_and_tampered_receipt_blocks_progress(self):
        with patch.object(train,'gh',side_effect=self.github):
            first=train.status(self.state)
            self.assertEqual('prepare',first['repositories']['core']['phase'])
            self.assertEqual('wait-for-upstream',first['repositories']['studio']['phase'])
            self.bind_fixture('core')
            second=train.status(self.state)
            self.assertEqual('prepare',second['repositories']['studio']['phase'])
            self.bind_fixture('studio');self.bind_fixture('extensions');self.bind_fixture('templates')
            self.site_fixture('website');self.site_fixture('documentation')
            self.assertEqual('announcements',train.status(self.state)['next'])
            binding=self.state['repositories']['core']['binding']
            Path(binding['manifest']).write_text('{}')
            self.assertEqual('verify-packages',train.status(self.state)['repositories']['core']['phase'])
            self.assertEqual('wait-for-upstream',train.status(self.state)['repositories']['extensions']['phase'])

    def test_exact_tag_and_required_job_fail_closed(self):
        self.bind_fixture('core')
        def wrong_tag(*args):
            if '/git/tags/' in args[-1]:return {'object':{'type':'commit','sha':'b'*40}}
            return self.github(*args)
        with patch.object(train,'gh',side_effect=wrong_tag),self.assertRaisesRegex(ValueError,'different commit'):
            train.inspect_release(self.state,'core')
        def skipped_job(*args):
            value=self.github(*args)
            if '/jobs?' in args[-1]:value[0]['jobs'][-1]['conclusion']='skipped'
            return value
        with patch.object(train,'gh',side_effect=skipped_job),self.assertRaisesRegex(ValueError,'required jobs'):
            train.inspect_release(self.state,'core')

    def test_running_job_stays_waiting_and_failed_job_requires_repair(self):
        self.bind_fixture('core')
        def run_status(status,conclusion):
            def respond(*args):
                value=self.github(*args)
                if '/workflows/' in args[-1]:value[0]['workflow_runs'][0].update(status=status,conclusion=conclusion)
                return value
            return respond
        with patch.object(train,'gh',side_effect=run_status('in_progress',None)):
            self.assertEqual('wait-for-run',train.inspect_release(self.state,'core')['phase'])
        with patch.object(train,'gh',side_effect=run_status('completed','failure')):
            self.assertEqual('repair-pipeline',train.inspect_release(self.state,'core')['phase'])

    def test_alignment_only_changes_one_configured_declaration(self):
        source='<Project><PackageVersion Include="Elsa.Api.Client" Version="3.8.0" /><PackageVersion Include="Other" Version="1.0" /></Project>'
        updated=train.aligned_text(source,{'package':'Elsa.Api.Client'},'3.9.0-rc1')
        self.assertIn('Version="3.9.0-rc1"',updated);self.assertIn('Include="Other" Version="1.0"',updated)
        self.assertEqual(updated,train.aligned_text(updated,{'package':'Elsa.Api.Client'},'3.9.0-rc1'))
        with self.assertRaisesRegex(ValueError,'found 2'):
            train.aligned_text(source+source,{'package':'Elsa.Api.Client'},'3.9.0')

    def test_template_alignment_updates_embedded_refs_branding_workflow_and_test_version(self):
        self.assertEqual(
            '<PackageReference Include="Elsa" Version="3.9.0" />',
            train.aligned_text('<PackageReference Include="Elsa" Version="3.8.0" />', {'package_prefix': 'Elsa'}, '3.9.0'),
        )
        self.assertEqual(
            'BASE_VERSION: 3.9.0',
            train.aligned_text('BASE_VERSION: 3.8.0', {'yaml_key': 'BASE_VERSION'}, '3.9.0'),
        )
        self.assertEqual(
            'BASE_VERSION: 3.9.0',
            train.aligned_text('BASE_VERSION: 3.8.0', {'yaml_key': 'BASE_VERSION', 'base_version': True}, '3.9.0-rc1'),
        )
        self.assertIn(
            'Elsa Studio 3.9',
            train.aligned_text('AppNameWithVersion => "Elsa Studio 3.8"', {'studio_branding': True}, '3.9.0'),
        )
        self.assertIn(
            'ElsaVersion = "3.9.0"',
            train.aligned_text('const string ElsaVersion = "3.8.0"', {'string_constant': 'ElsaVersion'}, '3.9.0'),
        )

    def test_announcements_cannot_complete_from_queued_or_changed_receipt(self):
        for name in self.state['repositories']:self.bind_fixture(name)
        self.site_fixture('website');self.site_fixture('documentation')
        with patch.object(train,'gh',side_effect=self.github):
            for platform in ['discord','linkedin','x']:
                message=self.root/f'{platform}.txt';message.write_text('Elsa 3.9.0 stable is available')
                receipt=self.root/f'{platform}.json'
                data={'id':platform,'url':'https://example.com/'+platform,'text':message.read_text(),'status':'scheduled','crossposted':True}
                train.save(receipt,data)
                args=SimpleNamespace(platform=platform,receipt=receipt,message_file=message)
                with self.assertRaisesRegex(ValueError,'verified publication'):
                    train.record_announcement(self.state,args)
                data['status']='sent';train.save(receipt,data)
                train.record_announcement(self.state,args)
            self.assertEqual('complete',train.status(self.state)['next'])
            receipt.write_text('{}')
            self.assertEqual('announcements',train.status(self.state)['next'])

    def test_site_gate_requires_live_evidence_and_rejects_queued_work(self):
        for name in self.state['repositories']: self.bind_fixture(name)
        with patch.object(train, 'gh', side_effect=self.github):
            self.assertEqual('sites', train.status(self.state)['next'])
            receipt = {
                'id': 'queued', 'target': 'website', 'status': 'queued', 'version': '3.9.0',
            }
            path = self.root / 'queued.json'; train.save(path, receipt)
            with self.assertRaisesRegex(ValueError, 'completed production'):
                train.record_site(self.state, SimpleNamespace(target='website', receipt=path))

    def test_site_gate_respects_no_post_refresh_independently_from_announcements(self):
        args = SimpleNamespace(**{**vars(self.args), 'state': self.root / 'no-sites.json', 'no_post_refresh': True})
        state = train.init(args)
        for name in state['repositories']:
            self.state['repositories'][name] = state['repositories'][name]
        self.state = state
        for name in state['repositories']: self.bind_fixture(name)
        with patch.object(train, 'gh', side_effect=self.github):
            self.assertEqual('announcements', train.status(state)['next'])
        args = SimpleNamespace(**{**vars(self.args), 'state': self.root / 'no-announcements.json', 'no_announcements': True})
        state = train.init(args)
        self.state = state
        for name in state['repositories']: self.bind_fixture(name)
        with patch.object(train, 'gh', side_effect=self.github):
            self.assertEqual('sites', train.status(state)['next'])

    def test_legacy_checkpoint_requires_explicit_adoption_and_supports_website_only_followup(self):
        for name in self.state['repositories']: self.bind_fixture(name)
        self.site_fixture('website');self.site_fixture('documentation')
        for platform in ('discord', 'linkedin', 'x'):
            message = self.root / f'{platform}.txt'; message.write_text('Elsa 3.9.0 stable is available')
            receipt = self.root / f'{platform}.json'
            train.save(receipt, {'id': platform, 'url': 'https://example.com/' + platform, 'text': message.read_text(), 'status': 'sent', 'crossposted': True})
            self.state['announcements'][platform] = {'receipt': str(receipt), 'sha256': train.digest(receipt), 'url': 'https://example.com/' + platform, 'id': platform}
        self.state.pop('post_refresh')
        self.state['profile'].pop('post_release_sites')
        self.state['schema'] = 1
        with patch.object(train, 'gh', side_effect=self.github):
            self.assertEqual('adopt-post-refresh', train.status(self.state)['next'])
        train.adopt_post_refresh(self.state, SimpleNamespace(targets=['website'], website_only=False, no_post_refresh=False))
        self.assertIn('post_release_sites', self.state['profile'])
        self.assertEqual(['website'], self.state['post_refresh']['targets'])
        self.state['post_refresh']['receipts'] = {}
        with patch.object(train, 'gh', side_effect=self.github):
            self.assertEqual('sites', train.status(self.state)['next'])
        self.site_fixture('website')
        with patch.object(train, 'gh', side_effect=self.github):
            self.assertEqual('complete', train.status(self.state)['next'])

    def test_prerelease_site_receipt_cannot_replace_latest_stable(self):
        self.state['version'] = '3.9.0-rc1'
        self.state['kind'] = 'rc'
        self.site_fixture('website', version='3.9.0-rc1')
        receipt_path = Path(self.state['post_refresh']['receipts']['website']['receipt'])
        receipt = train.read(receipt_path)
        receipt.update(content_label='rc', updates_current_stable=False, replaces_latest_stable=False)
        train.save(receipt_path, receipt)
        train.validate_site_receipt(self.state, 'website', receipt)
        receipt['replaces_latest_stable'] = True
        with self.assertRaisesRegex(ValueError, 'preserve latest stable'):
            train.validate_site_receipt(self.state, 'website', receipt)

    def test_older_stable_site_receipt_preserves_verified_newer_stable(self):
        self.site_fixture('website')
        receipt_path = Path(self.state['post_refresh']['receipts']['website']['receipt'])
        receipt = train.read(receipt_path)
        receipt.update(
            updates_current_stable=False,
            replaces_latest_stable=False,
            latest_stable_version='3.9.1',
            latest_stable_verification={
                'verified': True,
                'url': 'https://www.elsaworkflows.io',
                'version': '3.9.1',
                'evidence_at': receipt['evidence_at'],
            },
        )
        train.validate_site_receipt(self.state, 'website', receipt)
        receipt['latest_stable_verification']['version'] = '3.9.0'
        with self.assertRaisesRegex(ValueError, 'verify the preserved newer stable'):
            train.validate_site_receipt(self.state, 'website', receipt)

    def test_site_receipt_replacement_is_explicit_and_origin_and_time_are_checked(self):
        for name in self.state['repositories']: self.bind_fixture(name)
        self.site_fixture('website')
        original = self.state['post_refresh']['receipts']['website']
        replacement = self.root / 'replacement.json'
        receipt = train.read(original['receipt'])
        receipt['id'] = 'replacement-operation'
        receipt['changed_urls'] = ['https://untrusted.example/release-notes']
        train.save(replacement, receipt)
        with patch.object(train, 'gh', side_effect=self.github):
            with self.assertRaisesRegex(ValueError, 'non-empty changed_urls'):
                train.record_site(self.state, SimpleNamespace(target='website', receipt=replacement, replace=True))
        receipt['changed_urls'] = ['https://www.elsaworkflows.io/release-notes']
        receipt['evidence_at'] = '2099-01-01T00:00:00+00:00'
        receipt['production_verification']['evidence_at'] = receipt['evidence_at']
        train.save(replacement, receipt)
        with self.assertRaisesRegex(ValueError, 'cannot be in the future'):
            train.validate_site_receipt(self.state, 'website', receipt)
        receipt['evidence_at'] = '2026-09-04T10:00:00+00:00'
        receipt['production_verification']['evidence_at'] = receipt['evidence_at']
        train.save(replacement, receipt)
        with patch.object(train, 'gh', side_effect=self.github):
            with self.assertRaisesRegex(ValueError, 'different site receipt'):
                train.record_site(self.state, SimpleNamespace(target='website', receipt=replacement, replace=False))
            train.record_site(self.state, SimpleNamespace(target='website', receipt=replacement, replace=True))

    def test_manifest_requires_profile_feeds_npm_and_explicit_exceptions(self):
        manifest={'version':'3.9.0','source_commit':'a'*40,'nuget':[{'id':'Elsa','version':'3.9.0'}],'feeds':copy.deepcopy(self.state['profile']['feeds']),'npm':[]}
        train.validate_manifest(self.state,'core',manifest,'a'*40)
        manifest['nuget'][0]['verify_published']=False
        with self.assertRaisesRegex(ValueError,'exception'):
            train.validate_manifest(self.state,'core',manifest,'a'*40)
        manifest['nuget'][0].pop('verify_published');manifest['feeds'].pop()
        with self.assertRaisesRegex(ValueError,'feed policy'):
            train.validate_manifest(self.state,'core',manifest,'a'*40)

    def test_templates_manifest_requires_source_derived_content_expectations(self):
        manifest = {
            'version': '3.9.0',
            'source_commit': 'a' * 40,
            'nuget': [{'id': 'Elsa.Templates', 'version': '3.9.0'}],
            'feeds': copy.deepcopy(self.state['profile']['feeds']),
            'npm': [],
        }
        with self.assertRaisesRegex(ValueError, 'content expectations'):
            train.validate_manifest(self.state, 'templates', manifest, 'a' * 40)

    def test_solution_inventory_evaluates_packability_and_fixed_source_version(self):
        project=self.root/'Sample.csproj';project.write_text('<Project><PropertyGroup><Version>1.0.1</Version></PropertyGroup></Project>')
        fixed={'Sample':{'version':'1.0.1','verify_published':False,'reason':'fixed sample'}}
        with patch.object(package_manifest,'command',return_value='{"Properties":{"IsPackable":"true","PackageId":"Sample","PackageVersion":"3.9.0"}}'):
            self.assertEqual('1.0.1',package_manifest.evaluate_project(project,'3.9.0',fixed)['version'])
            project.write_text('<Project/>')
            with self.assertRaisesRegex(ValueError,'Fixed version changed'):
                package_manifest.evaluate_project(project,'3.9.0',fixed)
        with patch.object(package_manifest,'command',return_value='{"Properties":{"IsPackable":"false"}}'):
            self.assertIsNone(package_manifest.evaluate_project(project,'3.9.0',{}))

    def test_notes_keep_version_and_refuse_overwriting_reviewed_file(self):
        note=self.root/'notes.md';note.write_text('Reviewed notes')
        args=SimpleNamespace(version='3.9.0',repo_path=str(self.root),from_ref='3.8.0',to_ref='HEAD',output=str(note),overwrite=False)
        with patch.object(release_notes,'parse_args',return_value=args),patch.object(release_notes,'get_commits',return_value=[]):
            self.assertEqual(1,release_notes.main())
        self.assertEqual('Reviewed notes',note.read_text())
        self.assertIn('elsa-release-version: 3.9.0',release_notes.render_notes('3.9.0','3.8.0','HEAD',[]))


if __name__=='__main__':unittest.main()
