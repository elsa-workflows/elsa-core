from unittest.mock import patch

import product_release_metadata as metadata
import prepare_maintenance_build as maintenance
from product_artifact_execution import local_execution as create_local_execution


def patch_offline_local_execution(module):
    """Patch a module's execution factory to produce local identities in offline tests."""
    return patch.object(
        module,
        'selected_execution',
        side_effect=lambda *_args, **_kwargs: create_local_execution({})
    )


def registered_maintenance_selection(product, line, commit):
    """Return a registered maintenance row and its exact plan source binding."""
    rows = maintenance.registered_core_candidates(maintenance.load_register())
    matches = [row for row in rows
               if (row.get('product'), row.get('line'), row.get('kind'), row.get('commit')) ==
               (product, line, 'maintenance', commit)]
    if len(matches) != 1:
        raise ValueError(f'Expected one registered maintenance row for {product} {line}.')
    row = matches[0]
    source = {'product': product, 'line': line, 'kind': 'admitted-maintenance-descendant',
        **{key: row[key] for key in ('commit', 'tree', 'parents', 'original_commit', 'original_parents',
                                     'contained_commit', 'contained_tree')},
        'candidate_register_sha256': metadata.sha256(maintenance.CANDIDATES.read_bytes()),
        'original_register_sha256': metadata.sha256(maintenance.REGISTER.read_bytes())}
    return row, source
