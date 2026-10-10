"""Two immutable Extensions generator/runtime contracts; no independent selectors."""
from __future__ import annotations

from pathlib import Path

from prove_consolidated_packages import require
import product_release_metadata as metadata
import selected_maintenance_39 as maintenance39

SOURCE = '984009a61c786a9f585ee0ce6304a1281f367dff'
SOURCE_BLOBS = {
    'Directory.Build.props': 'cd8f89825d56e1649a1a240d98ab39f7f4161e84',
    'Directory.Packages.props': '1dcfbe635534098a37d33eddb367a34f82e080f9',
    'build/Build.cs': '1abe2f2dc4a50206fa30958a508d00279d0508a7',
    'src/modules/Directory.Build.props': 'c457c8e6daaa2de2f66f84dac7b27d281dcc8cf1',
    'src/modules/PackageManifestHints.cs': '63d76de64a93ad3f0d0bc763795de15bafe6c088',
    'src/modules/io/Elsa.IO.Http/Elsa.IO.Http.csproj': '2d5c62ea65e85f218d5fd719a5f33901fca7f7ab',
    'src/modules/io/Elsa.IO.Http/ShellFeatures/HttpIOShellFeature.cs': '1965ee72d03e97160c7e490008828c21d23e6f99',
    'src/modules/io/Elsa.IO.Http/Features/IOHttpFeature.cs': 'f33ae57a76da1c5c8ab191fa39340e489b20efae',
    'src/modules/io/Elsa.IO.Http/Services/Strategies/UrlContentStrategy.cs': '2803403d35e37d8a86f8103fa6faa9216aa80cfa',
    'src/modules/io/Elsa.IO/ShellFeatures/IOShellFeature.cs': '5e4d8041cac9a94c50959fbbc36edc32bcec634f',
    'src/modules/io/Elsa.IO.Compression/ShellFeatures/CompressionIOShellFeature.cs': '2edbe74770544c427d9c95f7a3617376dc337425',
    'src/modules/io/Elsa.IO/Models/BinaryContent.cs': 'e84f6cfe937947444e5a6915fa1eb59d53e75df5',
}
MANIFEST_FEATURE = {'typeName': 'Elsa.IO.Http.ShellFeatures.HttpIOShellFeature',
                    'displayName': 'HTTP I/O', 'description': 'Provides HTTP-based I/O activities for workflows'}


def verify_source(root: Path, commit: str) -> None:
    if commit == maintenance39.contract('extensions')['commit']:
        maintenance39.verify_git(root, 'extensions', commit)
        return
    require(commit == SOURCE, 'extensions_contract_source')
    for path, blob in SOURCE_BLOBS.items():
        require(metadata.git(root, 'rev-parse', commit + ':' + path) == blob, 'extensions_contract_blob')


def bind_manifest_contract(source: Path, row: dict, policy: dict) -> None:
    # Other maintenance controls retain generic generated-output verification.
    if policy['id'] != 'Elsa.IO.Http' or row['commit'] not in (SOURCE, maintenance39.contract('extensions')['commit']):
        return
    verify_source(source, row['commit'])
    apply_manifest_contract(policy, row['commit'])


def apply_manifest_contract(policy: dict, commit: str) -> None:
    # Pure seal calls this only after its exact registered-source admission.
    policy['source_commit'] = commit
    policy['manifest_expectation'] = MANIFEST_FEATURE
    policy['manifest_dependency_features'] = ['Elsa.IO.Http.I/O']
    policy['manifest_dependency_generator'] = '0.0.1-preview.50'
