///<reference path='../../../node_modules/@types/jquery/index.d.ts' />
///<reference path='../models/index.d.ts' />
///<reference path='../../decode-html/decode-html.ts' />

namespace Flowsharp {
    export class ActivityLibrary {

        private readonly activities: JQuery<HTMLElement>;

        constructor(private container: HTMLElement) {
            this.activities = $(container).find('.activity');
            this.initialize();
        }

        private static onDragStart(e: JQuery.Event) {
            const element: JQuery<HTMLElement> = $(<HTMLElement>e.target);
            const activityDescriptorJson: string = decode.decodeHTMLEntities(element.data('activity-descriptor-json'));
            const dragEvent = <DragEvent>e.originalEvent;

            dragEvent.dataTransfer.setData('activity-descriptor-json', activityDescriptorJson);
        }

        private initialize() {
            this.activities.on('dragstart', ActivityLibrary.onDragStart)
        }
    }
}