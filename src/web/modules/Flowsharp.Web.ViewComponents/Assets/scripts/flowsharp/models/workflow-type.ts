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