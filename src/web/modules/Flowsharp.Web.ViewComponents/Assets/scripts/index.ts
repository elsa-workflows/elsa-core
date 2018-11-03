///<reference path='../node_modules/@types/jquery/index.d.ts' />
///<reference path='./workflow-editor.ts' />

$(function(){
    $('.workflow-canvas').each((e, i) => {
        const editor=  new WorkflowEditor(e);
    });
});
