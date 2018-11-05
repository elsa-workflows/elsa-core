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