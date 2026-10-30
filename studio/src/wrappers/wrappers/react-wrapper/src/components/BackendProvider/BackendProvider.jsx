import React from 'react';
import PropTypes from 'prop-types';
import {v4 as uuid} from 'uuid';


/**
 * Primary UI component for user interaction
 */
export const BackendProvider = ({ remoteEndpoint, ...props }) => {
    const id = uuid();
    return React.createElement('elsa-backend-provider', { 
        ...props, 
        id: id, 
        "remote-endpoint": remoteEndpoint,
        "api-key": props.apiKey,
        "access-token": props.accessToken
    });
};

BackendProvider.propTypes = {
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

BackendProvider.defaultProps = {
    remoteEndpoint: null,
    apiKey: null,
    accessToken: null,
};
