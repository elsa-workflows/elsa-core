// Hosts one BPMN designer for BpmnCanvasHost. It imports the designer's module on demand, refuses a module that is not
// the designer the host chose or that implements a contract version it does not support, and forwards the designer's
// events to .NET. See studio/docs/specs/bpmn-designer-contract/contract.md.

const SUPPORTED_CONTRACT_VERSION = 1;

/**
 * Creates the designer `designerId` that `moduleUrl` exports, drawing into `container`, and returns the designer's
 * methods for .NET to call. `listener` is the component's DotNetObjectReference. URLs are relative to the page's base
 * URL, not to this file.
 */
export async function createDesigner(designerId, moduleUrl, stylesheetUrl, container, readOnly, listener) {
    if (stylesheetUrl) addStylesheet(stylesheetUrl);

    const module = (await import(new URL(moduleUrl, document.baseURI).href)).default;

    if (module?.designerId !== designerId)
        throw new Error(`The BPMN designer module '${moduleUrl}' declares the id '${module?.designerId}', not '${designerId}'.`);

    if (module.contractVersion !== SUPPORTED_CONTRACT_VERSION)
        throw new Error(`The BPMN designer module '${moduleUrl}' implements contract version ${module.contractVersion}, but this host supports version ${SUPPORTED_CONTRACT_VERSION}.`);

    const designer = module.createDesigner(container, { readOnly, propertiesContainer: null });

    // A notification can lose a race with the component's disposal; there is nothing left to tell then.
    const notify = (method, event) => listener.invokeMethodAsync(method, event).catch(() => {});
    const subscriptions = [
        designer.on('change', event => notify('OnDesignerChanged', event)),
        designer.on('selection', event => notify('OnDesignerSelectionChanged', event))
    ];

    return {
        load: xml => designer.load(xml),
        save: () => designer.save(),
        select: elementId => designer.select(elementId),
        getBinding: elementId => designer.getBinding(elementId),
        setBinding: (elementId, binding) => designer.setBinding(elementId, binding),
        undo: () => designer.undo(),
        redo: () => designer.redo(),
        applyOverlays: statsById => designer.applyOverlays(statsById),
        clearOverlays: () => designer.clearOverlays(),
        // Problem markers are optional in the contract. Null tells .NET that this designer has none.
        applyMarkers: markers => (typeof designer.applyMarkers === 'function' ? designer.applyMarkers(markers) : null),
        clearMarkers: () => (typeof designer.clearMarkers === 'function' ? designer.clearMarkers() : null),
        dispose() {
            for (const unsubscribe of subscriptions.splice(0)) unsubscribe();
            designer.dispose();
        }
    };
}

function addStylesheet(url) {
    const href = new URL(url, document.baseURI).href;

    if ([...document.querySelectorAll('link[rel="stylesheet"]')].some(link => link.href === href)) return;

    const link = document.createElement('link');
    link.rel = 'stylesheet';
    link.href = href;
    document.head.appendChild(link);
}
