import React from 'react';
import PropTypes from 'prop-types';
import {v4 as uuid} from 'uuid';
import {RemoteArgs} from "../../remote-args.js";

/**
 * Primary UI component for user interaction
 */
export const WorkflowInstanceViewer = ({ instanceId, remoteEndpoint, apiKey, ...props }) => {
    const id = uuid();
    return React.createElement('elsa-workflow-instance-viewer', { ...props, id: id, "instance-id": instanceId, "remote-endpoint": remoteEndpoint, "api-key": apiKey });
};

WorkflowInstanceViewer.propTypes = {
    /**
     * The workflow definition ID.
     */
    instanceId: PropTypes.string,
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

WorkflowInstanceViewer.defaultProps = {
    instanceId: null,
    remoteEndpoint: RemoteArgs.remoteEndpoint,
    apiKey: RemoteArgs.apiKey,
    accessToken: null,

};
