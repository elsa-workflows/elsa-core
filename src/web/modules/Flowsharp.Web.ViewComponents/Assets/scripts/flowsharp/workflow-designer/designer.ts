///<reference path='../../../node_modules/@types/jquery/index.d.ts' />
///<reference path='../../@types/jsplumb.d.ts' />
///<reference path='../models/index.d.ts' />
///<reference path='./config.ts' />
///<reference path='./activity-editor.ts' />

namespace Flowsharp {
    export class WorkflowDesigner {
        private plumber: any;
        private activityElements: any;
        private activities: any[];
        private static onDragOver = (e: JQuery.Event) => {
            e.preventDefault();
        };
        private activityEditor: ActivityEditor;
        private onDrop = (e: JQuery.Event) => {
            e.preventDefault();

            const dragEvent = <DragEvent>e.originalEvent;
            const activityDescriptor: ActivityDescriptor = JSON.parse(dragEvent.dataTransfer.getData('activity-descriptor-json'));
            const activityName = activityDescriptor.name;

            this.activityEditor.title = `Add ${activityDescriptor.displayText}`;
            this.activityEditor.display(activityName);
            this.activityEditor.show();
        }

        constructor(private readonly container: HTMLElement, private readonly activityEditorContainer: HTMLElement) {
            this.readActivities();
            this.plumber = WorkflowDesignerConfig.createJsPlumbInstance(container);
            this.initializeNodes();
            this.initializeDropTarget();
            this.activityEditor = new ActivityEditor(activityEditorContainer);
        }

        private readActivities() {
            const activityElements: any = $(this.container).find('.activity');
            const activities = [];

            for (let index = 0; index < activityElements.length; index++) {
                const activityElement: any = $(activityElements[index]);
                const activity: any = activityElement.data['activity-json'];

                activities.push(activity);
            }

            this.activityElements = activityElements;
            this.activities = activities;
        }

        private initializeNodes() {
            // Suspend drawing and initialize.
            this.plumber.batch(() => {
                let activityElements = this.activityElements;

                for (let index = 0; index < activityElements.length; index++) {
                    const activityElement = activityElements[index];
                    const activity = this.activities[index];

                    this.initializeElement(activityElement, activity);
                }

                // Connect activities.
                //this.updateConnections();

                // Make all activity elements visible.
                activityElements.show();
            });
        }

        private initializeElement(activityElement: HTMLElement, activity: Activity) {

            const $activityElement = $(activityElement);

            this.plumber.draggable(activityElement, {
                grid: [10, 10],
                containment: true,
                // start: args => {
                //     //this.dragStart = {left: args.e.screenX, top: args.e.screenY};
                // },
                // stop: args => {
                //     //this.hasDragged = this.dragStart.left !== args.e.screenX || this.dragStart.top !== args.e.screenY;
                // }
            });

            // Configure the activity as a target.
            this.plumber.makeTarget(activityElement, {
                dropOptions: {hoverClass: 'hover'},
                anchor: 'Continuous',
                endpoint: ['Blank', {radius: 8}]
            });

            // Add source endpoints.
            this.addSourceEndpoints(activity, activityElement);
        }

        private addSourceEndpoints(activity: any, activityElement: any) {
            for (let i = 0; i < activity.endpoints.length; i++) {
                const endpoint: any = activity.endpoints[i];
                const sourceEndpointOptions: any = WorkflowDesignerConfig.getSourceEndpointOptions(activity.id, endpoint.name);
                this.plumber.addEndpoint(activityElement, {
                    connectorOverlays: [['Label', {
                        label: endpoint.name,
                        cssClass: 'connection-label'
                    }]]
                }, sourceEndpointOptions);
            }
        }

        private initializeDropTarget() {
            $(this.container).on('dragover', WorkflowDesigner.onDragOver);
            $(this.container).on('drop', this.onDrop)
        }
    }
}