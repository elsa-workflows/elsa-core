"""Adversarial real ZIP/tar fixtures, never native or provider execution."""
from copy import deepcopy
from datetime import datetime, timezone
import gzip
import hashlib
import io
import json
from pathlib import Path
import stat
import struct
import tarfile
import tempfile
import unittest
import warnings
from unittest.mock import patch
from uuid import uuid4
import zipfile
import selected_control_transport as transport

NOW = datetime(2026, 10, 10, 12, tzinfo=timezone.utc)


def encode(value):
    return (json.dumps(value, sort_keys=True) + '\n').encode()


def zipped(entries):
    output = io.BytesIO()
    with zipfile.ZipFile(output, 'w', zipfile.ZIP_DEFLATED) as archive:
        for name, data in entries:
            with warnings.catch_warnings():
                warnings.simplefilter('ignore', UserWarning)
                archive.writestr(name, data)
    return output.getvalue()


def tarred(entries):
    output = io.BytesIO()
    with tarfile.open(fileobj=output, mode='w:gz', format=tarfile.PAX_FORMAT) as archive:
        for name, data, kind in entries:
            item = tarfile.TarInfo(name)
            item.type = kind
            item.size = len(data) if kind == tarfile.REGTYPE else 0
            item.linkname = '/private/tmp/payload' if kind in (tarfile.SYMTYPE, tarfile.LNKTYPE) else ''
            archive.addfile(item, io.BytesIO(data))
    return output.getvalue()


class Fixture(unittest.TestCase):
    def setUp(self):
        self.controller = {'commit': 'a' * 40, 'tree': 'b' * 40}
        self.context = {'repository': transport.REPOSITORY, 'repository_id': transport.REPOSITORY_ID,
            'event': 'push', 'ref': transport.HOSTED_REF, 'head_sha': 'a' * 40,
            'workflow_ref': f'{transport.REPOSITORY}/{transport.WORKFLOW}@{transport.HOSTED_REF}',
            'workflow_sha': 'a' * 40, 'workflow_path': transport.WORKFLOW,
            'run_id': '123', 'run_attempt': '2', 'job': 'control'}
        # Deliberately not an admitted selected plan/receipt: transport is not
        # semantic acceptance, even when these exact bytes read back correctly.
        self.plan = {'product': 'studio', 'line': '3.8', 'source': {'commit': 'c' * 40},
            'controller': self.controller | {'execution': {'repository': transport.REPOSITORY,
                'event_name': 'push', 'ref': transport.HOSTED_REF, 'run_id': '123', 'run_attempt': '2'}}}
        self.plan_hash = hashlib.sha256(encode(self.plan)).hexdigest()
        self.files = {'plan.json': encode(self.plan), 'producer/receipt.json': b'{"not_admitted":true}',
                      'consumer/receipt.json': b'{"not_admitted":true}'}
        self.expected = {'context': self.context, 'controller': self.controller, 'product': 'studio',
                         'line': '3.8', 'manifest_sha256': '0' * 64}
        self.manifest = {'schema': 1, 'scope': transport.TRANSPORT_SCOPE, 'context': self.context,
            'controller': self.controller, 'product': 'studio', 'line': '3.8', 'files': transport.inventory(self.files)}
        self.provider = {'schema': 1, 'repository': transport.REPOSITORY, 'repository_id': transport.REPOSITORY_ID,
            'run_id': '123', 'run_attempt': '2', 'head_sha': 'a' * 40,
            'head_branch': transport.HOSTED_REF.removeprefix('refs/heads/'), 'event': 'push',
            'workflow_path': transport.WORKFLOW, 'control_job': 'control', 'control_conclusion': 'success',
            'run_status': 'in_progress', 'run_conclusion': None, 'artifact_id': 456,
            'artifact_name': f"selected-control-studio-3.8-{'a' * 40}-123-2", 'archive_sha256': '0' * 64,
            'archive_size': 1, 'manifest_sha256': '0' * 64, 'created_at': '2026-10-10T10:00:00Z',
            'expires_at': '2026-11-10T10:00:00Z', 'expired': False, 'retrieved_at': '2026-10-10T11:00:00Z'}

    def archive(self, entries=None, manifest=None):
        raw = encode(self.manifest if manifest is None else manifest)
        self.expected['manifest_sha256'] = self.provider['manifest_sha256'] = hashlib.sha256(raw).hexdigest()
        data = zipped(list((self.files if entries is None else entries).items()) + [(transport.MANIFEST, raw)])
        self.provider.update(archive_size=len(data), archive_sha256=hashlib.sha256(data).hexdigest())
        return data

    def readback(self, data):
        return transport.readback_transport(data, self.provider, self.expected, now=NOW)

    def execution(self, role):
        return {'kind': 'github-actions-selected-control', 'id': str(uuid4()), 'started_at': '2026-10-10T09:00:00Z',
            'role': role, 'controller': self.controller, 'plan_sha256': self.plan_hash, 'product': 'studio',
            'line': '3.8', 'source': self.plan['source'], 'context': self.context,
            'authority': 'runner-environment-only-provider-unverified'}

    def pair(self, artifact, consumer):
        transport.validate_execution_pair(self.plan, self.plan_hash, artifact, consumer,
                                           self.controller, self.controller, now=NOW)


class OriginalZipContracts(Fixture):
    def test_transport_only_original_bytes_with_other_readback_environment(self):
        data = self.archive()
        with patch.dict('os.environ', {'GITHUB_JOB': 'readback', 'GITHUB_RUN_ID': '999'}):
            result = self.readback(data)
        self.assertEqual({key: result[key] for key in self.files}, self.files)
        self.assertEqual(transport.strict_json(result[transport.MANIFEST])['scope'], transport.TRANSPORT_SCOPE)
        self.assertFalse(hasattr(transport, 'seal'))
        self.assertFalse(hasattr(transport, 'verify_and_extract'))

    def test_explicit_six_transport_cells(self):
        for product in ('core', 'studio', 'extensions'):
            for line in ('3.8', '3.9'):
                with self.subTest(product=product, line=line):
                    self.expected.update(product=product, line=line)
                    self.manifest.update(product=product, line=line)
                    self.provider['artifact_name'] = f"selected-control-{product}-{line}-{'a' * 40}-123-2"
                    self.assertIn('plan.json', self.readback(self.archive()))

    def test_outer_size_digest(self):
        data = self.archive()
        for key, value in (('archive_sha256', 'f' * 64), ('archive_size', len(data) + 1)):
            original = self.provider[key]
            self.provider[key] = value
            with self.subTest(key=key), self.assertRaises(ValueError):
                self.readback(data)
            self.provider[key] = original

    def test_provider_identity_clock_and_artifact_tampering(self):
        data = self.archive()
        changes = {'repository': 'fork/core', 'repository_id': '999', 'run_id': '124', 'run_attempt': '3',
            'head_sha': 'f' * 40, 'head_branch': 'main', 'event': 'pull_request', 'workflow_path': 'other',
            'control_job': 'readback', 'control_conclusion': 'failure', 'artifact_id': True,
            'artifact_name': 'other', 'expired': True, 'manifest_sha256': 'f' * 64, 'schema': True,
            'created_at': '2026-10-10T13:00:00Z', 'retrieved_at': '2026-10-10T13:00:00Z',
            'expires_at': '2026-10-10T11:00:00Z'}
        for key, value in changes.items():
            original = self.provider[key]
            self.provider[key] = value
            with self.subTest(key=key), self.assertRaises(ValueError):
                self.readback(data)
            self.provider[key] = original

    def test_closed_provider_and_expected(self):
        data = self.archive()
        for target in (self.provider, self.expected):
            target['download_url'] = 'https://untrusted.invalid/private'
            with self.assertRaises(ValueError):
                self.readback(data)
            del target['download_url']

    def test_run_status_conclusion(self):
        data = self.archive()
        for status, conclusion in (('completed', 'failure'), ('queued', None), ('in_progress', 'success')):
            self.provider.update(run_status=status, run_conclusion=conclusion)
            with self.subTest(status=status), self.assertRaises(ValueError):
                self.readback(data)
        self.provider.update(run_status='completed', run_conclusion='success')
        self.assertIn('plan.json', self.readback(data))

    def test_nested_manifest_context_unknownkeys_and_scope(self):
        for key, value in (('context', self.context | {'run_attempt': '3'}), ('product', 'core'),
                           ('controller', self.controller | {'tree': 'f' * 40}), ('scope', 'control-accepted'),
                           ('schema', True), ('private_path', '/private/tmp/log')):
            manifest = deepcopy(self.manifest)
            manifest[key] = value
            with self.subTest(key=key), self.assertRaises(ValueError):
                self.readback(self.archive(manifest=manifest))

    def test_missing_extra_changed_members(self):
        for entries in ({key: value for key, value in self.files.items() if key != 'plan.json'},
                        self.files | {'producer/nuget/extra.1.0.nupkg': b'extra'},
                        self.files | {'consumer/receipt.json': b'changed'}):
            with self.assertRaises(ValueError):
                self.readback(self.archive(entries=entries))

    def test_manifest_entry_unknown_changed_missing_size_or_digest(self):
        for mutate in (lambda row: row.update(log='/private/tmp/log'), lambda row: row.update(size=True),
                       lambda row: row.update(size=1), lambda row: row.update(sha256='f' * 64),
                       lambda row: row.pop('sha256')):
            manifest = deepcopy(self.manifest)
            mutate(manifest['files'][0])
            with self.assertRaises(ValueError):
                self.readback(self.archive(manifest=manifest))

    def test_manifest_circular_duplicate_casefold_private_paths(self):
        for path in (transport.MANIFEST, 'PLAN.json', '/private/tmp/assets.json', '../secret',
                     'producer/private/raw.log', 'producer/nuget/../secret.nupkg'):
            manifest = deepcopy(self.manifest)
            manifest['files'].append({'path': path, 'size': 0, 'sha256': hashlib.sha256(b'').hexdigest()})
            with self.subTest(path=path), self.assertRaises(ValueError):
                self.readback(self.archive(manifest=manifest))

    def test_manifest_digest_checked_before_json(self):
        data = self.archive()
        self.expected['manifest_sha256'] = self.provider['manifest_sha256'] = 'f' * 64
        with self.assertRaisesRegex(ValueError, 'manifest_bytes'):
            self.readback(data)

    def test_limits_fail_without_autoexpansion(self):
        data = self.archive()
        for key, value in (('MAX_ARCHIVE_BYTES', len(data) - 1), ('MAX_EXPANDED_BYTES', 1),
                           ('MAX_PLAN_BYTES', 1), ('MAX_RECEIPT_BYTES', 1), ('MAX_MEMBERS', 1)):
            with self.subTest(key=key), patch.object(transport, key, value), self.assertRaises(ValueError):
                self.readback(data)


class ArchiveSafetyContracts(unittest.TestCase):
    def test_safe_path_aliases_controls_and_traversal(self):
        for name in ('/private/tmp/file', '../file', 'dir/../file', 'dir//file', './file', 'file/',
                     'C:/file', 'dir\\file', 'dir\x00file', 'dir\nfile', '\ud800', ''):
            with self.subTest(name=name), self.assertRaises(ValueError):
                transport.safe_name(name)

    def test_exact_full_zip_inventory(self):
        data = zipped([('a.nuspec', b'<package/>'), ('lib/net8.0/A.dll', b'compiled')])
        self.assertEqual(transport.archive_inventory(data), [
            {'path': 'a.nuspec', 'size': 10, 'sha256': hashlib.sha256(b'<package/>').hexdigest()},
            {'path': 'lib/net8.0/A.dll', 'size': 8, 'sha256': hashlib.sha256(b'compiled').hexdigest()}])

    def test_zip_duplicate_casefold_file_directory_collision(self):
        for names in (['a', 'a'], ['a', 'A'], ['a', 'a/b'], ['A', 'a/b']):
            with self.subTest(names=names), self.assertRaises(ValueError):
                transport.zip_members(zipped([(name, b'x') for name in names]), leaf=True)

    def test_zip_and_tar_reject_unsafe_resolved_member_paths(self):
        for name in ('../escape', '/private/tmp/escape', 'C:/escape', 'dir\\escape', 'dir//escape'):
            with self.subTest(name=name), self.assertRaises(ValueError):
                transport.zip_members(zipped([(name, b'x')]), leaf=True)
        # The tarfile reader resolves PAX path records. Check that resolved
        # identity, including long names, rather than trusting the raw header.
        data = tarred([('package/package.json', b'{}', tarfile.REGTYPE),
                       ('package/' + ('a' * 120) + '/../escape', b'x', tarfile.REGTYPE)])
        with self.assertRaises(ValueError):
            transport.tar_members(data)

    def test_zip_symlink_special_and_directory_members(self):
        for mode in (stat.S_IFLNK, stat.S_IFIFO, stat.S_IFSOCK, stat.S_IFCHR, stat.S_IFDIR):
            item = zipfile.ZipInfo('unsafe')
            item.create_system = 3
            item.external_attr = (mode | 0o644) << 16
            with self.subTest(mode=mode), self.assertRaises(ValueError):
                transport.zip_members(zipped([(item, b'payload')]), leaf=True)
        with self.assertRaises(ValueError):
            transport.zip_members(zipped([('directory/', b'')]), leaf=True)
        item = zipfile.ZipInfo('dos_directory_without_slash')
        item.create_system = 0
        item.external_attr = 0x10
        with self.assertRaises(ValueError):
            transport.zip_members(zipped([(item, b'')]), leaf=True)

    def test_zip_declared_expansion_bomb_precedes_read(self):
        data = bytearray(zipped([('payload', b'x')]))
        offset = data.index(b'PK\x01\x02')
        struct.pack_into('<I', data, offset + 24, transport.MAX_PACKAGE_BYTES + 1)
        with self.assertRaisesRegex(ValueError, 'size'):
            transport.zip_members(bytes(data), leaf=True)

    def test_zip_nul_original_filename_is_not_silently_truncated(self):
        data = zipped([('payload', b'x')]).replace(b'payload', b'pay\x00oad')
        with self.assertRaisesRegex(ValueError, 'path'):
            transport.zip_members(data, leaf=True)

    def test_zip_encryption_and_unsupported_compression(self):
        for field, value in ((8, 1), (10, 99)):
            data = bytearray(zipped([('payload', b'x')]))
            offset = data.index(b'PK\x01\x02')
            struct.pack_into('<H', data, offset + field, value)
            with self.assertRaisesRegex(ValueError, 'member_type'):
                transport.zip_members(bytes(data), leaf=True)

    def test_npm_real_tar_inventory_and_typed_integrity(self):
        data = tarred([('package/package.json', b'{"name":"@elsa/pkg"}', tarfile.REGTYPE),
                       ('package/dist/index.js', b'export {}', tarfile.REGTYPE)])
        self.assertEqual([row['path'] for row in transport.archive_inventory(data, npm=True)],
                         ['dist/index.js', 'package.json'])
        self.assertTrue(transport.sha512_integrity(data).startswith('sha512-'))
        self.assertNotEqual(transport.sha512_integrity(data), hashlib.sha512(data).hexdigest())

    def test_npm_links_special_directory_and_paths(self):
        for name, kind in (('package/link', tarfile.SYMTYPE), ('package/link', tarfile.LNKTYPE),
                           ('package/dir', tarfile.DIRTYPE), ('package/fifo', tarfile.FIFOTYPE),
                           ('package/../secret', tarfile.REGTYPE), ('/private/tmp/file', tarfile.REGTYPE)):
            data = tarred([('package/package.json', b'{}', tarfile.REGTYPE), (name, b'x', kind)])
            with self.subTest(name=name, kind=kind), self.assertRaises(ValueError):
                transport.tar_members(data)

    def test_npm_casefold_prefix_collision(self):
        for names in (['a', 'A'], ['a', 'a/b']):
            data = tarred([('package/package.json', b'{}', tarfile.REGTYPE)] +
                          [('package/' + name, b'x', tarfile.REGTYPE) for name in names])
            with self.assertRaises(ValueError):
                transport.tar_members(data)

    def test_tar_hidden_members_after_end_marker_are_rejected(self):
        first = tarred([('package/package.json', b'{}', tarfile.REGTYPE)])
        second = tarred([('package/hidden.log', b'private', tarfile.REGTYPE)])
        for data in (gzip.compress(gzip.decompress(first) + gzip.decompress(second)), first + second):
            with self.assertRaisesRegex(ValueError, 'trailing_members'):
                transport.tar_members(data)

    def test_npm_expansion_limit_malformed_archives(self):
        data = tarred([('package/package.json', b'{}', tarfile.REGTYPE),
                       ('package/large', b'x' * 1024, tarfile.REGTYPE)])
        with patch.object(transport, 'MAX_PACKAGE_BYTES', len(data) + 1), self.assertRaises(ValueError):
            transport.tar_members(data)
        for data in (b'not tar', b'not zip'):
            with self.assertRaises(ValueError):
                transport.tar_members(data)
            with self.assertRaises(ValueError):
                transport.zip_members(data, leaf=True)

    def test_strict_json_duplicates_constants_unicode_and_nonobjects(self):
        for data in (b'{"a":1,"a":2}', b'{"a":{"b":1,"b":2}}', b'{"a":NaN}', b'{"a":Infinity}', b'{"a":1e999}', b'{"a":-1e999}',
                     b'\xff', b'[]', b'null', b'{'):
            with self.subTest(data=data), self.assertRaises(ValueError):
                transport.strict_json(data)

    def test_exact_bound_file_and_symlink_ancestors(self):
        with tempfile.TemporaryDirectory() as raw:
            directory = Path(raw).resolve()
            path = directory / 'receipt.json'
            data = b'{ "preserve" : true }\n'
            path.write_bytes(data)
            sha = hashlib.sha256(data).hexdigest()
            self.assertEqual(transport.read_bound(path, sha, len(data)), data)
            for expected, size in (('f' * 64, len(data)), (sha, len(data) - 1)):
                with self.assertRaises(ValueError):
                    transport.read_bound(path, expected, size)
            link = directory / 'link'
            link.symlink_to(path)
            with self.assertRaises(ValueError):
                transport.read_bound(link, sha, len(data))
            ancestor = directory / 'alias'
            ancestor.symlink_to(directory, target_is_directory=True)
            with self.assertRaises(ValueError):
                transport.read_bound(ancestor / 'receipt.json', sha, len(data))


class ImmutableIdentityContracts(Fixture):
    def test_same_job_distinct_roles_independent_environment(self):
        artifact, consumer = self.execution('artifact'), self.execution('consumer')
        consumer['started_at'] = '2026-10-10T09:01:00Z'
        with patch.dict('os.environ', {'GITHUB_JOB': 'readback', 'GITHUB_RUN_ATTEMPT': '999'}):
            self.pair(artifact, consumer)

    def test_no_local_promotion(self):
        artifact = {key: self.execution('artifact')[key] for key in ('id', 'started_at')} | {'kind': 'local-control'}
        with self.assertRaises(ValueError):
            self.pair(artifact, self.execution('consumer'))

    def test_execution_source_controller_role_cell_clock(self):
        changes = {'source': {'commit': 'f' * 40}, 'controller': self.controller | {'tree': 'f' * 40},
            'role': 'consumer', 'product': 'core', 'line': '3.9', 'plan_sha256': 'f' * 64,
            'authority': 'provider-verified', 'started_at': '2026-10-10T13:00:00Z', 'id': 'not-uuid'}
        for key, value in changes.items():
            artifact = self.execution('artifact')
            artifact[key] = value
            with self.subTest(key=key), self.assertRaises(ValueError):
                self.pair(artifact, self.execution('consumer'))

    def test_equal_uuid_reversed_clock_or_job(self):
        for key, value in (('id', None), ('started_at', '2026-10-10T08:00:00Z'),
                           ('context', self.context | {'job': 'another_control'})):
            artifact, consumer = self.execution('artifact'), self.execution('consumer')
            consumer[key] = artifact['id'] if value is None else value
            with self.assertRaises(ValueError):
                self.pair(artifact, consumer)

    def test_context_fork_pr_partial_attempt_workflow_head(self):
        for key, value in (('repository', 'fork/core'), ('repository_id', '999'), ('event', 'pull_request'),
            ('run_attempt', '0'), ('workflow_sha', 'f' * 40), ('workflow_path', 'other'), ('job', '../job')):
            with self.subTest(key=key), self.assertRaises(ValueError):
                transport.validate_context(self.context | {key: value}, self.controller)
        context = deepcopy(self.context)
        del context['run_id']
        with self.assertRaises(ValueError):
            transport.validate_context(context, self.controller)

    def test_planner_context_join(self):
        self.plan['controller']['execution']['run_attempt'] = '1'
        with self.assertRaises(ValueError):
            self.pair(self.execution('artifact'), self.execution('consumer'))

    def test_utc_future_and_naive_clock_rejected(self):
        for value in ('2026-10-10T12:00:00', '2026-10-10T12:00:00+01:00', 'tomorrow', '2026-10-10T13:00:00Z'):
            with self.subTest(value=value), self.assertRaises(ValueError):
                transport.utc(value, now=NOW)
        with self.assertRaises(ValueError):
            transport.utc('2026-10-10T11:00:00Z', now=NOW.replace(tzinfo=None))


if __name__ == '__main__':
    unittest.main()
