import React, {useEffect} from 'react';
import PropTypes from 'prop-types';
import {v4 as uuid} from 'uuid';
import {RemoteArgs} from "../../remote-args.js";

/**
 * Primary UI component for user interaction
 */
export const WorkflowDefinitionList = (props) => {
    const id = uuid();
    const { remoteEndpoint, apiKey } = props;

    useEffect(() => {
        let element = document.getElementById(id);
        element.editWorkflowDefinition = (definitionId) => {
            props.onEditWorkflowDefinition(definitionId);
        };
    }, [id, props]); // Dependency array includes what the effect depends on to run.

    return React.createElement('elsa-workflow-definition-list', { ...props, id: id, "remote-endpoint": remoteEndpoint, "api-key": apiKey });
}

WorkflowDefinitionList.propTypes = {
    /**
     * The remote backend URL.
     */
    remoteEndpoint: PropTypes.string,
    /**
     * The API Key, if any.
     */
    apiKey: PropTypes.string,
    /**
     * The access token, if any.
     */
    accessToken: PropTypes.string,
    /**
     * The callback invoked when a workflow definition is edited.
     */
    onEditWorkflowDefinition: PropTypes.func,
};

WorkflowDefinitionList.defaultProps = {
    remoteEndpoint: RemoteArgs.remoteEndpoint,
    apiKey: RemoteArgs.apiKey,
    accessToken: null
};
