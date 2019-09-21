const designer = document.querySelector("#designerHost");
let workflow = null;

//designer.addEventListener('componentReady', onWorkflowDesignerReady);

function exportWorkflow() {
    designer.export({
        format: 'json',
        fileExtension: '.json',
        mimeType: 'application/json',
        displayName: 'JSON'
    });
}

function onWorkflowDesignerReady() {
    const input = document.querySelector('[data-workflow]');
    const json = input.attributes['data-workflow'].value;

    if (!json)
        return;

    designer.workflow = workflow = JSON.parse(json);
}

// Temporary workaround until I figure out how to listen for the workflow designer component's ready event.
setTimeout(onWorkflowDesignerReady, 100);