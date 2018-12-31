///<reference path="../../../Elsa.Web.Components/Assets/scripts/elsa/workflow-designer.ts"/>
///<reference path="../../../Elsa.Web.Components/Assets/scripts/elsa/activity-picker.ts"/>
///<reference path="@types/jquery.d.ts"/>

namespace Elsa {
    export class WorkflowEditor {
        private readonly workflowId: string;
        private readonly workflowDesigner: WorkflowDesigner;
        private readonly activityPicker: ActivityPicker;
        private readonly activityEditor: ActivityEditor;
        private readonly containerElement: JQuery<HTMLElement>;

        constructor(container: HTMLElement,) {
            this.containerElement = $(container);

            const saveButton = this.containerElement.find('#save-workflow-button');
            const exportButtons = this.containerElement.find('.export-workflow-button');

            this.workflowDesigner = new WorkflowDesigner($('#elsa-workflow-designer')[0]);
            this.activityPicker = new ActivityPicker($('#elsa-activity-picker')[0]);
            this.activityEditor = new ActivityEditor($('#elsa-activity-editor')[0]);
            this.workflowId = this.containerElement.data('workflow-id');

            saveButton.on('click', this.onSaveClick);
            exportButtons.on('click', this.onExportClick);

            this.activityPicker.addEventListener('activity-selected', this.onActivityPicked);
            this.workflowDesigner.addEventListener('edit-activity', this.onEditActivity);
        }

        private onEditActivity = (e: CustomEvent) => {
            const activityInfo: ActivityInfo = e.detail.activityInfo;
            this.activityEditor.title = `Edit ${activityInfo.activityDisplayText}`;
            this.activityEditor.show();
            this.activityEditor
                .display(activityInfo.activityName, activityInfo.activity)
                .done((updatedHtml: string) => this.workflowDesigner.updateActivityElement(activityInfo.activityElement, updatedHtml));
        };

        private onActivityPicked = (e: CustomEvent) => {
            this.activityPicker.hide();

            const selectedActivityInfo: SelectedActivityInfo = e.detail;
            const activityName: string = selectedActivityInfo.activityName;
            const activityDisplayText = selectedActivityInfo.activityDisplayText;
            this.activityEditor.title = `Add ${activityDisplayText}`;
            this.activityEditor.show();

            this.activityEditor
                .display(activityName, null)
                .done((activityHtml: string) => this.workflowDesigner.addActivity(activityHtml));
        };

        private onSaveClick = (e: JQuery.Event) => {
            e.preventDefault();
            const workflow = this.workflowDesigner.getWorkflow();
            const workflowJson = JSON.stringify(workflow);
            const isNew = this.workflowId === null || this.workflowId == '';
            const url = isNew ? '/workflows' :`/workflows/${this.workflowId}`;
            const method = isNew ? 'POST' : 'PUT';

            $.ajax({
                url: url,
                type: method,
                data: workflowJson,
                contentType: 'application/json',
                dataType: 'json'
            }).done(this.handleSaveResult);
        };

        private onExportClick = (e: JQuery.Event) => {
            e.preventDefault();
            const workflowJson = this.serializeWorkflow(false);
            const format = $(e.target).attr('href').substr(1);

            $.ajax({
                url: `/workflows/${this.workflowId}/download?format=${format}`,
                method: 'POST',
                data: workflowJson,
                dataType: 'binary',
                processData: false,
                contentType: 'application/json',
                xhrFields: {
                    responseType: 'blob'
                },
                success: (data) => {
                    this.downloadData(data);
                }
            });
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
        
        private handleSaveResult = (data: any) => {
          if(data.redirect)
          {
              window.location.href = data.redirect;
          }
          else{
              this.displayNotification();
          }
        };

        private displayNotification = () => {
            $.notify({
                message: 'Workflow saved!',
            }, {
                type: 'success',
                newest_on_top: true,
                delay: 1000,
                animate: {
                    enter: 'animated fadeInDown',
                    exit: 'animated fadeOutUp'
                },
                placement: {
                    align: 'right',
                    from: 'top'
                }
            });
        };
    }

    $(() => {
        $('.workflow-editor-container').each((i, e) => {
            const editor = new WorkflowEditor(e);
        });
    });
}