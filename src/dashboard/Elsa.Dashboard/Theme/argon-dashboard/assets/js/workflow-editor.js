const designer = document.querySelector("#designerHost");
const modal = document.querySelector("#workflow-properties-modal");
let workflow = null;

designer.addEventListener('workflowChanged', onWorkflowChanged);

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
    const isDisabled = formData.get("IsDisabled").toString() === 'true';
    const isSingleton = formData.get("IsSingleton").toString() === 'true';
    const editorCaption = document.querySelector("#editorCaption");
    const editorDescription = document.querySelector("#editorDescription");
    const workflowNameInput = document.querySelector("#workflowName");
    const workflowDescriptionInput = document.querySelector("#workflowDescription");
    const workflowIsDisabledInput = document.querySelector("#workflowIsDisabled");
    const workflowSingletonInput = document.querySelector("#workflowIsSingleton");
    
    editorCaption.innerHTML = workflowNameInput.value = name;
    editorDescription.innerHTML = workflowDescriptionInput.value = description;
    workflowIsDisabledInput.value = isDisabled.toString();
    workflowSingletonInput.value = isSingleton.toString();

    $(modal).modal('hide');
}

function onWorkflowChanged(e) {
    const workflow = e.detail;
    const json = JSON.stringify(workflow);
    const input = document.querySelector('#workflowData');

    input.value = json;
}