///<reference path='../../node_modules/@types/jquery/index.d.ts' />
///<reference path='../@types/jsplumb.d.ts' />
///<reference path='../@types/popper.d.ts' />
///<reference path='./workflow-designer-config.ts' />
///<reference path='./activity-editor.ts' />
///<reference path="activity-descriptor.ts"/>
///<reference path="workflow.ts"/>
///<reference path="activity-info.ts"/>

namespace Flowsharp {
    export class WorkflowDesigner {
        private readonly container: JQuery<HTMLElement>;
        private readonly canvasContainer: JQuery<HTMLElement>;
        private readonly plumber: any;
        private dragStart: { left: number; top: number };
        private hasDragged: boolean;

        constructor(containerElement: HTMLElement) {
            this.container = $(containerElement);
            this.canvasContainer = this.container.find('.workflow-canvas');
            this.plumber = WorkflowDesignerConfig.createJsPlumbInstance(this.canvasContainer[0]);
            this.initializeNodes();
            this.container.on('contextmenu', '.workflow-canvas', this.onCanvasContextMenu);
            this.canvasContainer.on('contextmenu', '.activity', this.onActivityContextMenu);
            this.canvasContainer.on('dblclick', '.activity', this.onActivityDoubleClick);
            this.canvasContainer.on('click', '.activity .context-menu .dropdown-item', this.onActivityContextMenuItemClick);
            this.canvasContainer.on('click', '.canvas-context-menu .dropdown-item', this.onCanvasContextMenuItemClick);
            $(document).on('click', 'body', this.hideContextMenu);
        }

        public getWorkflow = (): IWorkflow => {
            const workflowMetadata: IWorkflowMetadata = this.canvasContainer.data('workflow-metadata');
            const workflowStatus: WorkflowStatus = this.canvasContainer.data('workflow-status');
            const activityElements = this.canvasContainer.find('.activity');
            const activities: IActivity[] = [];
            const connections: IConnection[] = [];

            for (let index = 0; index < activityElements.length; index++) {
                const activityElement = $(activityElements[index]);
                const activity: IActivity = activityElement.data('activity-model');

                activities.push(activity);
            }

            for (let connection of this.plumber.getConnections()) {
                const sourceEndpoint: Endpoint = connection.endpoints[0];
                const sourceEndpointName = sourceEndpoint.getParameters().endpointName;
                const sourceActivityId: string = $(connection.source).data('activity-id');
                const targetActivityId: string = $(connection.target).data('activity-id');

                connections.push({
                    source: {
                        name: sourceEndpointName,
                        activityId: sourceActivityId
                    },
                    target: {
                        activityId: targetActivityId
                    }
                });
            }

            return {
                metadata: workflowMetadata,
                activities: activities,
                connections: connections,
                status: workflowStatus
            };
        };

        public addActivity = (activityHtml: string): HTMLElement => {
            const $activityElement = $(activityHtml);

            this.plumber.batch(() => {

                const x = 300;
                const y = 300;
                
                $activityElement.css({left: `${x}px`, top: `${y}px`});
                
                this.canvasContainer[0].appendChild($activityElement[0]);
                this.initializeElement($activityElement);
                this.updateActivityModel($activityElement, (activity) => {
                    this.setActivityPosition(activity, x, y);
                });
                $activityElement.show();
            });

            return $activityElement[0];
        };

        public updateActivityElement = (activityElement: HTMLElement, updatedHtml: string) => {
            this.plumber.batch(() => {
                this.plumber.remove(activityElement);

                const $updatedActivityElement = $(updatedHtml);
                const updatedActivityElement = $updatedActivityElement[0];
                this.canvasContainer[0].appendChild(updatedActivityElement);
                this.initializeElement($updatedActivityElement);
                this.restoreConnections(updatedActivityElement);
                $updatedActivityElement.show();
            });
        };

        public addEventListener = (type: string, listener: EventListenerOrEventListenerObject): void => {
            this.container[0].addEventListener(type, listener);
        };

        private editActivity = (activityElement: JQuery<HTMLElement>) => {
            const activityInfo: ActivityInfo = {
                activityElement: activityElement[0],
                activityName: activityElement.data('activity-name'),
                activityDisplayText: activityElement.attr('title'),
                activity: activityElement.data('activity-model')
            };

            this.container[0].dispatchEvent(new CustomEvent('edit-activity', {detail: {activityInfo: activityInfo}}));
        };

        private updateActivityModel = (activityElement: JQuery<HTMLElement>, update: (activity: IActivity) => any) => {
            const model: IActivity = activityElement.data('activity-model');
            update(model);
            activityElement.data('activity-model', model);
        };

        private initializeNodes = () => {

            this.plumber.batch(() => {
                const activityElements = $(this.canvasContainer).find('.activity');
                for (let index = 0; index < activityElements.length; index++) {
                    const activityElement = $(activityElements[index]);
                    this.initializeElement(activityElement);
                }

                this.createConnections();
                activityElements.show();
            });
        };

        private showContextMenu = (menu: JQuery<HTMLElement>, e: JQuery.Event<HTMLElement>) => {
            if (e.isDefaultPrevented())
                return;

            e.preventDefault();
            this.hideContextMenu();

            const element: HTMLElement = e.currentTarget;
            const offset = $(element).offset();
            const relX = e.pageX - offset.left;
            const relY = e.pageY - offset.top;

            menu
                .css({
                    display: 'block',
                    left: relX,
                    top: relY
                })
                .addClass('show')
                .off('click')
                .on('click', 'a', (e) => {
                    this.hideContextMenu();
                });
        };

        private hideContextMenu = () => {
            $('.context-menu').hide().removeClass('show');
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
                        this.setActivityPosition(activity, args.pos[0], args.pos[1]);
                    });
                }
            });

            this.plumber.makeTarget(activityElement, {
                dropOptions: {hoverClass: 'hover'},
                anchor: 'Continuous',
                endpoint: ['Blank', {radius: 8}]
            });

            this.addSourceEndpoints(activityElement[0], activityId, activityEndpoints);
            this.plumber.revalidate(activityElement[0]);
        };

        private setActivityPosition = (activity: IActivity, x: number, y: number) => {
            const designer = activity.metadata.customFields.designer || {x: 0, y: 0};
            jQuery.extend(designer, {
                x: x,
                y: y
            });
            activity.metadata.customFields.designer = designer;
        };

        private restoreConnections = (activityElement: HTMLElement) => {
            const activity: IActivity = $(activityElement).data('activity-model');
            const connections: IConnection[] = $(this.canvasContainer).data('workflow-connections');
            const plumber = this.plumber;
            const activityConnections = connections.filter(x => x.target.activityId == activity.id || x.source.activityId == activity.id);

            for (let connection of activityConnections) {
                const sourceEndpointUuid: string = `${connection.source.activityId}-${connection.source.name}`;
                const sourceEndpoint: Endpoint = plumber.getEndpoint(sourceEndpointUuid);
                const destinationElementId: string = `activity-${connection.target.activityId}`;

                plumber.connect({
                    source: sourceEndpoint,
                    target: destinationElementId
                });
            }
        };

        private createConnections = () => {
            const connections: IConnection[] = $(this.canvasContainer).data('workflow-connections');
            const plumber = this.plumber;

            for (let connection of connections) {
                const sourceEndpointUuid: string = `${connection.source.activityId}-${connection.source.name}`;
                const sourceEndpoint: Endpoint = plumber.getEndpoint(sourceEndpointUuid);
                const destinationElementId: string = `activity-${connection.target.activityId}`;

                plumber.connect({
                    source: sourceEndpoint,
                    target: destinationElementId
                });
            }
        };

        private addSourceEndpoints = (activityElement: HTMLElement, activityId: string, endpoints: any[]) => {
            for (let endpoint of endpoints) {
                const sourceEndpointOptions: any = WorkflowDesignerConfig.getSourceEndpointOptions(activityId, endpoint.name);
                this.plumber.addEndpoint(activityElement, {
                    connectorOverlays: [['Label', {
                        label: endpoint.name,
                        cssClass: 'connection-label'
                    }]]
                }, sourceEndpointOptions);
            }
        };

        private onCanvasContextMenu = (e: JQuery.Event<HTMLElement>) => {
            const menu = $(e.currentTarget).find('.canvas-context-menu');
            this.showContextMenu(menu, e);
        };

        private onActivityContextMenu = (e: JQuery.Event<HTMLElement>) => {
            const menu = $(e.currentTarget).find('.activity-context-menu');
            this.showContextMenu(menu, e);
        };

        private onActivityContextMenuItemClick = (e: JQuery.Event<HTMLElement>) => {
            e.preventDefault();

            const $activityElement: JQuery<HTMLElement> = $(e.currentTarget).parents('.activity');
            const action = $(e.currentTarget).attr('href').substr(1);

            switch (action) {
                case 'edit':
                    this.editActivity($activityElement);
                    break;
                case 'delete':
                    this.plumber.remove($activityElement);
                    break;
            }
        };

        private onCanvasContextMenuItemClick = (e: JQuery.Event<HTMLElement>) => {
            e.preventDefault();

            const action = $(e.currentTarget).attr('href').substr(1);

            switch (action) {
                case 'add-trigger':

                    break;
                case 'add-action':

                    break;
            }
        };

        private onActivityDoubleClick = (e: JQuery.Event<HTMLElement>) => {
            e.preventDefault();

            const activityElement: JQuery<HTMLElement> = $(e.currentTarget);
            this.editActivity(activityElement);
        };
    }
}