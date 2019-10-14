const designer = document.querySelector("#designerHost");

function exportWorkflow() {
    designer.export({
        format: 'json',
        fileExtension: '.json',
        mimeType: 'application/json',
        displayName: 'JSON'
    });
}