const designer = document.querySelector("#designerHost");
const modal = document.querySelector("#workflow-properties-modal");
let workflow = null;

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

function onWorkflowPropertiesSubmit(e) {
    e.preventDefault();
    const formData = new FormData(e.target);
    const name = formData.get("Name").toString();
    const description = formData.get("Description").toString();
    const isSingleton = formData.get("IsSingleton") === "true";
    const isDisabled = formData.get("IsDisabled") === "true";

    designer.workflow = {...workflow, name, description, isSingleton, isDisabled};
    
    const editorCaption = document.querySelector("#editorCaption");
    const editorDescription = document.querySelector("#editorDescription");
    
    editorCaption.innerHTML = name;
    editorDescription.innerHTML = description;

    $(modal).modal('hide');
}

function onWorkflowChanged(e) {
    const workflow = e.detail;
    const json = JSON.stringify(workflow);
    const input = document.querySelector('#workflowData');

    input.value = json;
}

// Temporary workaround until I figure out how to listen for the workflow designer component's ready event.
setTimeout(onWorkflowDesignerReady, 100);