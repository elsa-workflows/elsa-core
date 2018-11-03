///<reference path='../node_modules/@types/jquery/index.d.ts' />
///<reference path='./typings.d.ts' />
///<reference path='./workflow-models.ts' />

class WorkflowEditor {
    private readonly container: any;
    private plumber: any;
    private activityElements: any;
    private activities: any[];
    
    constructor(container: any) {
        this.container = container;
        this.readActivities();
        this.plumber = this.createJsPlumbInstance();
        this.initializeNodes();
    }

    readActivities() {
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

    createJsPlumbInstance() {
        return jsPlumb.getInstance({
            Anchor: "Continuous",
            DragOptions: {cursor: 'pointer', zIndex: 2000},
            //EndpointStyle: [{fillStyle: '#225588'}],
            //Endpoints: [["Dot", {radius: 7}], ["Blank"]],
            ConnectionOverlays: [
                ["Arrow", {width: 12, length: 12, location: -5}],
            ],
            Container: this.container
        });
    };

    initializeNodes() {
        // Suspend drawing and initialize.
        this.plumber.batch(() => {
            let activityElements = this.activityElements;

            for (let index = 0; index < activityElements.length; index++) {
                const activityElement = activityElements[index];
                const activity = this.activities[index];
                const $activityElement = $(activityElement);

                // Make the activity draggable.
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

            // Connect activities.
            this.updateConnections();

            // Make all activity elements visible.
            activityElements.show();
        });
    }
    
    addSourceEndpoints(activity: any, activityElement: any){
        for(let i = 0; i < activity.endpoints.length; i++)
        {
            const endpoint: any = activity.endpoints[i];
            const sourceEndpointOptions: any = WorkflowEditor.getSourceEndpointOptions(activity.id, endpoint.name);
            this.plumber.addEndpoint(activityElement, {
                connectorOverlays: [['Label', {
                    label: endpoint.name,
                    cssClass: 'connection-label'
                }]]
            }, sourceEndpointOptions);
        }
    }

    static getSourceEndpointOptions(activityId: any, endpointName: any) {
        // The definition of source endpoints.
        const paintColor = '#7ab02c';
        return {
            endpoint: 'Dot',
            anchor: 'Continuous',
            paintStyle: {
                stroke: paintColor,
                fill: paintColor,
                radius: 7,
                strokeWidth: 1
            },
            isSource: true,
            connector: ['Flowchart', {stub: [40, 60], gap: 0, cornerRadius: 5, alwaysRespectStubs: true}],
            connectorStyle: {
                strokeWidth: 2,
                stroke: '#999999',
                joinstyle: 'round',
                outlineStroke: 'white',
                outlineWidth: 2
            },
            hoverPaintStyle: {
                fill: '#216477',
                stroke: '#216477'
            },
            connectorHoverStyle: {
                strokeWidth: 3,
                stroke: '#216477',
                outlineWidth: 5,
                outlineStroke: 'white'
            },
            connectorOverlays: [['Label', {location: [3, -1.5], cssClass: 'endpointSourceLabel'}]],
            dragOptions: {},
            uuid: `${activityId}-${endpointName}`
        };
    };

    updateConnections() {
        const workflowId = "";

        // for (let transitionModel of this.workflowType.transitions) {
        //     const sourceEndpointUuid: string = `${transitionModel.sourceActivityId}-${transitionModel.sourceOutcomeName}`;
        //     const sourceEndpoint: Endpoint = plumber.getEndpoint(sourceEndpointUuid);
        //     const destinationElementId: string = `activity-${workflowId}-${transitionModel.destinationActivityId}`;
        //
        //     plumber.connect({
        //         source: sourceEndpoint,
        //         target: destinationElementId
        //     });
        // }
    }

    static areEqualOutcomes(outcomes1: any, outcomes2: any) {
        if (outcomes1.length !== outcomes2.length) {
            return false;
        }

        for (let i = 0; i < outcomes1.length; i++) {
            const outcome1 = outcomes1[i];
            const outcome2 = outcomes2[i];

            if (outcome1.name !== outcome2.displayName || outcome1.displayName !== outcome2.displayName) {
                return false;
            }
        }

        return true;
    }
}