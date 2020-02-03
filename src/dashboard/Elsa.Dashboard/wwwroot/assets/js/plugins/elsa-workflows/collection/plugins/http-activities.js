import { OutcomeNames } from "../models/outcome-names";
import pluginStore from '../services/workflow-plugin-store';
export class HttpActivities {
    constructor() {
        this.getName = () => "HttpActivities";
        this.getActivityDefinitions = () => ([
            this.sendHttpRequest(),
            this.sendHttpResponse(),
            this.handleHttpRequest()
        ]);
        this.sendHttpRequest = () => ({
            type: "HttpRequestAction",
            displayName: "Send HTTP Request",
            description: "Send an HTTP request.",
            category: HttpActivities.Category,
            icon: 'fas fa-cloud',
            properties: [{
                    name: 'url',
                    type: 'text',
                    label: 'URL',
                    hint: 'The URL to send the HTTP request to.'
                },
                {
                    name: 'method',
                    type: 'select',
                    label: 'Method',
                    hint: 'The HTTP method to use when making the request.',
                    options: {
                        items: ['GET', 'POST', 'PUT', 'DELETE', 'OPTIONS', 'HEAD']
                    }
                },
                {
                    name: 'content',
                    type: 'expression',
                    label: 'Content',
                    hint: 'The HTTP content to send along with the request.'
                }, {
                    name: 'statusCodes',
                    type: 'list',
                    label: 'Status Codes',
                    hint: 'A list of possible HTTP status codes to handle, comma-separated. Example: 200, 400, 404'
                }],
            outcomes: 'x => !!x.state.statusCodes ? x.state.statusCodes : []'
        });
        this.handleHttpRequest = () => ({
            type: "HttpRequestEvent",
            displayName: "Handle HTTP Request",
            description: "Handle an incoming HTTP request.",
            category: HttpActivities.Category,
            icon: 'fas fa-cloud',
            properties: [{
                    name: 'path',
                    type: 'text',
                    label: 'Path',
                    hint: 'The relative path that triggers this activity.'
                },
                {
                    name: 'method',
                    type: 'select',
                    label: 'Method',
                    hint: 'The HTTP method that triggers this activity.',
                    options: {
                        items: ['GET', 'POST', 'PUT', 'DELETE', 'OPTIONS', 'HEAD']
                    }
                },
                {
                    name: 'readContent',
                    type: 'boolean',
                    label: 'Read Content',
                    hint: 'Check if the HTTP request content body should be read and stored as part of the HTTP request model. The stored format depends on the content-type header.'
                }],
            runtimeDescription: `x => !!x.state.path ? \`Handle <strong>\${ x.state.method } \${ x.state.path }</strong>.\` : x.definition.description`,
            outcomes: [OutcomeNames.Done]
        });
        this.sendHttpResponse = () => ({
            type: "HttpResponseAction",
            displayName: "Send HTTP Response",
            description: "Send an HTTP response.",
            category: HttpActivities.Category,
            icon: 'fas fa-cloud',
            properties: [{
                    name: 'statusCode',
                    type: 'select',
                    label: 'Status Code',
                    hint: 'The HTTP status code to write.',
                    options: {
                        items: [
                            { label: '2xx', options: [200, 201, 202, 203, 204] },
                            { label: '3xx', options: [301, 302, 304, 307, 308] },
                            { label: '4xx', options: [400, 401, 402, 403, 404, 405, 409, 410, 412, 413, 415, 417, 418, 420, 428, 429] }
                        ]
                    }
                },
                {
                    name: 'content',
                    type: 'expression',
                    label: 'Content',
                    hint: 'The HTTP content to write.',
                    options: { multiline: true }
                },
                {
                    name: 'contentType',
                    type: 'select',
                    label: 'Content Type',
                    hint: 'The HTTP content type header to write.',
                    options: {
                        items: ['text/plain', 'text/html', 'application/json', 'application/xml']
                    }
                },
                {
                    name: 'responseHeaders',
                    type: 'expression',
                    label: 'Response Headers',
                    hint: 'The headers to send along with the response. One \'header: value\' pair per line.',
                    options: { multiline: true }
                }],
            runtimeDescription: `x => !!x.state.statusCode ? \`Send an HTTP <strong>\${ x.state.statusCode }</strong><br/><br/> \${ x.state.contentType }</strong><br/>\${ !!x.state.content ? x.state.content.expression ? x.state.content.expression.substr(0,100) + '...' : '' : '' }\` : x.definition.description`,
            outcomes: [OutcomeNames.Done]
        });
    }
}
HttpActivities.Category = "HTTP";
pluginStore.add(new HttpActivities());
