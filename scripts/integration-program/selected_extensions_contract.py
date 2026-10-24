"""Two immutable Extensions generator/runtime contracts; no independent selectors."""
from __future__ import annotations

from pathlib import Path

from prove_consolidated_packages import require
import product_release_metadata as metadata
import selected_maintenance_39 as maintenance39

SOURCE = '20c7a51599b4c85bf591c4615d34674e0f5706bf'
SOURCE_BLOBS = {
    'test/modules/servicebus/Elsa.ServiceBus.AzureServiceBus.ComponentTests/Elsa.ServiceBus.AzureServiceBus.ComponentTests.csproj': '5717c6fd2248ce1f1582f0bc17f66df11ea4039a',
    'src/workbench/Elsa.Server.Web/Elsa.Server.Web.csproj': '4d3f83983df64dffa2946ec5acbdfd48d1834504',
    'src/workbench/Elsa.Server.Web/Program.cs': '464a460fbd774bca2c56f99a2f4fe0317a652f30',
    'src/workbench/Elsa.ServerAndStudio.Web/Elsa.ServerAndStudio.Web.csproj': '7d4a483715448920c4edabaef739d3c499d18e69',
    'src/workbench/Elsa.ServerAndStudio.Web/Program.cs': '3ba5792e53e829aea8c93bc0b3a909d632ad1480',
    'test/modules/scheduling/Elsa.Scheduling.Quartz.UnitTests/Elsa.Scheduling.Quartz.UnitTests.csproj': '8de039c1a29f57460da6b5ebfa81b98c7e146f42',
    'test/modules/scheduling/Elsa.Scheduling.Quartz.UnitTests/ShellFeatures/QuartzFeatureTests.cs': '2e5dce5779fbc1d0a492940fb0e47b0738c3cf30',
    'src/modules/agents/Elsa.Agents.Persistence.EFCore.MySql/Elsa.Agents.Persistence.EFCore.MySql.csproj': '817946a9b25ebb51f301405eb8dec6bfc8e26aec',
    'src/modules/scheduling/Elsa.Scheduling.Quartz.EFCore.MySql/Elsa.Scheduling.Quartz.EFCore.MySql.csproj': '0532241497a11b62c8163838eaa7ac3458e56ef4',
    'Directory.Build.props': 'cd8f89825d56e1649a1a240d98ab39f7f4161e84',
    'Directory.Packages.props': '1dcfbe635534098a37d33eddb367a34f82e080f9',
    'build/Build.cs': '4ea2b88cd5238c4dbc241bcee3f93c8fed0e12bb',
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
    """Verify the exact admitted Extensions commit and its source contract blobs."""
    if commit == maintenance39.contract('extensions')['commit']:
        maintenance39.verify_git(root, 'extensions', commit)
        return
    require(commit == SOURCE, 'extensions_contract_source')
    for path, blob in SOURCE_BLOBS.items():
        require(metadata.git(root, 'rev-parse', commit + ':' + path) == blob, 'extensions_contract_blob')


def bind_manifest_contract(source: Path, row: dict, policy: dict) -> None:
    # Other maintenance controls retain generic generated-output verification.
    """Attach the HTTP manifest contract only for the registered Extensions sources."""
    if policy['id'] != 'Elsa.IO.Http' or row['commit'] not in (SOURCE, maintenance39.contract('extensions')['commit']):
        return
    verify_source(source, row['commit'])
    apply_manifest_contract(policy, row['commit'])


def apply_manifest_contract(policy: dict, commit: str) -> None:
    # Pure seal calls this only after its exact registered-source admission.
    """Add the source-specific HTTP feature and generator expectations to package policy."""
    policy['source_commit'] = commit
    policy['manifest_expectation'] = MANIFEST_FEATURE
    policy['manifest_dependency_features'] = ['Elsa.IO.Http.I/O']
    policy['manifest_dependency_generator'] = '0.0.1-preview.50'
