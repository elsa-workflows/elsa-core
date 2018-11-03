///<reference path='../node_modules/@types/jquery/index.d.ts' />
///<reference path='./workflow-editor.ts' />

$(function(){
    $('.workflow-canvas').each((i, e) => {
        const editor=  new WorkflowEditor(e);
    });
});
