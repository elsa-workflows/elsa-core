"""Retire only owned, fully verified cell caches in explicit bounded-disk mode."""
from __future__ import annotations

import hashlib
import json
import os
from pathlib import Path
import re
import shutil


def retire_successful_cell_caches(private: Path, cell: Path, result: dict) -> None:
    """Persist completed proof before retiring the two fixed private cache roots."""
    if result.get('success') is not True or not isinstance(result.get('id'), str) or not result['id']:
        raise ValueError('consumer_cache_retirement_incomplete')
    framework = result.get('framework')
    if not isinstance(framework, str) or not re.fullmatch(r'net[0-9]+\.[0-9]+', framework):
        raise ValueError('consumer_cache_retirement_identity')
    name = ('runtime-' + framework if 'runtime' in result else
            hashlib.sha256((result['id'] + '/' + framework).encode()).hexdigest()[:16])
    if not private.is_absolute() or cell != private / name:
        raise ValueError('consumer_cache_retirement_ownership')
    caches = (cell / 'packages', cell / 'discovery/packages')
    # Validate both complete trees before removing either one. Never follow links,
    # including links in an ancestor of the run's freshly created private root.
    for cache in caches:
        if any(path.is_symlink() for path in (cache, *cache.parents)) or cache.resolve(strict=True) != cache:
            raise ValueError('consumer_cache_retirement_path')
        if not cache.is_dir():
            raise ValueError('consumer_cache_retirement_directory')
        pending = [cache]
        while pending:
            for path in pending.pop().iterdir():
                if path.is_symlink():
                    raise ValueError('consumer_cache_retirement_symlink')
                if path.is_dir():
                    pending.append(path)
                elif not path.is_file():
                    raise ValueError('consumer_cache_retirement_file')
    if not shutil.rmtree.avoids_symlink_attacks:
        raise ValueError('consumer_cache_retirement_unsafe_platform')
    data = (json.dumps(result, indent=2, sort_keys=True, allow_nan=False) + '\n').encode()
    with (cell / 'cell-proof.private.json').open('xb') as proof:
        proof.write(data)
        proof.flush()
        os.fsync(proof.fileno())
    # Errors propagate: even a partial retirement cannot become full acceptance.
    for cache in caches:
        shutil.rmtree(cache)
        if cache.exists() or cache.is_symlink():
            raise ValueError('consumer_cache_retirement_failed')
