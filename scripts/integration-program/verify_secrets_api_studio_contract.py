#!/usr/bin/env python3
"""Compare pinned legacy Secrets API, Core endpoint and Studio client contracts."""
import argparse
import json
from pathlib import Path
import re
import subprocess


ROUTE = re.compile(r'\b(Get|Post|Put|Patch|Delete)\s*\(\s*["\']([^"\']+)["\']')
CORE_PERMISSION = re.compile(r'RequirePermission\([^,]+,\s*(CoreVerbs\.\w+|"[^"]+")\)')
LEGACY_PERMISSION = re.compile(r'ConfigurePermissions\(\s*"([^"]+)"\s*\)')
STUDIO_METHOD = re.compile(
    r'\[(Get|Post|Put|Patch|Delete)\("([^"]+)"\)\]\s*'
    r'Task(?:<([^>]+)>)?\s+(\w+)\s*\((.*?)\);', re.DOTALL)
BODY_ARGUMENT = re.compile(r'\[Body\]\s*([\w.]+)\s+\w+')
PROPERTY = re.compile(r'public\s+[\w?<>., ]+\s+(\w+)\s*\{\s*get;\s*set;\s*\}')


def fail(message):
    raise ValueError(message)


def git(repo, *arguments):
    return subprocess.check_output(['git', '-C', str(repo), *arguments], text=True)


def verify_pin(repo, expected):
    actual = git(repo, 'rev-parse', '--verify', f'{expected}^{{commit}}').strip()
    if actual != expected:
        fail(f'Source pin mismatch: expected {expected}, got {actual}')
    return actual


def read_source(repo, ref, path):
    return git(repo, 'show', f'{ref}:{path}')


def tracked_paths(repo, ref, prefix):
    output = git(repo, 'ls-tree', '-r', '--name-only', ref, '--', prefix)
    return [path for path in output.splitlines() if path]


def endpoint_dto(source):
    match = re.search(r'\bclass\s+Endpoint\b[\s\S]*?:\s*(ElsaEndpoint(?:WithoutRequest)?)', source)
    if not match:
        return None, None
    base = match.group(1)
    rest = source[match.end():].lstrip()
    if not rest.startswith('<'):
        return None, None
    depth = 0
    end = None
    for index, character in enumerate(rest):
        if character == '<':
            depth += 1
        elif character == '>':
            depth -= 1
            if depth == 0:
                end = index
                break
    if end is None:
        fail(f'Unclosed endpoint DTO generic in {source[:120]}')
    arguments = rest[1:end]
    parts = []
    part_start = 0
    depth = 0
    for index, character in enumerate(arguments):
        if character == '<':
            depth += 1
        elif character == '>':
            depth -= 1
        elif character == ',' and depth == 0:
            parts.append(arguments[part_start:index].strip())
            part_start = index + 1
    parts.append(arguments[part_start:].strip())
    if base == 'ElsaEndpointWithoutRequest':
        return None, parts[0]
    if base == 'ElsaEndpoint':
        if len(parts) == 1:
            return parts[0], None
        if len(parts) == 2:
            return parts[0], parts[1]
    fail(f'Unsupported endpoint DTO base: {base}<{arguments}>')


def parse_api_routes(repo, ref, prefix, kind):
    result = []
    for path in tracked_paths(repo, ref, prefix):
        if not path.endswith('/Endpoint.cs'):
            continue
        source = read_source(repo, ref, path)
        matches = list(ROUTE.finditer(source))
        if not matches:
            continue
        if len(matches) != 1:
            fail(f'Expected one route declaration in {path}, got {len(matches)}')
        verb, route = matches[0].groups()
        request, response = endpoint_dto(source)
        if kind == 'core':
            permission_match = CORE_PERMISSION.search(source)
            if not permission_match:
                fail(f'Core endpoint has no Elsa permission declaration: {path}')
            permission = permission_match.group(1).strip('"')
            if permission.startswith('CoreVerbs.'):
                permission = permission.removeprefix('CoreVerbs.').lower()
            if permission not in {'view', 'write', 'delete', 'test'}:
                fail(f'Unknown Core Secrets permission {permission!r} at {path}')
            permission = f'secrets:{permission}'
            result.append({
                'verb': verb.upper(), 'path': route, 'endpoint': Path(path).parent.name,
                'request': request, 'response': response, 'permission': permission
            })
        else:
            permission_match = LEGACY_PERMISSION.search(source)
            if not permission_match:
                fail(f'Legacy endpoint has no permission declaration: {path}')
            result.append({
                'verb': verb.upper(), 'path': route, 'endpoint': Path(path).parent.name,
                'request': request, 'response': response, 'permission': permission_match.group(1)
            })
    return sorted(result, key=lambda row: (row['verb'], row['path'], row['endpoint']))


def parse_studio_routes(repo, ref, path):
    source = read_source(repo, ref, path)
    result = []
    for verb, route, response, method, signature in STUDIO_METHOD.findall(source):
        body = BODY_ARGUMENT.search(signature)
        query = []
        for parameter in signature.split(','):
            if '[Body]' in parameter or 'CancellationToken' in parameter:
                continue
            match = re.search(r'([A-Za-z_]\w*)\s*(?:=[^,]+)?\s*$', parameter.strip())
            if match:
                query.append(match.group(1))
        result.append({
            'verb': verb.upper(), 'path': route, 'method': method,
            'request': body.group(1) if body else None,
            'response': response.strip() if response else None,
            'query': query
        })
    if not result:
        fail(f'No Refit Secrets API methods found in {path}')
    return sorted(result, key=lambda row: (row['verb'], row['path'], row['method']))


def normalize_route(route):
    return re.sub(r'\{[^{}]+\}', '{}', route).casefold()


def route_conflicts(left, right):
    left_keys = {(row['verb'], normalize_route(row['path'])) for row in left}
    right_keys = {(row['verb'], normalize_route(row['path'])) for row in right}
    return sorted([verb, path] for verb, path in left_keys & right_keys)


def property_names(source, class_name):
    pattern = r'\bclass\s+' + re.escape(class_name) + r'\b[^{]*\{(.*?)\n\}'
    match = re.search(pattern, source, re.DOTALL)
    if not match:
        fail(f'Unable to locate DTO class {class_name}')
    return sorted(PROPERTY.findall(match.group(1)))


def property_map(repo, ref, path, class_names):
    source = read_source(repo, ref, path)
    return {name: property_names(source, name) for name in class_names}


def assert_equal(label, actual, expected):
    if actual != expected:
        fail(f'{label} differs from pinned contract fixture.\nExpected: {json.dumps(expected, sort_keys=True)}\nActual: {json.dumps(actual, sort_keys=True)}')


def verify(core_repo, extensions_repo, studio_repo, fixture):
    pins = fixture['sourcePins']
    for name, repo in [('core', core_repo), ('extensions', extensions_repo), ('studio', studio_repo)]:
        verify_pin(repo, pins[name])
    verify_pin(extensions_repo, pins['extensionsSchema'])

    core_routes = parse_api_routes(core_repo, pins['core'], fixture['sourcePaths']['coreApi'], 'core')
    legacy_routes = parse_api_routes(extensions_repo, pins['extensions'], fixture['sourcePaths']['legacyApi'], 'legacy')
    studio_routes = parse_studio_routes(studio_repo, pins['studio'], fixture['sourcePaths']['studioApi'])
    assert_equal('Core endpoint routes', core_routes, fixture['coreRoutes'])
    assert_equal('Legacy endpoint routes', legacy_routes, fixture['legacyRoutes'])
    assert_equal('Studio Refit methods', studio_routes, fixture['studioRoutes'])

    conflicts = route_conflicts(core_routes, legacy_routes)
    assert_equal('Normalized legacy/Core route collisions', conflicts, fixture['routeConflicts'])
    core_keys = [(row['verb'], normalize_route(row['path'])) for row in core_routes]
    for route in studio_routes:
        key = (route['verb'], normalize_route(route['path']))
        if core_keys.count(key) != 1:
            fail(f'Studio route must have exactly one Core owner: {route}')

    core_models = read_source(core_repo, pins['core'], fixture['sourcePaths']['coreModels'])
    studio_models = read_source(studio_repo, pins['studio'], fixture['sourcePaths']['studioModels'])
    legacy_models = read_source(extensions_repo, pins['extensionsSchema'], fixture['sourcePaths']['legacyModels'])
    legacy_input_model = read_source(extensions_repo, pins['extensionsSchema'], fixture['sourcePaths']['legacyInputModel'])
    legacy_entity = read_source(extensions_repo, pins['extensionsSchema'], fixture['sourcePaths']['legacyEntity'])
    assert_equal('Core SecretModel properties', property_names(core_models, 'SecretModel'), fixture['coreSecretModelProperties'])
    assert_equal('Studio SecretModel properties', property_names(studio_models, 'SecretModel'), fixture['studioSecretModelProperties'])
    assert_equal('Legacy SecretModel properties', property_names(legacy_models, 'SecretModel'), fixture['legacySecretModelProperties'])
    assert_equal('Legacy SecretInputModel properties', property_names(legacy_input_model, 'SecretInputModel'), fixture['legacySecretInputModelProperties'])
    core_dtos = property_map(core_repo, pins['core'], fixture['sourcePaths']['coreModels'], fixture['sharedStudioDtos'])
    studio_dtos = property_map(studio_repo, pins['studio'], fixture['sourcePaths']['studioModels'], fixture['sharedStudioDtos'])
    for name in fixture['sharedStudioDtos']:
        omitted = sorted(set(core_dtos[name]) - set(studio_dtos[name]))
        unexpected = sorted(set(studio_dtos[name]) - set(core_dtos[name]))
        assert_equal(f'{name} Studio-only fields', unexpected, [])
        assert_equal(f'{name} intentional Core fields omitted by Studio', omitted, fixture['dtoProjectionDifferences'].get(name, []))
    sensitive_fields = {value.casefold() for value in fixture['secretValueFields']}
    if sensitive_fields & {value.casefold() for value in core_dtos['SecretModel']}:
        fail('Core SecretModel exposes plaintext or encrypted secret value fields')
    if sensitive_fields & {value.casefold() for value in studio_dtos['SecretModel']}:
        fail('Studio SecretModel includes plaintext or encrypted secret value fields')
    list_query_names = sorted(name[0].upper() + name[1:] for name in next(row for row in studio_routes if row['method'] == 'ListAsync')['query'])
    list_omissions = sorted(set(property_names(core_models, 'ListSecretsRequest')) - set(list_query_names))
    assert_equal('Studio optional list query omissions', list_omissions, fixture['dtoProjectionDifferences']['ListSecretsRequest'])
    if not all(name in legacy_models for name in fixture['legacyModelMarkers']):
        fail('The pinned legacy DTO no longer contains all recorded identity/value fields')
    if not all(name in legacy_entity for name in fixture['legacyEntityMarkers']):
        fail('The pinned legacy entity no longer contains all recorded identity/value fields')

    legacy_endpoints = [read_source(extensions_repo, pins['extensions'], path)
                        for path in tracked_paths(extensions_repo, pins['extensions'], fixture['sourcePaths']['legacyApi'])
                        if path.endswith('/Endpoint.cs')]
    if any(re.search(r'\bOwner\b', source) for source in legacy_endpoints):
        fail('Legacy API endpoint code references Owner; the recorded non-authorization conclusion must be reviewed')
    plaintext_route = fixture['legacyPlaintextRoute']
    if tuple(plaintext_route) not in [(row['verb'], row['path']) for row in legacy_routes]:
        fail('The pinned legacy plaintext-read route is missing from the route table')
    if tuple(plaintext_route) in [(row['verb'], row['path']) for row in core_routes]:
        fail('The Core route set unexpectedly exposes the legacy plaintext-read route')
    plaintext_source = read_source(extensions_repo, pins['extensions'], fixture['sourcePaths']['legacyPlaintextEndpoint'])
    if not all(marker in plaintext_source for marker in fixture['legacyPlaintextMarkers']):
        fail('Pinned legacy input endpoint no longer decrypts and returns the plaintext input model')
    core_get = read_source(core_repo, pins['core'], fixture['sourcePaths']['coreGetEndpoint'])
    legacy_get = read_source(extensions_repo, pins['extensions'], fixture['sourcePaths']['legacyGetEndpoint'])
    if fixture['coreLookupMarker'] not in core_get or fixture['legacyLookupMarker'] not in legacy_get:
        fail('Core name lookup and legacy row-ID lookup semantics changed')

    core_feature = read_source(core_repo, pins['core'], fixture['sourcePaths']['coreFeature'])
    legacy_feature = read_source(extensions_repo, pins['extensions'], fixture['sourcePaths']['legacyFeature'])
    if 'Module.AddFastEndpointsAssembly<SecretsFeature>()' not in core_feature:
        fail('Core SecretsFeature no longer registers its own endpoint assembly')
    if 'Module.AddFastEndpointsAssembly<SecretsApiFeature>()' not in legacy_feature:
        fail('Legacy API feature registration changed; inspect before asserting host composition')

    return {
        'sourceCommits': pins,
        'coreRoutes': core_routes,
        'legacyRoutes': legacy_routes,
        'studioRoutes': studio_routes,
        'normalizedLegacyCoreRouteCollisions': conflicts,
        'dtoProjectionDifferences': fixture['dtoProjectionDifferences'],
        'coreSecretResponseContainsNoPlaintextOrCiphertext': True,
        'legacyPlaintextRouteExcludedFromCore': True,
        'legacyOwnerFieldHasNoEndpointAuthorizationUse': True,
        'studioRoutesHaveOneCoreTemplateOwner': True,
        'coreFeatureAndLegacyApiAreSeparateRegistrations': True,
        'consolidatedSampleBuildObservation': fixture['consolidatedSampleBuildObservation'],
        'defaultHostRuntimeRouteCompositionVerified': False,
        'crossTenantHttpRequestsVerified': False,
        'legacySidecarCompatibilityVerified': False,
        'note': 'Source contract only. Default-host HTTP composition and sidecar-dependent ID behavior require separate runtime fixtures.'
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--core', type=Path, required=True, help='full Core checkout containing the pinned source commit')
    parser.add_argument('--extensions', type=Path, required=True, help='Extensions checkout containing both pinned commits')
    parser.add_argument('--studio', type=Path, required=True, help='Studio checkout containing the pinned source commit')
    parser.add_argument('--fixture', type=Path, default=Path(__file__).with_name('secrets-api-studio-contract.json'))
    args = parser.parse_args()
    fixture = json.loads(args.fixture.read_text())
    print(json.dumps(verify(args.core, args.extensions, args.studio, fixture), indent=2))


if __name__ == '__main__':
    main()
