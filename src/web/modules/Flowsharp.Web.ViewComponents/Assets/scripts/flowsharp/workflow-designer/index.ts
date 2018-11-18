///<reference path='../../../node_modules/@types/jquery/index.d.ts' />
///<reference path='./designer.ts' />

namespace Flowsharp {
    $(function () {
        $('.workflow-designer-container').each((i, e) => {
            const canvasContainer = $(e).find('.workflow-canvas')[0];
            const activityEditorContainer = $(e).find('.activity-editor-modal')[0];
            const editor = new WorkflowDesigner(canvasContainer, activityEditorContainer);
        });
    });
}