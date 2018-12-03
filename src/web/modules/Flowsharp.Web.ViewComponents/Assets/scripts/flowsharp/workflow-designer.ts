///<reference path='../../node_modules/@types/jquery/index.d.ts' />
///<reference path='../@types/jsplumb.d.ts' />
///<reference path='./workflow-designer-config.ts' />
///<reference path='./activity-editor.ts' />
///<reference path="activity-descriptor.ts"/>
///<reference path="workflow.ts"/>

namespace Flowsharp {
    export class WorkflowDesigner {
        private plumber: any;
        private activityEditor: ActivityEditor;
        private dragStart: { left: number; top: number };
        private hasDragged: boolean;

        constructor(private readonly container: HTMLElement, private readonly activityEditorContainer: HTMLElement) {
            this.plumber = WorkflowDesignerConfig.createJsPlumbInstance(container);
            this.initializeNodes();
            this.initializeDropTarget();
            this.activityEditor = new ActivityEditor(activityEditorContainer);
        }

        public getWorkflow = (): IWorkflow => {
            const containerElement = $(this.container);
            const workflowMetadata: IWorkflowMetadata = containerElement.data('workflow-metadata');
            const workflowStatus: WorkflowStatus = containerElement.data('workflow-status');
            const activityElements = containerElement.find('.activity');
            const activities: IActivity[] = [];

            for (let index = 0; index < activityElements.length; index++) {
                const activityElement = $(activityElements[index]);
                const activity: IActivity = activityElement.data('activity-model');

                activities.push(activity);
            }

            return {
                metadata: workflowMetadata,
                activities: activities,
                connections: [],
                status: workflowStatus
            };
        };

        private editActivity = (activityElement: JQuery<HTMLElement>) => {
            const displayText = activityElement.attr('title');
            const activityName = activityElement.data('activity-name');
            const activity: IActivity = activityElement.data('activity-model');
            this.activityEditor.title = `Edit ${displayText}`;
            this.activityEditor.show();
            this.activityEditor
                .display(activityName, activity)
                .done((updatedHtml: string) => this.updateActivityElement(activityElement, updatedHtml));
        };

        private addActivity = (activityHtml: string) => {
            const activityElement = $(activityHtml);

            this.container.appendChild(activityElement[0]);
            this.initializeElement(activityElement);
        };

        private updateActivityElement = (activityElement: JQuery<HTMLElement>, updatedHtml: string) => {
            this.plumber.deleteConnectionsForElement(activityElement);
            this.plumber.removeAllEndpoints(activityElement);
            activityElement.remove();

            const updatedActivityElement = $(updatedHtml);
            this.container.appendChild(updatedActivityElement[0]);
            this.initializeElement(updatedActivityElement);
        };

        private updateActivityModel = (activityElement: JQuery<HTMLElement>, update: (activity: IActivity) => any) => {
            const model: IActivity = activityElement.data('activity-model');
            update(model);
            activityElement.data('activity-model', model);
        };

        private initializeNodes = () => {
            this.plumber.batch(() => {
                const activityElements = $(this.container).find('.activity');

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
                containment: true,
                start: (args: any) => {
                    this.dragStart = {left: args.e.screenX, top: args.e.screenY};
                },
                stop: (args: any) => {
                    this.hasDragged = this.dragStart.left !== args.e.screenX || this.dragStart.top !== args.e.screenY;

                    if (!this.hasDragged) {
                        return;
                    }

                    this.updateActivityModel(activityElement, activity => {
                        const designer = activity.metadata.customFields.designer || {x: 0, y: 0};
                        jQuery.extend(designer, {
                            x: args.pos[0],
                            y: args.pos[1]
                        });
                        activity.metadata.customFields.designer = designer;
                    });
                }
            });

            this.plumber.makeTarget(activityElement, {
                dropOptions: {hoverClass: 'hover'},
                anchor: 'Continuous',
                endpoint: ['Blank', {radius: 8}]
            });

            this.addSourceEndpoints(activityElement[0], activityId, activityEndpoints);

            activityElement.on('dblclick', this.onDoubleClick);
            this.plumber.revalidate(activityElement[0]);
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

        private static onDragOver = (e: JQuery.Event) => {
            e.preventDefault();
        };

        private onDrop = (e: JQuery.Event) => {
            e.preventDefault();

            const dragEvent = <DragEvent>e.originalEvent;
            const activityDescriptor: ActivityDescriptor = JSON.parse(dragEvent.dataTransfer.getData('activity-descriptor-json'));
            const activityName = activityDescriptor.name;
            const x = dragEvent.offsetX;
            const y = dragEvent.offsetY;
            const activity: IActivity = {
                id: null,
                endpoints: [],
                metadata: {
                    customFields: {
                        designer: {x: x, y: y}
                    }
                }
            };

            this.activityEditor.title = `Add ${activityDescriptor.displayText}`;
            this.activityEditor.show();
            this.activityEditor.display(activityName, activity).done((data: string) => this.addActivity(data));
        };

        private onDoubleClick = (e: JQuery.Event) => {
            e.preventDefault();

            const activityElement: JQuery<HTMLElement> = $(<HTMLElement>e.originalEvent.currentTarget);
            this.editActivity(activityElement);
        };
    }

    $(() => {
        $('.workflow-designer-container').each((i, e) => {
            const canvasContainer = $(e).find('.workflow-canvas')[0];
            const activityEditorContainer = $(e).find('.activity-editor-modal')[0];
            const workflowDesigner = new WorkflowDesigner(canvasContainer, activityEditorContainer);

            const win = <any>window;
            win.Flowsharp = win.Flowsharp || {};
            win.Flowsharp.workflowDesigner = workflowDesigner;
        });
    });
}