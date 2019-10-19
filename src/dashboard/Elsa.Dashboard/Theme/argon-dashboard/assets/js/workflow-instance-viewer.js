const designer = document.querySelector("#designerHost");

function exportWorkflow() {
    designer.export({
        format: 'json',
        fileExtension: '.json',
        mimeType: 'application/json',
        displayName: 'JSON'
    });
}

$(() => {
    const $workflowViewer = $('#workflowViewer');
    const $executionLog = $('#executionLog');

    $workflowViewer.on('mouseenter', 'wf-designer .activity', e => {
        const activityId = $(e.currentTarget).data('activity-id');

        $executionLog.find(`tr[data-activity-id="${activityId}"]`).addClass('table-row-dark');
    }).on('mouseleave', 'wf-designer .activity', e => {
        const activityId = $(e.currentTarget).data('activity-id');

        $executionLog.find(`tr[data-activity-id="${activityId}"]`).removeClass('table-row-dark');
    });

    $executionLog.on('mouseenter', 'tbody tr', e => {
        const activityId = $(e.currentTarget).data('activity-id');

        $workflowViewer.find(`[data-activity-id="${activityId}"]`).addClass('highlight');
    }).on('mouseleave', 'tbody tr', e => {
        const activityId = $(e.currentTarget).data('activity-id');

        $workflowViewer.find(`[data-activity-id="${activityId}"]`).removeClass('highlight');
    });
});