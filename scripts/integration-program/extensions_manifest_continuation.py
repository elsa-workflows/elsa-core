"""Exact original-layout Compile PackageVersion corrections for both Extensions lines."""
from __future__ import annotations

from prove_consolidated_packages import require

PATH = 'build/Build.cs'
BEFORE_BLOB = 'b39a758717f88096e9b0226ff63a17ebfa58277d'
AFTER_BLOB = '1abe2f2dc4a50206fa30958a508d00279d0508a7'
CONTINUATIONS = {
    '3.8': {'parent': '92c27a3dd2c7f1dc107cba34749dd293a5d77743',
            'commit': '984009a61c786a9f585ee0ce6304a1281f367dff', 'tree': 'fe83383557c290f967a5b7f80d4f113113ce1c1e'},
    '3.9': {'parent': 'bd7b846efae7e93676c6c3a594e682a5a958b766',
            'commit': '25d70474c6326a430b7f5c8701dc5e416295549a', 'tree': '6274c04fdcdc05f12dc38a6ae4bba982abf45b80'},
}


def corrected_build(original: bytes) -> bytes:
    before = b'        .SetWarningLevel(IsServerBuild ? 0 : 1);'
    after = (b'        .SetWarningLevel(IsServerBuild ? 0 : 1)\n'
             b'        .When(_ => !string.IsNullOrWhiteSpace(Version), settings => settings.SetProperty("PackageVersion", Version));')
    require(original.count(before) == 1, 'Extensions Compile version source mismatch')
    return original.replace(before, after)


def verify_delta(row: dict, parent: dict, change: dict, before: bytes, after: bytes) -> None:
    expected = CONTINUATIONS.get(row['line'], {})
    require(row['product'] == 'extensions' and row.get('source_kind') == 'core' and
            row['commit'] == expected.get('commit') and row['tree'] == expected.get('tree') and
            parent['commit'] == expected.get('parent') and row['parents'] == [parent['commit']] and
            row['delta'] == [change] and change == {'path': PATH,
                'before': {'mode': '100644', 'type': 'blob', 'blob': BEFORE_BLOB},
                'after': {'mode': '100644', 'type': 'blob', 'blob': AFTER_BLOB}} and
            after == corrected_build(before), 'Extensions Compile version delta mismatch')
