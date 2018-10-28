export class WorkflowType {
    constructor(){
        this.id = null;
        this.activities = [];
        this.transitions = [];
        this.removedActivities = [];
    }
}

export class Activity {
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
    constructor(){
        this.name = null;
        this.displayName = null;   
    }
}

export class Transition {
    constructor(){
        this.sourceActivityId = null;
        this.sourceOutcomeName = null;
        this.destinationActivityId = null;
    }
}