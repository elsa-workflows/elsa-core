///<reference path='../../../node_modules/@types/jquery/index.d.ts' />
///<reference path='./designer.ts' />

namespace Flowsharp {
    $(function () {
        $('.workflow-canvas').each((i, e) => {
            const editor = new WorkflowDesigner(e);
        });
    });
}