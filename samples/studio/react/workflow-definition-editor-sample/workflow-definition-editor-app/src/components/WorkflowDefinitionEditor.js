import React from "react";
import {v4 as uuid} from "uuid";

export default class WorkflowDefinitionEditor extends React.Component {

    constructor(props) {
        super(props);

        debugger;
        
        if (!this.props.id)
            this.id = uuid();
    }

    id = "";

    onValueChanged = function (value) {
    };

    render() {
        return React.createElement('elsa-studio-workflow-definition-editor', { ...this.props, id: this.id, "definition-id": this.props.definitionId });
    }

    componentDidMount() {
        let element = document.getElementById(this.id);
        element.valueChanged = (x) => {
            this.props.onValueChanged(x);
        };
    }
}