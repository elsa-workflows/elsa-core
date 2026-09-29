import React from 'react';
import PropTypes from 'prop-types';
import {v4 as uuid} from 'uuid';
import {RemoteArgs} from "../../remote-args.js";


/**
 * Primary UI component for user interaction
 */
export const WorkflowDefinitionEditor = ({ definitionId, remoteEndpoint, apiKey, ...props }) => {
    const id = uuid();
    return React.createElement('elsa-workflow-definition-editor', { ...props, id: id, "definition-id": definitionId, "remote-endpoint": remoteEndpoint, "api-key": apiKey });
};

WorkflowDefinitionEditor.propTypes = {
    /**
     * The workflow definition ID.
     */
    definitionId: PropTypes.string,
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
};

WorkflowDefinitionEditor.defaultProps = {
    definitionId: null,
    remoteEndpoint: RemoteArgs.remoteEndpoint,
    apiKey: RemoteArgs.apiKey,
    accessToken: null,

};
