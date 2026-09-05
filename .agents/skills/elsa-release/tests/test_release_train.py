import copy
from pathlib import Path
import sys
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch

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
        self.args=SimpleNamespace(version='3.9.0',kind=None,profile=train.DEFAULT_PROFILE,repositories=None,repos_root=str(self.root),pr=None,source=None,no_announcements=False,state=self.root/'state.json')
        self.state=train.init(self.args)
        self.remote_existing=False

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
        if '/workflows/' in url:
            return [{'workflow_runs':[{'id':42,'head_sha':'a'*40,'head_branch':'3.9.0','event':'release','run_number':42,'run_attempt':1,'status':'completed','conclusion':'success','html_url':'https://github.com/run'}]}]
        if '/jobs?' in url:
            cfg=next(r for r in self.state['profile']['repositories'] if r['github'] in url)
            return [{'jobs':[{'name':n,'conclusion':'success'} for n in cfg['required_jobs']]}]
        self.fail(f'Unexpected GitHub call {args}')

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

    def test_dependency_order_and_tampered_receipt_blocks_progress(self):
        with patch.object(train,'gh',side_effect=self.github):
            first=train.status(self.state)
            self.assertEqual('prepare',first['repositories']['core']['phase'])
            self.assertEqual('wait-for-upstream',first['repositories']['studio']['phase'])
            self.bind_fixture('core')
            second=train.status(self.state)
            self.assertEqual('prepare',second['repositories']['studio']['phase'])
            self.bind_fixture('studio');self.bind_fixture('extensions')
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
