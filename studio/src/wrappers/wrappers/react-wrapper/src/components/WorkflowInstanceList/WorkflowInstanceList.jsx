import React from 'react';
import PropTypes from 'prop-types';
import {v4 as uuid} from 'uuid';
import {RemoteArgs} from "../../remote-args.js";

/**
 * Primary UI component for user interaction
 */
export const WorkflowInstanceList = ({ remoteEndpoint, apiKey, ...props }) => {
    const id = uuid();
    return React.createElement('elsa-workflow-instance-list', { ...props, id: id, "remote-endpoint": remoteEndpoint, "api-key": apiKey });
};

WorkflowInstanceList.propTypes = {
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

WorkflowInstanceList.defaultProps = {
    remoteEndpoint: RemoteArgs.remoteEndpoint,
    apiKey: RemoteArgs.apiKey,
    accessToken: null,

};
