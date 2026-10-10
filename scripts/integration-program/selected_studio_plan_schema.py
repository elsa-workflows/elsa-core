"""Closed public plan projection for the six original selected-product cells."""
from __future__ import annotations

from datetime import datetime, timezone
import re
from urllib.parse import urlsplit

import plan_product_release as planner
import product_release_metadata as metadata
import prepare_maintenance_build as maintenance
from prove_consolidated_packages import require
import selected_control_transport as transport

# These are concrete writer projections, not raw assets or arbitrary JSON trees.
SHAPES = {
    '': ['schema mode controller source product line requested_version inventory expected_artifacts excluded_artifacts prerequisites_excluded_from_publication prerequisites selected_dependency_intent histories npm eligible reasons published version_allocated tag_created semantics limits requested_version_input observed_at consumer_feed_policy feed_service_observations'],
    'controller': ['commit tree execution input_sha256'],
    'controller.execution': ['repository event_name ref run_id run_attempt'],
    'source': ['product line kind commit tree parents original_commit original_parents contained_commit contained_tree candidate_register_sha256 original_register_sha256', 'product line kind commit tree observation'],
    'source.observation': ['ref commit tree observed_at branch_observation tag_history tag_observation tag_scope'],
    'source.observation.tag_history[]': ['ref node_id url object'],
    'source.observation.tag_history[].object': ['sha type url'],
    'excluded_artifacts[]': ['id reason'],
    'inventory': ['source_commit source_tree requested_version projects selected excluded release_recipe applicable_tests ownership_policy sha256'],
    'inventory.excluded[]': ['id project reason'],
    'inventory.release_recipe': ['solution sha256 projects workflow workflow_sha256'],
    'inventory.ownership_policy': ['path sha256 lines canonical_owners'],
    'inventory.selected[]': ['id project frameworks include_build_output metadata project_url repository_url symbol_format symbols'],
    'inventory.selected[].metadata': ['status nuspec_sha256 dependency_groups framework_reference_groups restore_assets_sha256 restore_scope original_output_policy original_content_project_sha256 manifest_content_targets sdk_pack_targets_sha256 projection'],
    'inventory.selected[].metadata.dependency_groups[]': ['framework dependencies'],
    'inventory.selected[].metadata.dependency_groups[].dependencies[]': ['id version include exclude'],
    'inventory.selected[].metadata.framework_reference_groups[]': ['framework references'],
    'inventory.selected[].metadata.restore_scope': ['config_files public_sources local_sources'],
    'inventory.selected[].metadata.restore_scope.config_files[]': ['path sha256'],
    'inventory.selected[].metadata.original_output_policy.*': ['IncludeBuildOutput IncludeContentInPack IncludeSymbols SymbolPackageFormat GenerateElsaPackageManifest ElsaPackageManifestIncludeInPackage ElsaPackageManifestPackagePath'],
    'inventory.selected[].metadata.manifest_content_targets[]': ['id version restore_sha512 entry sha256'],
    'inventory.selected[].metadata.projection': ['NoBuild ContinuePackingAfterGeneratingNuspec IncludeBuildOutput ElsaPackageManifestIncludeInPackage IncludeContentInPack'],
    'inventory.projects[]': ['path package_id is_packable is_test_project target_frameworks references_by_framework project_references package_references'],
    'inventory.projects[].references_by_framework.*': ['project_references package_references'],
    'inventory.projects[].references_by_framework.*.project_references[]': ['target_project'],
    'inventory.projects[].references_by_framework.*.package_references[]': ['id Version VersionOverride PrivateAssets IncludeAssets ExcludeAssets'],
    'inventory.projects[].project_references[]': ['target_project'],
    'inventory.projects[].package_references[]': ['id'],
    'prerequisites[]': ['compatibility_scope consumer eligible feeds framework id project range reason version'],
    'prerequisites[].feeds[]': ['applicable_dependency_group compatibility_scope consumer eligible feed framework id metadata observation project range reason version', 'consumer eligible feed framework id observation project range reason version'],
    'prerequisites[].feeds[].metadata': ['groups id version'],
    'prerequisites[].feeds[].metadata.groups[]': ['dependencies framework'],
    'prerequisites[].feeds[].metadata.groups[].dependencies[]': ['id range'],
    'prerequisites[].feeds[].applicable_dependency_group': ['dependencies framework'],
    'prerequisites[].feeds[].applicable_dependency_group.dependencies[]': ['id range'],
    'selected_dependency_intent[]': ['consumer eligible framework id project range version'],
    'histories[]': ['eligible feeds id reason', 'decision eligible id observation reason versions'],
    'histories[].feeds[]': ['decision eligible feed id observation reason versions'],
    'histories[].decision': ['duplicate latest_in_line monotonic normalized reused'],
    'histories[].feeds[].decision': ['duplicate latest_in_line monotonic normalized reused'],
    'npm': ['artifact_proof atomic historical_workflow_executed line manifests packages source_commit source_tree workflow wrapper_dependency_intent'],
    'npm.workflow': ['path sha256'],
    'npm.manifests[]': ['checked_in_dependencies checked_in_version path peer_dependencies sha256'],
    'npm.packages[]': ['expected_tarball id version'],
    'semantics': ['assemblies sdk_version'],
    'semantics.assemblies[]': ['file_version name sha256 version'],
    'consumer_feed_policy': ['config_path config_sha256 history_authority mapping_enabled packages sources'],
    'consumer_feed_policy.sources[]': ['name url'],
    'consumer_feed_policy.packages[]': ['id sources'],
    'feed_service_observations.*': ['base eligible observation'],
}
DYNAMIC = {
    'controller.input_sha256': lambda keys: set(keys) == {
        'scripts/integration-program/plan_product_release.py', 'scripts/integration-program/product_release_metadata.py',
        'scripts/integration-program/ProductReleaseSemantics/Program.cs',
        'scripts/integration-program/ProductReleaseSemantics/ProductReleaseSemantics.csproj'},
    'inventory.ownership_policy.canonical_owners': lambda keys: set(keys) == set(metadata.CANONICAL_OWNERS),
    'inventory.selected[].metadata.original_output_policy': lambda keys: bool(keys) and set(keys) <= {'net8.0', 'net9.0', 'net10.0'},
    'inventory.projects[].references_by_framework': lambda keys: bool(keys) and set(keys) <= {'net7.0', 'net8.0', 'net9.0', 'net10.0'},
    'npm.manifests[].checked_in_dependencies': lambda keys: all(re.fullmatch(r'[@A-Za-z0-9_./-]+', key) for key in keys),
    'npm.manifests[].peer_dependencies': lambda keys: all(re.fullmatch(r'[@A-Za-z0-9_./-]+', key) for key in keys),
    'npm.wrapper_dependency_intent': lambda keys: set(keys) == {'@elsa-workflows/elsa-studio-wasm'},
    'feed_service_observations': lambda keys: bool(keys) and set(keys) <= set(planner.FEED_BASES),
}
ARRAYS = {'source.observation.tag_history'} | set('expected_artifacts excluded_artifacts prerequisites_excluded_from_publication prerequisites selected_dependency_intent histories reasons limits source.parents source.original_parents inventory.projects inventory.selected inventory.excluded inventory.applicable_tests inventory.release_recipe.projects inventory.ownership_policy.lines inventory.selected[].frameworks inventory.selected[].metadata.dependency_groups inventory.selected[].metadata.dependency_groups[].dependencies inventory.selected[].metadata.framework_reference_groups inventory.selected[].metadata.framework_reference_groups[].references inventory.selected[].metadata.restore_scope.config_files inventory.selected[].metadata.restore_scope.public_sources inventory.selected[].metadata.restore_scope.local_sources inventory.selected[].metadata.manifest_content_targets inventory.projects[].target_frameworks inventory.projects[].project_references inventory.projects[].package_references inventory.projects[].references_by_framework.*.project_references inventory.projects[].references_by_framework.*.package_references prerequisites[].feeds prerequisites[].feeds[].metadata.groups prerequisites[].feeds[].metadata.groups[].dependencies prerequisites[].feeds[].applicable_dependency_group.dependencies histories[].feeds histories[].versions histories[].feeds[].versions npm.manifests npm.packages semantics.assemblies consumer_feed_policy.packages consumer_feed_policy.packages[].sources consumer_feed_policy.sources'.split())
BOOL_FIELDS = {'eligible', 'published', 'version_allocated', 'tag_created', 'symbols', 'include_build_output',
    'is_packable', 'is_test_project', 'duplicate', 'monotonic', 'reused', 'atomic', 'artifact_proof',
    'historical_workflow_executed', 'mapping_enabled', 'NoBuild', 'ContinuePackingAfterGeneratingNuspec',
    'IncludeBuildOutput', 'ElsaPackageManifestIncludeInPackage', 'IncludeContentInPack'}
PATH_FIELDS = {'project', 'target_project', 'path', 'solution', 'workflow', 'config_path', 'entry', 'expected_tarball'}
URL_FIELDS = {'url', 'feed', 'base', 'project_url', 'repository_url'}


def public_url(value: str) -> None:
    parts = urlsplit(value)
    require(parts.scheme == 'https' and not parts.username and not parts.password and not parts.fragment and
            not parts.query and parts.netloc in {'github.com', 'raw.githubusercontent.com', 'api.nuget.org',
                'f.feedz.io', 'registry.npmjs.org', 'api.github.com'}, 'selected_plan_url')


def _walk(value, path='', *, now: datetime):
    key = path.rsplit('.', 1)[-1]
    if value is None and path == 'npm':
        return
    if path in ARRAYS:
        require(type(value) is list, 'selected_plan_array_type')
    elif path in SHAPES or path in DYNAMIC or path.endswith(('.observation', '.branch_observation', '.tag_observation')):
        require(type(value) is dict, 'selected_plan_object_type')
    else:
        require(type(value) not in (dict, list), 'selected_plan_scalar_type')
        boolean = key in BOOL_FIELDS and not '.original_output_policy.' in path
        if boolean:
            require(type(value) is bool, 'selected_plan_boolean_type')
        elif key in {'schema', 'bytes'}:
            require(type(value) is int, 'selected_plan_integer_type')
        else:
            require(type(value) is str or value is None, 'selected_plan_scalar_type')
    if type(value) is dict:
        if path in DYNAMIC:
            require(DYNAMIC[path](list(value)), 'selected_plan_map')
            for item, child in value.items():
                if path == 'feed_service_observations':
                    public_url(item)
                _walk(child, path + '.*', now=now)
        elif path.endswith(('.observation', '.branch_observation', '.tag_observation')) and path not in SHAPES:
            require(set(value) == {'url', 'observed_at', 'status'} | (
                {'bytes', 'sha256'} if value.get('status') == 'observed' else set()), 'selected_plan_observation_schema')
            for item, child in value.items():
                _walk(child, path + '.' + item, now=now)
        else:
            require(path in SHAPES and any(set(value) == set(shape.split()) for shape in SHAPES[path]),
                    'selected_plan_object_schema')
            for item, child in value.items():
                _walk(child, path + '.' + item if path else item, now=now)
    elif type(value) is list:
        for child in value:
            _walk(child, path + '[]', now=now)
    elif value is None:
        require(key in {'reason', 'latest_in_line'} or path == 'inventory.excluded[].id', 'selected_plan_null')
    elif type(value) is bool:
        require(key in BOOL_FIELDS, 'selected_plan_boolean')
    elif type(value) is int:
        require((key == 'schema' and value == 1) or (key == 'bytes' and value > 0), 'selected_plan_integer')
    else:
        require(type(value) is str and not any(ord(char) < 32 for char in value), 'selected_plan_string')
        if key in URL_FIELDS or path.endswith('.public_sources[]'):
            if value or key not in {'project_url', 'repository_url'}:
                public_url(value)
        elif key == 'observed_at':
            transport.utc(value, now=now)
        elif key in PATH_FIELDS or path.endswith(('.applicable_tests[]', '.release_recipe.projects[]')):
            transport.safe_name(value)
        elif key.endswith('sha256') or path == 'controller.input_sha256.*':
            transport.digest(value)
        elif key in {'commit', 'tree', 'original_commit', 'contained_commit', 'contained_tree', 'source_commit', 'source_tree', 'sha'} or path.endswith(('.parents[]', '.original_parents[]')):
            transport.digest(value, 40)
        else:
            require(not value.startswith(('/', '\\')) and ':' not in value and not re.match(r'[A-Za-z]:', value),
                    'selected_plan_private_string')


def validate(plan: dict, contracts: dict, *, now: datetime) -> None:
    _walk(plan, now=now)
    require(plan['semantics']['sdk_version'] == metadata.SDK and
            sorted(row['name'] for row in plan['semantics']['assemblies']) ==
            sorted(('NuGet.Versioning', 'NuGet.Frameworks', 'NuGet.Packaging', 'NuGet.Configuration', 'NuGet.Common')),
            'selected_plan_native_identity')
    used_feeds = {check['feed'] for row in plan['histories'] + plan['prerequisites']
                  for check in row.get('feeds', [row]) if 'feed' in check}
    require(set(plan['feed_service_observations']) == used_feeds and
            used_feeds <= {row['url'] for row in plan['consumer_feed_policy']['sources']},
            'selected_plan_feed_service_inventory')
    product, line, source = plan['product'], plan['line'], plan['source']
    require(product in ('core', 'studio', 'extensions') and line in ('3.8', '3.9') and
            (source['product'], source['line']) == (product, line), 'selected_payload_cell_not_implemented')
    require(plan['excluded_artifacts'] == ([] if product == 'studio' else
            [{'id': name, 'reason': 'studio_only'} for name in planner.NPM_IDS]), 'selected_plan_artifact_exclusions')
    if product == 'core':
        pinned = contracts['core']['producer']['sources'][line]
        require(source['kind'] == 'observed-core-release-branch' and
                (source['commit'], source['tree']) == (pinned['commit'], pinned['tree']), 'selected_plan_source_policy')
        observation = source['observation']
        require((observation['ref'], observation['commit'], observation['tree']) ==
                (metadata.CORE_REFS[line], source['commit'], source['tree']) and
                observation['tag_scope'] == 'bounded-matching-ref-snapshot-not-complete-version-authority' and
                observation['branch_observation']['url'] ==
                'https://api.github.com/repos/elsa-workflows/elsa-core/git/ref/' + metadata.CORE_REFS[line].removeprefix('refs/') and
                observation['tag_observation']['url'] ==
                'https://api.github.com/repos/elsa-workflows/elsa-core/git/matching-refs/tags/' + line + '.',
                'selected_plan_core_observation')
        refs = []
        for tag in observation['tag_history']:
            ref, obj = tag['ref'], tag['object']
            require(ref.startswith('refs/tags/' + line + '.') and obj['type'] in ('tag', 'commit') and
                    tag['url'] == 'https://api.github.com/repos/elsa-workflows/elsa-core/git/' + ref and
                    obj['url'] == 'https://api.github.com/repos/elsa-workflows/elsa-core/git/' +
                    ('tags/' if obj['type'] == 'tag' else 'commits/') + obj['sha'], 'selected_plan_core_tag')
            refs.append(ref)
        require(len(refs) == len(set(refs)), 'selected_plan_core_tag_duplicate')
    else:
        rows = [row for row in contracts['candidates']['candidates'] if row['product'] == product and
                row['line'] == line and row['commit'] == metadata.DESCENDANTS[(product, line)]]
        require(len(rows) == 1, 'selected_plan_registered_source')
        row = rows[0]
        expected = {'product': product, 'line': line, 'kind': 'admitted-maintenance-descendant',
            **{key: row[key] for key in ('commit', 'tree', 'parents', 'original_commit', 'original_parents', 'contained_commit', 'contained_tree')},
            'candidate_register_sha256': contracts['candidate_register_sha256'],
            'original_register_sha256': contracts['original_register_sha256']}
        require(source == expected, 'selected_plan_source_policy')
    for project in plan['inventory']['projects']:
        if 'net7.0' in project['target_frameworks'] or 'net7.0' in project['references_by_framework']:
            require(product == 'core' and project['path'] ==
                    'src/extensions/Elsa.Testing.Extensions/Elsa.Testing.Extensions.csproj', 'selected_plan_original_net7_scope')
    for package in plan['inventory']['selected']:
        if not package['project_url'] or not package['repository_url']:
            require(product == 'core' and (package['id'], package['project']) ==
                    ('Elsa.SamplePackage', 'src/apps/Elsa.SamplePackage/Elsa.SamplePackage.csproj'), 'selected_plan_original_empty_url_scope')
    require((plan['npm'] is not None) == (product == 'studio'), 'selected_plan_npm_scope')
    if line == '3.9' and product != 'core':
        pinned = contracts['maintenance39']['sources'][product]
        require((source['commit'], source['tree']) == (pinned['commit'], pinned['tree']) and
                (plan['inventory']['release_recipe']['solution'], plan['inventory']['release_recipe']['workflow']) ==
                (pinned['solution'], pinned['workflow']) and
                {row['path']: row['target_frameworks'] for row in plan['inventory']['projects'] if row['is_test_project']} ==
                pinned['test_projects'] and all(row in plan['inventory']['excluded'] for row in pinned['excluded_canonical']),
                'selected_plan_maintenance39_source')


def admit(plan: dict, plan_hash: str, plan_bytes: bytes, started: str, contracts: dict) -> None:
    """Original historical admission predicates with fixed contracts preloaded."""
    validate(plan, contracts, now=datetime.fromisoformat(started.replace('Z', '+00:00')))
    require(metadata.sha256(plan_bytes) == plan_hash and plan['schema'] == 1 and
            plan['mode'] == 'read-only-product-release-plan' and plan['eligible'] is True and plan['reasons'] == [] and
            all(plan[key] is False for key in ('published', 'version_allocated', 'tag_created')), 'selected_plan_ineligible')
    require(re.fullmatch(re.escape(plan['line']) + r'\.[0-9]+(?:-[0-9A-Za-z.-]+)?', plan['requested_version']), 'selected_plan_version')
    require(plan['controller']['input_sha256'] == contracts['planner_inputs'], 'selected_plan_controller_inputs')
    inventory, binding, version = plan['inventory'], plan['source'], plan['requested_version']
    require((inventory['source_commit'], inventory['source_tree'], inventory['requested_version']) ==
            (binding['commit'], binding['tree'], version) and
            inventory['sha256'] == metadata.canonical_hash(metadata.public_inventory(inventory)) and
            inventory['ownership_policy'] == contracts['ownership'], 'selected_plan_inventory_identity')
    projects, selected = inventory['projects'], inventory['selected']
    metadata.validate_project_references(projects)
    by_path = {row['path']: row for row in projects}
    selected_paths = [row['project'] for row in selected]
    excluded_paths = [row['project'] for row in inventory['excluded']]
    require(projects and selected and len(by_path) == len(projects) and len(set(selected_paths)) == len(selected_paths) and
            len(set(excluded_paths)) == len(excluded_paths) and not set(selected_paths) & set(excluded_paths) and
            set(selected_paths) | set(excluded_paths) == set(by_path) and
            len({row['id'].casefold() for row in selected}) == len(selected), 'selected_plan_inventory_partition')
    graph = metadata.InventoryGraph({'repositories': {'source': {'slug': 'source'}},
                                     'project_inventory': {'source': projects}})
    closure = graph.affected_tests([('source', row['project']) for row in selected])
    require(inventory['applicable_tests'] == sorted(path for _, path in closure), 'selected_plan_test_inventory')
    recipe = set(inventory['release_recipe']['projects'])
    for row in selected:
        project = by_path[row['project']]
        require(project['package_id'] == row['id'] and metadata.project_scope(project, plan['product'], recipe) is None and
                row['frameworks'] == project['target_frameworks'] and
                set(row['metadata']['original_output_policy']) == set(row['frameworks']), 'selected_plan_selected_scope')
    for row in inventory['excluded']:
        require(metadata.project_scope(by_path[row['project']], plan['product'], recipe) == row['reason'], 'selected_plan_exclusion')
    expected = [name for row in selected for name in ([f"{row['id']}.{version}.nupkg"] +
                ([f"{row['id']}.{version}.snupkg"] if row['symbols'] else []))]
    npm = plan['npm']
    if npm:
        expected += [row['expected_tarball'] for row in npm['packages']]
    require(plan['expected_artifacts'] == expected and len(expected) == len(set(expected)) and
            (npm is None or (npm['atomic'] is True and npm['line'] == plan['line'] and
            plan['npm']['source_commit'] == binding['commit'] and plan['npm']['source_tree'] == binding['tree'] and
            [row['id'] for row in plan['npm']['packages']] == list(planner.NPM_IDS) and
            all(row['version'] == version for row in npm['packages']))), 'selected_plan_artifact_partition')
    history_ids = [row['id'].casefold() for row in selected] + ([name.casefold() for name in planner.NPM_IDS] if npm else [])
    require(sorted(row['id'].casefold() for row in plan['histories']) == sorted(history_ids), 'selected_plan_history_inventory')
    from prove_product_release_artifacts import fresh_public
    fresh_public({'status': 'observed', 'observed_at': plan['observed_at'], 'sha256': plan_hash, 'bytes': len(plan_bytes)}, started)
    observations = ([plan['source']['observation'][key] for key in ('branch_observation', 'tag_observation')]
                    if plan['product'] == 'core' else [])
    for row in plan['histories'] + plan['prerequisites']:
        require(row['eligible'] is True and row['reason'] is None, 'selected_plan_prerequisite_ineligible')
        checks = row.get('feeds', [row])
        require(checks, 'selected_plan_feed_inventory')
        for check in checks:
            require(check.get('eligible') is True or (check.get('reason') == 'prerequisite_missing' and
                    check['eligible'] is False and check['observation']['status'] == 'missing'), 'selected_plan_feed_ineligible')
            observations.append(check['observation'])
    for service in plan['feed_service_observations'].values():
        require(service['eligible'] is True, 'selected_plan_feed_ineligible')
        observations.append(service['observation'])
    require(observations, 'selected_plan_observation_inventory')
    for row in observations:
        fresh_public(row, started)
    selected_ids = {row['id'].casefold() for row in selected}
    require(not any(row['id'].casefold() in selected_ids for row in plan['prerequisites']) and
            sorted(plan['prerequisites_excluded_from_publication'], key=str.casefold) ==
            sorted({row['id'] for row in plan['prerequisites']}, key=str.casefold) and
            all(row['eligible'] is True for row in plan['selected_dependency_intent']) and
            all(row['metadata']['status'] == 'observed' for row in selected), 'selected_plan_prerequisite_scope')
