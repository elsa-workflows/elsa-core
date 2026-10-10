from unittest.mock import patch

from product_artifact_execution import local_execution as create_local_execution


def patch_offline_local_execution(module):
    """Patch a module's execution factory to produce local identities in offline tests."""
    return patch.object(
        module,
        'selected_execution',
        side_effect=lambda *_args, **_kwargs: create_local_execution({})
    )
