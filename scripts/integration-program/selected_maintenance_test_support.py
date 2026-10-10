from unittest.mock import patch

from product_artifact_execution import local_execution as create_local_execution


def patch_offline_local_execution(module):
    return patch.object(
        module,
        'selected_execution',
        side_effect=lambda *_args, **_kwargs: create_local_execution({})
    )
