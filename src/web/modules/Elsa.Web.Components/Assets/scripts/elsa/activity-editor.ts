///<reference path='../../node_modules/@types/jquery/index.d.ts' />
///<reference path='../../node_modules/@types/bootstrap/index.d.ts' />
///<reference path="activity.ts"/>

namespace Elsa {

    export class ActivityEditor {
        private containerElement: JQuery<HTMLElement>;
        private titleElement: JQuery<HTMLElement>;
        private contentElement: JQuery<HTMLElement>;
        private formElement: JQuery<HTMLFormElement>;
        private activityName: string;
        private deferred: JQuery.Deferred<any>;
        private saveButtonElement: JQuery<HTMLElement>;

        constructor(container: HTMLElement) {
            this.containerElement = $(container);
            this.titleElement = this.containerElement.find('.modal-title');
            this.saveButtonElement = this.containerElement.find('.save-button');
            this.contentElement = this.containerElement.find('.modal-body');
            this.formElement = <JQuery<HTMLFormElement>>this.containerElement.find('form');

            this.formElement.on('submit', this.onFormSubmit);
        }

        public show = () => this.containerElement.modal('show');
        public hide = () => this.containerElement.modal('hide');

        public display = (activityName: string, activity: IActivity): JQuery.Promise<any> => {
            this.activityName = activityName;
            this.deferred = jQuery.Deferred<any>();
            const isNew = activity == null;
            const action = isNew ? 'create' : 'edit';
            const saveButtonText = isNew ? 'Add' : 'Save';
            const activityJson: string = activity == null ? null : JSON.stringify(activity);

            this.saveButtonElement.text(saveButtonText);

            $.ajax({
                type: 'POST',
                url: `/activity/${action}/${activityName}`,
                data: activityJson,
                contentType: 'application/json',
                dataType: 'html'
            }).done(this.displayActivity);

            return this.deferred.promise();
        };

        private displayActivity = (data: any) => {
            const html = <string>data;
            this.contentElement.html(html);
        };

        public get title(): string {
            return this.titleElement.text();
        }

        public set title(value: string) {
            this.titleElement.text(value);
        }

        private onFormSubmit = (e: JQuery.Event) => {
            $.ajax({
                type: 'POST',
                url: `/activity/update/${this.activityName}`,
                data: this.formElement.serialize(),
                dataType: 'html'
            }).done((data: any, textStatus: JQuery.Ajax.SuccessTextStatus, xhr: any) => {
                const activityElement: JQuery = $(data);
                
                if (activityElement.is('.activity-editor-fields')) {
                    this.displayActivity(data);
                }
                else {
                    this.deferred.resolve(data);
                    this.hide();
                }
            });

            e.preventDefault();
        }
    }
}