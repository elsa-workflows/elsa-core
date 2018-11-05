///<reference path='../../../node_modules/@types/jquery/index.d.ts' />
///<reference path='../models/index.d.ts' />
///<reference path='../../decode-html/decode-html.ts' />

class ActivityLibrary {

    private readonly activities: JQuery<HTMLElement>;

    constructor(private container: HTMLElement) {
        this.activities = $(container).find('.activity');
        this.initialize();
    }

    initialize() {
        this.activities.on('dragstart', ActivityLibrary.onDragStart)
    }

    static onDragStart(e: JQuery.Event) {
        const element: JQuery<HTMLElement> = $(<HTMLElement>e.target);
        const descriptor: ActivityDescriptor = JSON.parse(decode.decodeHTMLEntities(element.data('activity-descriptor-json')));
        const activityName: string = descriptor.name;
        const dragEvent = <DragEvent>e.originalEvent;

        dragEvent.dataTransfer.setData('activity-name', activityName);
    }
}