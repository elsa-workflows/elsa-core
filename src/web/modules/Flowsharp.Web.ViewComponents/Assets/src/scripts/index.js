import * as flowsharp from './workflow-editor.js'
import $ from 'jquery';
import '../styles/index.scss';

$(function(){
    $('.workflow-canvas').each((e, i) => {
        const editor=  new flowsharp.WorkflowEditor(e);
    });
});
