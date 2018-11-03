export class WorkflowType {
    private id: null;
    private activities: any[];
    private transitions: any[];
    private removedActivities: any[];
    
    constructor(){
        this.id = null;
        this.activities = [];
        this.transitions = [];
        this.removedActivities = [];
    }
}

export class Activity {
    private id: null;
    private x: number;
    private y: number;
    private isStart: boolean;
    private isBlocking: boolean;
    private isEvent: boolean;
    private endpoints: any[];
    
    constructor(){
        this.id = null;
        this.x = 0;
        this.y = 0;
        this.isStart = false;
        this.isBlocking = false;
        this.isEvent = false;
        this.endpoints = [];    
    }
    
}

export class Endpoint {
    private name: null;
    private displayName: null;
    constructor(){
        this.name = null;
        this.displayName = null;   
    }
}

export class Transition {
    private sourceActivityId: null;
    private sourceOutcomeName: null;
    private destinationActivityId: null;
    
    constructor(){
        this.sourceActivityId = null;
        this.sourceOutcomeName = null;
        this.destinationActivityId = null;
    }
}