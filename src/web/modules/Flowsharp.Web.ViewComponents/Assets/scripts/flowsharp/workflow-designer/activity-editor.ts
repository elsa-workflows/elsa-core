///<reference path='../../../node_modules/@types/jquery/index.d.ts' />
///<reference path='../../../node_modules/@types/bootstrap/index.d.ts' />

namespace Flowsharp {

    export class ActivityEditor {
        public show = () => this.containerElement.modal('show');
        public hide = () => this.containerElement.modal('hide');
        public display = (activityName: string) => $.ajax({
            type: 'POST',
            url: `/activity/display/${activityName}`,
            data: {},
            contentType: 'application/json',
            dataType: 'json'
        }).done(this.displayActivity);
        private baseUrl: string;
        private containerElement: JQuery<HTMLElement>;
        private titleElement: JQuery<HTMLElement>;
        private contentElement: JQuery<HTMLElement>;
        private displayActivity = (data: any) => {
            const html = <string>data;
            this.contentElement.html(html);
        };

        constructor(container: HTMLElement) {
            this.containerElement = $(container);
            this.titleElement = this.containerElement.find('.modal-title');
            this.contentElement = this.containerElement.find('.modal-body');
            this.baseUrl = this.containerElement.data('base-url');
        }

        public get title(): string {
            return this.titleElement.text();
        }

        public set title(value: string) {
            this.titleElement.text(value);
        }
    }
}