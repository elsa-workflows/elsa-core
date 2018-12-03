///<reference path="../../../Flowsharp.Web.ViewComponents/Assets/scripts/flowsharp/workflow-designer.ts"/>

namespace Flowsharp {
    const win: any = window;

    export class WorkflowEditor {
        private workflowDesigner: WorkflowDesigner;
        private containerElement: JQuery<HTMLElement>;
        private workflowId: string;

        constructor(container: HTMLElement,) {
            this.containerElement = $(container);
            
            const saveButton = this.containerElement.find('.save-workflow-button');
            const downloadButton = this.containerElement.find('.download-workflow-button');
            
            this.workflowDesigner = (<any>window).Flowsharp.workflowDesigner;
            this.workflowId = this.containerElement.data('workflow-id');

            saveButton.on('click', this.onSaveButtonClick);
            downloadButton.on('click', this.onDownloadButtonClick);
        }

        private onSaveButtonClick = (e: JQuery.Event) => {
            e.preventDefault();
            const workflow = this.workflowDesigner.getWorkflow();
            const workflowJson = JSON.stringify(workflow);

            $.ajax({
                url: `/workflows/${this.workflowId}`,
                type: 'PUT',
                data: workflowJson,
                contentType: 'application/json',
                dataType: 'json'
            }).done(this.displayNotification);
        };

        private onDownloadButtonClick = (e: JQuery.Event) => {
            e.preventDefault();
            const workflowJson = this.serializeWorkflow(false);
            
            $.ajax({
                url: `/workflows/${this.workflowId}/download`,
                method: 'POST',
                data: workflowJson,
                contentType: 'application/json',
                xhrFields: {
                    responseType: 'blob'
                },
                success: (data) => {
                    this.downloadData(data);
                }
            });
        };
        
        private downloadJson = () => {
            const workflowJson = this.serializeWorkflow(true);
            const data = new Blob([workflowJson], { type: 'text/yaml' });
            this.downloadData(data);
        };

        private downloadData = (data: any) => {
            const a = document.createElement('a');
            const url = window.URL.createObjectURL(data);
            a.href = url;
            a.download = this.workflowId;
            a.click();
            window.URL.revokeObjectURL(url);
        };
        
        private serializeWorkflow = (pretty: boolean): string => {
            const workflow = this.workflowDesigner.getWorkflow();
            return JSON.stringify(workflow, null, pretty ? 2 : null);
        };

        private displayNotification = () => {
            alert("Saved!");
        };
    }

    $(() => {
        $('.workflow-editor-container').each((i, e) => {
            const editor = new WorkflowEditor(e);
        });
    });
}