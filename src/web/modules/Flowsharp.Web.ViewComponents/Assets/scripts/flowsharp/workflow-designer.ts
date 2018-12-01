///<reference path='../../node_modules/@types/jquery/index.d.ts' />
///<reference path='../@types/jsplumb.d.ts' />
///<reference path='./workflow-designer-config.ts' />
///<reference path='./activity-editor.ts' />
///<reference path="activity-descriptor.ts"/>

namespace Flowsharp {
    export class WorkflowDesigner {
        private plumber: any;
        private activityEditor: ActivityEditor;

        constructor(private readonly container: HTMLElement, private readonly activityEditorContainer: HTMLElement) {
            this.plumber = WorkflowDesignerConfig.createJsPlumbInstance(container);
            this.initializeNodes();
            this.initializeDropTarget();
            this.activityEditor = new ActivityEditor(activityEditorContainer);
        }
        
        private static onDragOver = (e: JQuery.Event) => {
            e.preventDefault();
        };

        private editActivity = (activityElement: JQuery<HTMLElement>) => {
            const displayText = activityElement.attr('title');
            const activityName = activityElement.data('activity-name');
            const activity:IActivity = activityElement.data('activity-model');
            this.activityEditor.title = `Edit ${displayText}`;
            this.activityEditor.show();
            this.activityEditor.display(activityName, activity).done(this.updateActivity);
        };

        private addActivity = (activityHtml: string, x: number, y: number) => {
            const activityElement = $(activityHtml);
            
            activityElement.css({ x: x, y: y });
            this.container.appendChild(activityElement[0]);
            this.initializeElement(activityElement);
        };

        private updateActivity = (activityHtml: string) => {
            const activityElement = $(activityHtml);
            //this.initializeElement(activityElement);
        };

        private initializeNodes = () => {
            this.plumber.batch(() => {
                let activityElements = $(this.container).find('.activity');

                for (let index = 0; index < activityElements.length; index++) {
                    const activityElement = $(activityElements[index]);
                    this.initializeElement(activityElement);
                }

                //this.updateConnections();

                activityElements.show();
            });
        };

        private initializeElement = (activityElement: JQuery<HTMLElement>) => {
            const activityId: string = activityElement.data('activity-id');
            const activityEndpoints: any[] = activityElement.data('activity-endpoints');
            
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
            
            this.plumber.makeTarget(activityElement, {
                dropOptions: {hoverClass: 'hover'},
                anchor: 'Continuous',
                endpoint: ['Blank', {radius: 8}]
            });
            
            this.addSourceEndpoints(activityElement[0], activityId, activityEndpoints);
            
            activityElement.on('dblclick', this.onDoubleClick);
        };

        private addSourceEndpoints = (activityElement: HTMLElement, activityId: string, endpoints: any[]) => {
            for (let i = 0; i < endpoints.length; i++) {
                const endpoint: any = endpoints[i];
                const sourceEndpointOptions: any = WorkflowDesignerConfig.getSourceEndpointOptions(activityId, endpoint.name);
                this.plumber.addEndpoint(activityElement, {
                    connectorOverlays: [['Label', {
                        label: endpoint.name,
                        cssClass: 'connection-label'
                    }]]
                }, sourceEndpointOptions);
            }
        };

        private initializeDropTarget = () => {
            $(this.container).on('dragover', WorkflowDesigner.onDragOver);
            $(this.container).on('drop', this.onDrop)
        };

        private onDrop = (e: JQuery.Event) => {
            e.preventDefault();

            const dragEvent = <DragEvent>e.originalEvent;
            const activityDescriptor: ActivityDescriptor = JSON.parse(dragEvent.dataTransfer.getData('activity-descriptor-json'));
            const activityName = activityDescriptor.name;
            const x = dragEvent.offsetX;
            const y = dragEvent.offsetY;

            this.activityEditor.title = `Add ${activityDescriptor.displayText}`;
            this.activityEditor.show();
            this.activityEditor.display(activityName, null).done((data: string) => this.addActivity(data, x, y));
        };

        private onDoubleClick = (e: JQuery.Event) => {
            e.preventDefault();

            const activityElement: JQuery<HTMLElement> = $(<HTMLElement>e.originalEvent.currentTarget);
            this.editActivity(activityElement);
        };
    }

    $(function () {
        $('.workflow-designer-container').each((i, e) => {
            const canvasContainer = $(e).find('.workflow-canvas')[0];
            const activityEditorContainer = $(e).find('.activity-editor-modal')[0];
            const editor = new WorkflowDesigner(canvasContainer, activityEditorContainer);
        });
    });
}