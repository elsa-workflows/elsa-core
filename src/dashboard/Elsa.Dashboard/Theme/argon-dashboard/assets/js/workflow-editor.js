const designer = document.querySelector("#designerHost");

designer.addEventListener('workflowChanged', onWorkflowChanged);
//designer.addEventListener('componentReady', onWorkflowDesignerReady);

function addActivity() {
    designer.showActivityPicker();
}

function createNewWorkflow() {
    if (confirm('Are you sure you want to discard current changes?'))
        designer.newWorkflow();
}

function importWorkflow() {
    designer.import();
}

function exportWorkflow() {
    designer.export({
        format: 'json',
        fileExtension: '.json',
        mimeType: 'application/json',
        displayName: 'JSON'
    });
}

function onWorkflowChanged(e) {
    const workflow = e.detail;
    const json = JSON.stringify(workflow);
    const input = document.querySelector('#workflowData');
    
    input.value = json;
}

function onWorkflowDesignerReady(){
    debugger;
    const input = document.querySelector('#workflowData');
    const json = input.value;
    
    if(!json)
        return;

    designer.workflow = JSON.parse(json);
}

setTimeout(onWorkflowDesignerReady, 500);