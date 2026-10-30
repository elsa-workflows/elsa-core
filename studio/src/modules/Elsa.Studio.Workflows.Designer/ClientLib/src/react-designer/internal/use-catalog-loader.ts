import { useCallback, useRef, useState } from 'react';
import type { DotNetReactDesigner } from '../dotnet-bridge';
import type { ActivityDescriptorDto } from '../types';

/**
 * Lazily fetches the activity catalog from C# the first time it's needed
 * (when the user opens an activity picker — either via port-drag-end or via
 * the + button on an edge). Subsequent calls are no-ops.
 *
 * Keeping the load deferred avoids paying the JSInterop cost on designer
 * mount; the data is only useful once the user actually invokes the picker.
 */
export function useCatalogLoader(interop: DotNetReactDesigner) {
    const [catalog, setCatalog] = useState<ActivityDescriptorDto[]>([]);
    const [loading, setLoading] = useState(false);
    const loadedRef = useRef(false);

    const ensureLoaded = useCallback(() => {
        if (loadedRef.current) return;
        loadedRef.current = true;
        setLoading(true);
        interop.getAvailableActivities()
            .then(setCatalog)
            .catch(() => setCatalog([]))
            .finally(() => setLoading(false));
    }, [interop]);

    return { catalog, loading, ensureLoaded };
}
