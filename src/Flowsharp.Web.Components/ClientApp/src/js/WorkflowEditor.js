import React from 'react';
import dotnetify from 'dotnetify';
import WorkflowCanvas from "./WorkflowCanvas";

export default class WorkflowEditor extends React.Component {
    constructor(props) {
        super(props);
        dotnetify.react.connect('WorkflowEditor', this);
        this.state = { Greetings: '', ServerTime: '' };
    }

    render() {
        return (
            <div>
                <p>{this.state.Greetings}</p>
                <p>Server time is: {this.state.ServerTime}</p>
                <WorkflowCanvas/>
            </div>
        );
    }
}