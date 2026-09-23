#!/usr/bin/env python3
"""Compare published Secrets persistence/API source snapshots without executing code.

This is a static contract check, not a migration runner. It reads pinned git
objects from local Core and Extensions clones and reports route collisions,
initial-migration column differences, project identities, and migration filenames.
"""
import argparse
import json
from pathlib import Path
import re
import subprocess

PINS = {
    'core_3_8_4': '33181ae3048f628f591a0155b5665a8e4d1bcea2',
    'core_candidate': 'fa1e36e8890a3ccd35626844772b781c29503342',
    'extensions_3_8_1': '01bf9ad70d0399afadae9609fe820703d3de290d',
    'extensions_3_8_4': '154ba15fb4da85b4bebecfbe43639579cbda1d0d',
}

CORE_API = 'src/modules/Elsa.Secrets/Endpoints/Secrets'
LEGACY_API = 'src/modules/secrets/Elsa.Secrets.Api/Endpoints/Secrets'
CORE_SQLITE_MIGRATIONS = 'src/modules/Elsa.Secrets.Persistence.EFCore.Sqlite/Migrations/Secrets'
LEGACY_SQLITE_MIGRATION = 'src/modules/secrets/Elsa.Secrets.Persistence.EFCore.Sqlite/Migrations/20240915164114_V3_3.cs'
COLLISION_PROJECTS = {
    'Elsa.Secrets.Persistence.EFCore',
    'Elsa.Secrets.Persistence.EFCore.PostgreSql',
    'Elsa.Secrets.Persistence.EFCore.SqlServer',
    'Elsa.Secrets.Persistence.EFCore.Sqlite',
    'Elsa.Studio.Secrets',
}
LEGACY_SUPPORT_PROJECTS = {
    'Elsa.Secrets.Api',
    'Elsa.Secrets.Core',
    'Elsa.Secrets.Management',
    'Elsa.Secrets.Models',
    'Elsa.Secrets.Scripting',
}

ROUTE = re.compile(r'\b(Get|Post|Put|Patch|Delete)\s*\(\s*["\']([^"\']+)["\']')
COLUMN = re.compile(r'^\s*(\w+)\s*=\s*table\.Column\s*<', re.MULTILINE)


def git(repo, *args):
    return subprocess.check_output(['git', '-C', str(repo), *args])


def pinned(repo, ref):
    return git(repo, 'rev-parse', '--verify', ref + '^{commit}').decode().strip()


def read_source(repo, ref, path):
    return git(repo, 'show', f'{ref}:{path}').decode()


def tracked_paths(repo, ref, prefix):
    entries = git(repo, 'ls-tree', '-r', '-z', '--name-only', ref, '--', prefix)
    return [path.decode() for path in entries.split(b'\0') if path]


def extract_routes(source):
    return {(verb.upper(), path) for verb, path in ROUTE.findall(source)}


def normalize_route(path):
    return re.sub(r'\{[^{}]+\}', '{}', path).casefold()


def route_conflicts(left, right):
    normalized_left = {(verb, normalize_route(path)) for verb, path in left}
    normalized_right = {(verb, normalize_route(path)) for verb, path in right}
    return sorted(normalized_left & normalized_right)


def extract_columns(source):
    return sorted(set(COLUMN.findall(source)))


def api_routes(repo, ref, prefix):
    routes = set()
    for path in tracked_paths(repo, ref, prefix):
        if path.endswith('.cs'):
            routes |= extract_routes(read_source(repo, ref, path))
    return routes


def migration_files(repo, ref, prefix):
    return sorted(
        Path(path).name for path in tracked_paths(repo, ref, prefix)
        if path.endswith('.cs') and not path.endswith('.Designer.cs') and 'ModelSnapshot' not in path
    )


def project_names(repo, ref, prefix):
    return sorted(
        Path(path).parent.name for path in tracked_paths(repo, ref, prefix)
        if path.endswith('.csproj')
    )


def compare(core_repo, extensions_repo):
    refs = {
        'core_3_8_4': pinned(core_repo, PINS['core_3_8_4']),
        'core_candidate': pinned(core_repo, PINS['core_candidate']),
        'extensions_3_8_1': pinned(extensions_repo, PINS['extensions_3_8_1']),
        'extensions_3_8_4': pinned(extensions_repo, PINS['extensions_3_8_4']),
    }
    legacy_columns = extract_columns(read_source(
        extensions_repo, refs['extensions_3_8_1'], LEGACY_SQLITE_MIGRATION))
    published_core_columns = extract_columns(read_source(
        core_repo, refs['core_3_8_4'], f'{CORE_SQLITE_MIGRATIONS}/20260531141623_Initial.cs'))
    candidate_core_columns = extract_columns(read_source(
        core_repo, refs['core_candidate'], f'{CORE_SQLITE_MIGRATIONS}/20260531141623_Initial.cs'))

    legacy_routes = api_routes(extensions_repo, refs['extensions_3_8_1'], LEGACY_API)
    published_core_routes = api_routes(core_repo, refs['core_3_8_4'], CORE_API)
    candidate_core_routes = api_routes(core_repo, refs['core_candidate'], CORE_API)

    extension_projects = project_names(
        extensions_repo, refs['extensions_3_8_4'], 'src/modules/secrets')

    return {
        'sourceCommits': refs,
        'extensionProjectsAt3_8_4SourceCommit': {
            'collisionCopies': sorted(set(extension_projects) & COLLISION_PROJECTS),
            'legacySupportPackages': sorted(set(extension_projects) & LEGACY_SUPPORT_PROJECTS),
        },
        'sqlite': {
            'extensions3_8_1V3_3MigrationColumns': legacy_columns,
            'core3_8_4InitialMigrationColumns': published_core_columns,
            'coreCandidateInitialMigrationColumns': candidate_core_columns,
            'v3_3ToPublishedInitialOnlyColumns': sorted(set(legacy_columns) - set(published_core_columns)),
            'publishedInitialToV3_3OnlyColumns': sorted(set(published_core_columns) - set(legacy_columns)),
            'core3_8_4Migrations': migration_files(
                core_repo, refs['core_3_8_4'], CORE_SQLITE_MIGRATIONS),
            'coreCandidateMigrations': migration_files(
                core_repo, refs['core_candidate'], CORE_SQLITE_MIGRATIONS),
        },
        'apiRoutes': {
            'extensions3_8_1': sorted(legacy_routes),
            'core3_8_4': sorted(published_core_routes),
            'coreCandidate': sorted(candidate_core_routes),
            'publishedRouteConflicts': route_conflicts(legacy_routes, published_core_routes),
            'candidateRouteConflicts': route_conflicts(legacy_routes, candidate_core_routes),
        },
        'migrationExecutionVerified': False,
        'credentialConversionVerified': False,
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--core', type=Path, required=True, help='local full Core git clone')
    parser.add_argument('--extensions', type=Path, required=True, help='local full Extensions git clone')
    args = parser.parse_args()
    print(json.dumps(compare(args.core, args.extensions), indent=2))


if __name__ == '__main__':
    main()
