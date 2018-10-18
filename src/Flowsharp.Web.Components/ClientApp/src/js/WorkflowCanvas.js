import React from 'react';
import dotnetify from 'dotnetify';
import { jsPlumb, jsPlumbInstance, Connection, ConnectionMadeEventInfo } from 'jsplumb';
import * as ReactDOM from "react-dom";
import * as Flowsharp from './flowsharp';
import Activity from "./Activity";

export default class WorkflowCanvas extends React.Component {
    constructor(props) {
        super(props);
        dotnetify.react.connect('WorkflowCanvas', this);
        
        this.state = { workflow: null };
        
        jsPlumb.ready(() => {
            this.container = ReactDOM.findDOMNode(this);
            this.plumber = WorkflowCanvas.createPlumber(this.container);
        });
    }

    render() {
        return (
            <div className='workflow-canvas'>
                Workfow Canvas
                <Activity/>
                <Activity/>
            </div>
        );
    }
    
    static createPlumber(container: HTMLDivElement){
        return jsPlumb.getInstance({
            Anchor: "Continuous",
            DragOptions: {
                cursor: 'pointer', zIndex: 2000
            },
            EndpointStyles: [{ fillStyle: '#225588' }],
            Endpoints: [["Dot", { radius: 7 }], ["Blank"]],
            ConnectionOverlays: [
                ['Arrow', {
                    location: 1,
                    visible: true,
                    width: 11,
                    length: 11
                }],
                ['Label', {
                    location: 0.5,
                    id: 'label',
                    cssClass: 'connection-label'
                }]
            ],
            ConnectorZIndex: 5,
            Container: container
        });
    }
    
    static setupJsPlumb(plumber: jsPlumbInstance){
        
        // Listen for new connections.
        plumber.bind('connection', function (connectionInfo: ConnectionMadeEventInfo, originalEvent: Event) {
            const connection: Connection = connectionInfo.connection;
            const endpoint: Flowsharp.Endpoint = connection.getParameters().endpoint;
            const label: any = connection.getOverlay('label');
            label.setLabel(endpoint.Name);
        });
    }
}