"""Exercise snapshot growth and live drift without GitHub credentials."""
import copy
import importlib.util
from pathlib import Path
import unittest

PATH = Path(__file__).resolve().parents[2] / 'doc/integration-program/verify-hierarchy.py'
spec = importlib.util.spec_from_file_location('verify_hierarchy', PATH)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class HierarchyTests(unittest.TestCase):
    def setUp(self):
        self.data = {
            'program': 8194, 'repository': 'elsa-workflows/elsa-core',
            'issues': [dict(number=n, level=level, url=f'https://github.com/elsa-workflows/elsa-core/issues/{n}')
                       for n, level in zip(range(8194, 8199), ['Program', 'Epic', 'Feature', 'Story', 'Task'])],
            'parent_edges': [[n, n + 1] for n in range(8194, 8198)],
            'blocking_edges': [[8196, 8198]],
        }

    def fetch(self, path):
        number = int(path.split('/issues/')[1].split('/')[0])
        if '/sub_issues?' in path:
            selected = {b for a, b in self.data['parent_edges'] if a == number}
        else:
            selected = {a for a, b in self.data['blocking_edges'] if b == number}
        return [{'html_url': r['url']} for r in self.data['issues'] if r['number'] in selected]

    def test_growing_graph_keeps_valid_levels_without_fixed_counts(self):
        self.data['issues'].append(dict(number=8200, level='Task', url='https://github.com/elsa-workflows/elsa-core/issues/8200'))
        self.data['parent_edges'].append([8197, 8200])
        self.assertEqual(len(module.validate(self.data)), 6)
        module.verify_live(self.data, self.fetch)

    def test_invalid_graphs_fail(self):
        mutations = [
            lambda d: d['issues'].append(d['issues'][0]),
            lambda d: d['parent_edges'].pop(),
            lambda d: d['parent_edges'].__setitem__(3, [8196, 8198]),
            lambda d: d['blocking_edges'].append([8198, 8196]),
            lambda d: d['blocking_edges'].append([9000, 8198]),
            lambda d: d['issues'][0].update(level='Epic'),
        ]
        for mutate in mutations:
            with self.subTest(mutation=mutate):
                data = copy.deepcopy(self.data)
                mutate(data)
                with self.assertRaises(ValueError):
                    module.validate(data)

    def test_live_detects_unrecorded_children_and_blockers_including_leaves(self):
        for endpoint in ('sub_issues', 'dependencies/blocked_by'):
            def drift(path):
                rows = self.fetch(path)
                if f'/8198/{endpoint}?' in path:
                    rows.append({'html_url': 'https://github.com/another/repo/issues/8194'})
                return rows
            with self.subTest(endpoint=endpoint), self.assertRaisesRegex(ValueError, 'unrecorded='):
                module.verify_live(self.data, drift)

    def test_live_detects_missing_relationship(self):
        with self.assertRaisesRegex(ValueError, 'missing='):
            module.verify_live(self.data, lambda _: [])


if __name__ == '__main__':
    unittest.main()
