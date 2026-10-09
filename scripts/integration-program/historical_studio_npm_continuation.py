"""Exact reviewed correction of the two original Studio inline copy lifecycles."""
from __future__ import annotations

import json

from prove_consolidated_packages import require

PATH = 'src/wrappers/wrappers/react-wrapper/package.json'
BEFORE_BLOB = '47510f2cb2a18596dcf6e383a2a95ccba192c692'
AFTER_BLOB = '8ee363cd0af1242a2152bf8399b3bd4c2a20e973'
CONTINUATIONS = {
    '3.8': {'parent': '41ed15db7bfce7af5be2d362001518e8e692c22e',
            'commit': 'da2dec10ba36c65e376138ee45e1c34525e45e49', 'tree': 'f77c11fb4aeb1d2734ae3e326643ab23707e6fea'},
    '3.9': {'parent': 'e7cf096bc117d970dc0bbc11e26dabd2ea2e1b00',
            'commit': '98a3f23d9c3c67080f3926eeb584036c49855b72', 'tree': 'de330a0c1284c41a85c5fb0dd7c4af2ab0b1168e'},
}
COPY_SCRIPT = '''node -e "const fs=require('node:fs'),path=require('node:path'),source=path.dirname(require.resolve('@elsa-workflows/elsa-studio-wasm/package.json'));fs.mkdirSync('public',{recursive:true});for(const name of ['_content','_framework','appsettings.json'])fs.cpSync(path.join(source,name),path.join('public',name),{recursive:true});"'''


def corrected_manifest(original: bytes) -> bytes:
    old = json.loads(original)['scripts']['copy:elsa-studio-wasm']
    encoded = json.dumps(old).encode()
    require(original.count(encoded) == 1, 'Studio inline lifecycle source mismatch')
    return original.replace(encoded, json.dumps(COPY_SCRIPT).encode())


def verify_delta(row: dict, parent: dict, change: dict, before: bytes, after: bytes) -> None:
    expected = CONTINUATIONS.get(row['line'], {})
    require(row['product'] == 'studio' and row.get('source_kind') == 'core' and
            row['commit'] == expected.get('commit') and row['tree'] == expected.get('tree') and
            parent['commit'] == expected.get('parent') and row['parents'] == [parent['commit']] and
            row['delta'] == [change] and change == {'path': PATH,
                'before': {'mode': '100644', 'type': 'blob', 'blob': BEFORE_BLOB},
                'after': {'mode': '100644', 'type': 'blob', 'blob': AFTER_BLOB}} and
            after == corrected_manifest(before), 'Studio inline lifecycle delta mismatch')
