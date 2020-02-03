import { r as registerInstance, c as createEvent, h, d as getElement } from './chunk-25ccd4a5.js';
import { c as createCommonjsModule, a as commonjsGlobal, d as deepClone } from './chunk-b68c6ae2.js';
import { D as DisplayManager } from './chunk-80cbdbf5.js';

var dragscroll = createCommonjsModule(function (module, exports) {
!function(e,n){"function"==typeof undefined&&undefined.amd?undefined(["exports"],n):n("undefined"!='object'?exports:e.dragscroll={});}(commonjsGlobal,function(e){var n,t,o=window,l=document,c="mousemove",r="mouseup",i="mousedown",m="EventListener",d="add"+m,s="remove"+m,f=[],u=function(e,m){for(e=0;e<f.length;)m=f[e++],m=m.container||m,m[s](i,m.md,0),o[s](r,m.mu,0),o[s](c,m.mm,0);for(f=[].slice.call(l.getElementsByClassName("dragscroll")),e=0;e<f.length;)!function(e,m,s,f,u,a){(a=e.container||e)[d](i,a.md=function(n){e.hasAttribute("nochilddrag")&&l.elementFromPoint(n.pageX,n.pageY)!=a||(f=1,m=n.clientX,s=n.clientY,n.preventDefault());},0),o[d](r,a.mu=function(){f=0;},0),o[d](c,a.mm=function(o){f&&((u=e.scroller||e).scrollLeft-=n=-m+(m=o.clientX),u.scrollTop-=t=-s+(s=o.clientY),e==l.body&&((u=l.documentElement).scrollLeft-=n,u.scrollTop-=t));},0);}(f[e++]);};"complete"==l.readyState?u():o[d]("load",u,0),e.reset=u;});
});

class BooleanFieldDriver {
    constructor() {
        this.displayEditor = (activity, property) => {
            const name = property.name;
            const label = property.label;
            const checked = activity.state[name] === 'true';
            return `<wf-boolean-field name="${name}" label="${label}" hint="${property.hint}" checked="${checked}"></wf-boolean-field>`;
        };
        this.updateEditor = (activity, property, formData) => {
            activity.state[property.name] = formData.get(property.name);
        };
    }
}

class ExpressionFieldDriver {
    constructor() {
        this.displayEditor = (activity, property) => {
            const name = property.name;
            const label = property.label;
            const value = activity.state[name] || { expression: '', syntax: 'Literal' };
            const multiline = (property.options || {}).multiline || false;
            const expressionValue = value.expression.replace(/"/g, '&quot;');
            return `<wf-expression-field name="${name}" label="${label}" hint="${property.hint}" value="${expressionValue}" syntax="${value.syntax}" multiline="${multiline}"></wf-expression-field>`;
        };
        this.updateEditor = (activity, property, formData) => {
            const expressionFieldName = `${property.name}.expression`;
            const syntaxFieldName = `${property.name}.syntax`;
            const expression = formData.get(expressionFieldName).toString().trim();
            const syntax = formData.get(syntaxFieldName).toString();
            activity.state[property.name] = {
                expression: expression,
                syntax: syntax
            };
        };
    }
}

class ListFieldDriver {
    constructor() {
        this.displayEditor = (activity, property) => {
            const name = property.name;
            const label = property.label;
            const items = activity.state[name] || [];
            const value = items.join(', ');
            return `<wf-list-field name="${name}" label="${label}" hint="${property.hint}" items="${value}"></wf-list-field>`;
        };
        this.updateEditor = (activity, property, formData) => {
            const value = formData.get(property.name).toString();
            activity.state[property.name] = value.split(',').map(x => x.trim());
        };
    }
}

class TextFieldDriver {
    constructor() {
        this.displayEditor = (activity, property) => {
            const name = property.name;
            const label = property.label;
            const value = activity.state[name] || '';
            return `<wf-text-field name="${name}" label="${label}" hint="${property.hint}" value="${value}"></wf-text-field>`;
        };
        this.updateEditor = (activity, property, formData) => {
            activity.state[property.name] = formData.get(property.name).toString().trim();
        };
    }
}

class SelectFieldDriver {
    constructor() {
        this.displayEditor = (activity, property) => {
            const name = property.name;
            const label = property.label;
            const value = activity.state[name] || '';
            const items = property.options.items || [];
            const itemsJson = encodeURI(JSON.stringify(items));
            return `<wf-select-field name="${name}" label="${label}" hint="${property.hint}" data-items="${itemsJson}" value="${value}"></wf-select-field>`;
        };
        this.updateEditor = (activity, property, formData) => {
            const value = formData.get(property.name).toString();
            activity.state[property.name] = value.trim();
        };
    }
}

class WorkflowPluginStore {
    constructor() {
        this.plugins = [];
        this.add = (plugin) => {
            this.plugins = [...this.plugins, plugin];
        };
        this.list = () => [...this.plugins];
    }
}
const pluginStore = new WorkflowPluginStore();
const win = window;
const elsa = win.elsa || {};
elsa.pluginStore = pluginStore;
win.elsa = elsa;

var OutcomeNames;
(function (OutcomeNames) {
    OutcomeNames["Done"] = "Done";
    OutcomeNames["True"] = "True";
    OutcomeNames["False"] = "False";
})(OutcomeNames || (OutcomeNames = {}));

class ConsoleActivities {
    constructor() {
        this.getName = () => "ConsoleActivities";
        this.getActivityDefinitions = () => ([this.readLine(), this.writeLine()]);
        this.readLine = () => ({
            type: 'ReadLine',
            displayName: 'Read Line',
            description: 'Read text from standard in.',
            runtimeDescription: 'a => !!a.state.variableName ? `Read text from standard in and store into <strong>${ a.state.variableName }</strong>.` : \'Read text from standard in.\'',
            outcomes: [OutcomeNames.Done],
            category: ConsoleActivities.Category,
            icon: 'fas fa-terminal',
            properties: [{
                    name: 'variableName',
                    type: 'text',
                    label: 'Variable Name',
                    hint: 'The name of the variable to store the value into.'
                }]
        });
        this.writeLine = () => ({
            type: 'WriteLine',
            displayName: 'Write Line',
            description: 'Write text to standard out.',
            category: ConsoleActivities.Category,
            icon: 'fas fa-terminal',
            runtimeDescription: `x => !!x.state.textExpression ? \`Write <strong>\${ x.state.textExpression.expression }</strong> to standard out.\` : x.definition.description`,
            outcomes: [OutcomeNames.Done],
            properties: [{
                    name: 'textExpression',
                    type: 'expression',
                    label: 'Text',
                    hint: 'The text to write.'
                }]
        });
    }
}
ConsoleActivities.Category = "Console";
pluginStore.add(new ConsoleActivities());

class ControlFlowActivities {
    constructor() {
        this.getName = () => "ControlFlowActivities";
        this.getActivityDefinitions = () => ([
            this.fork(),
            this.ifElse(),
            this.join(),
            this.switch()
        ]);
        this.fork = () => ({
            type: "Fork",
            displayName: "Fork",
            description: "Fork workflow execution into multiple branches.",
            category: ControlFlowActivities.Category,
            icon: 'fas fa-code-branch fa-rotate-180',
            outcomes: 'x => x.state.branches',
            properties: [{
                    name: 'branches',
                    type: 'list',
                    label: 'Branches',
                    hint: 'Enter one or more names representing branches, separated with a comma. Example: Branch 1, Branch 2'
                }]
        });
        this.ifElse = () => ({
            type: "IfElse",
            displayName: "If/Else",
            description: "Evaluate a Boolean expression and continue execution depending on the result.",
            category: ControlFlowActivities.Category,
            runtimeDescription: 'x => !!x.state.expression ? `Evaluate <strong>${ x.state.expression.expression }</strong> and continue execution depending on the result.` : x.definition.description',
            outcomes: [OutcomeNames.True, OutcomeNames.False],
            properties: [{
                    name: 'expression',
                    type: 'expression',
                    label: 'Expression',
                    hint: 'The expression to evaluate. The evaluated value will be used to switch on.'
                }]
        });
        this.join = () => ({
            type: "Join",
            displayName: "Join",
            description: "Merge workflow execution back into a single branch.",
            category: ControlFlowActivities.Category,
            icon: 'fas fa-code-branch',
            runtimeDescription: 'x => !!x.state.joinMode ? `Merge workflow execution back into a single branch using mode <strong>${ x.state.joinMode }</strong>` : x.definition.description',
            outcomes: [OutcomeNames.Done],
            properties: [{
                    name: 'joinMode',
                    type: 'text',
                    label: 'Join Mode',
                    hint: 'Either \'WaitAll\' or \'WaitAny\''
                }]
        });
        this.switch = () => ({
            type: "Switch",
            displayName: "Switch",
            description: "Switch execution based on a given expression.",
            category: ControlFlowActivities.Category,
            icon: 'far fa-list-alt',
            runtimeDescription: 'x => !!x.state.expression ? `Switch execution based on <strong>${ x.state.expression.expression }</strong>.` : x.definition.description',
            outcomes: 'x => x.state.cases.map(c => c.toString())',
            properties: [{
                    name: 'expression',
                    type: 'expression',
                    label: 'Expression',
                    hint: 'The expression to evaluate. The evaluated value will be used to switch on.'
                },
                {
                    name: 'cases',
                    type: 'list',
                    label: 'Cases',
                    hint: 'A comma-separated list of possible outcomes of the expression.'
                }]
        });
    }
}
ControlFlowActivities.Category = "Control Flow";
pluginStore.add(new ControlFlowActivities());

class EmailActivities {
    constructor() {
        this.getName = () => "EmailActivities";
        this.getActivityDefinitions = () => ([this.sendEmail()]);
        this.sendEmail = () => ({
            type: "SendEmail",
            displayName: "Send Email",
            description: "Send an email message.",
            category: EmailActivities.Category,
            icon: 'far fa-envelope',
            outcomes: [OutcomeNames.Done],
            properties: [
                {
                    name: 'from',
                    type: 'expression',
                    label: 'From',
                    hint: 'The sender\'s email address'
                },
                {
                    name: 'to',
                    type: 'expression',
                    label: 'To',
                    hint: 'The recipient\'s email address'
                },
                {
                    name: 'subject',
                    type: 'expression',
                    label: 'Subject',
                    hint: 'The subject of the email message.'
                },
                {
                    name: 'body',
                    type: 'expression',
                    label: 'Body',
                    hint: 'The body of the email message.',
                    options: {
                        multiline: true
                    }
                }
            ]
        });
    }
}
EmailActivities.Category = "Email";
pluginStore.add(new EmailActivities());

class HttpActivities {
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

class MassTransitActivities {
    constructor() {
        this.getName = () => "MassTransitActivities";
        this.getActivityDefinitions = () => ([
            this.receiveMassTransitMessage(),
            this.sendMassTransitMessage()
        ]);
        this.receiveMassTransitMessage = () => ({
            type: "ReceiveMassTransitMessage",
            displayName: "Receive MassTransit Message",
            description: "Receive a message via MassTransit.",
            category: MassTransitActivities.Category,
            icon: 'fas fa-envelope-open-text',
            properties: [{
                    name: 'messageType',
                    type: 'text',
                    label: 'Message Type',
                    hint: 'The assembly-qualified type name of the message to receive.'
                }],
            outcomes: [OutcomeNames.Done]
        });
        this.sendMassTransitMessage = () => ({
            type: "SendMassTransitMessage",
            displayName: "Send MassTransit Message",
            description: "Send a message via MassTransit.",
            category: MassTransitActivities.Category,
            icon: 'fas fa-envelope',
            properties: [{
                    name: 'messageType',
                    type: 'text',
                    label: 'Message Type',
                    hint: 'The assembly-qualified type name of the message to send.'
                },
                {
                    name: 'message',
                    type: 'expression',
                    label: 'Message',
                    hint: 'An expression that evaluates to the message to send.'
                }],
            outcomes: [OutcomeNames.Done]
        });
    }
}
MassTransitActivities.Category = "MassTransit";
pluginStore.add(new MassTransitActivities());

class PrimitiveActivities {
    constructor() {
        this.getName = () => "PrimitiveActivities";
        this.getActivityDefinitions = () => ([this.log(), this.setVariable()]);
        this.log = () => ({
            type: "Log",
            displayName: "Log",
            description: "Log a message.",
            category: PrimitiveActivities.Category,
            icon: 'fas fa-feather-alt',
            properties: [{
                    name: 'message',
                    type: 'text',
                    label: 'Message',
                    hint: 'The text to log.'
                },
                {
                    name: 'logLevel',
                    type: 'text',
                    label: 'Log Level',
                    hint: 'The log level to use.'
                }],
            runtimeDescription: 'x => !!x.state.message ? `Log <strong>${x.state.logLevel}: ${x.state.message}</strong>` : x.definition.description',
            outcomes: [OutcomeNames.Done]
        });
        this.setVariable = () => ({
            type: "SetVariable",
            displayName: "Set Variable",
            description: "Set a variable on the workflow.",
            category: PrimitiveActivities.Category,
            properties: [{
                    name: 'variableName',
                    type: 'text',
                    label: 'Variable Name',
                    hint: 'The name of the variable to store the value into.'
                }, {
                    name: 'expression',
                    type: 'expression',
                    label: 'Variable Expression',
                    hint: 'An expression that evaluates to the value to store in the variable.'
                }],
            runtimeDescription: 'x => !!x.state.variableName ? `${x.state.expression.syntax}: <strong>${x.state.variableName}</strong> = <strong>${x.state.expression.expression}</strong>` : x.definition.description',
            outcomes: [OutcomeNames.Done]
        });
    }
}
PrimitiveActivities.Category = "Primitives";
pluginStore.add(new PrimitiveActivities());

class TimerActivities {
    constructor() {
        this.getName = () => "TimerActivities";
        this.getActivityDefinitions = () => ([this.timerEvent()]);
        this.timerEvent = () => ({
            type: "TimerEvent",
            displayName: "Timer Event",
            description: "Triggers after a specified amount of time.",
            category: TimerActivities.Category,
            icon: 'fas fa-hourglass-start',
            properties: [
                {
                    name: 'expression',
                    type: 'expression',
                    label: 'Timeout Expression',
                    hint: 'The amount of time to wait before this timer event is triggered. Format: \'d.HH:mm:ss\'.'
                },
                {
                    name: 'name',
                    type: 'text',
                    label: 'Name',
                    hint: 'Optionally provide a name for this activity. You can reference named activities from expressions.'
                },
                {
                    name: 'title',
                    type: 'text',
                    label: 'Title',
                    hint: 'Optionally provide a custom title for this activity.'
                },
                {
                    name: 'description',
                    type: 'text',
                    label: 'Description',
                    hint: 'Optionally provide a custom description for this activity.'
                }
            ],
            runtimeDescription: 'x => !!x.state.expression ? `Triggers after <strong>${ x.state.expression.expression }</strong>` : x.definition.description',
            outcomes: [OutcomeNames.Done]
        });
    }
}
TimerActivities.Category = "Timers";
pluginStore.add(new TimerActivities());

class DesignerHost {
    constructor(hostRef) {
        registerInstance(this, hostRef);
        this.activityDefinitions = [];
        this.loadActivityDefinitions = () => {
            const pluginsData = this.pluginsData || '';
            const pluginNames = pluginsData.split(/[ ,]+/).map(x => x.trim());
            return pluginStore
                .list()
                .filter(x => pluginNames.indexOf(x.getName()) > -1)
                .filter(x => !!x.getActivityDefinitions)
                .map(x => x.getActivityDefinitions())
                .reduce((a, b) => a.concat(b), []);
        };
        this.onWorkflowChanged = (e) => {
            this.workflowChanged.emit(e.detail);
        };
        this.initActivityDefinitions = () => {
            this.activityDefinitions = this.loadActivityDefinitions();
            if (!!this.activityDefinitionsData) {
                const definitions = JSON.parse(this.activityDefinitionsData);
                this.activityDefinitions = [...this.activityDefinitions, ...definitions];
            }
        };
        this.initFieldDrivers = () => {
            DisplayManager.addDriver('text', new TextFieldDriver());
            DisplayManager.addDriver('expression', new ExpressionFieldDriver());
            DisplayManager.addDriver('list', new ListFieldDriver());
            DisplayManager.addDriver('boolean', new BooleanFieldDriver());
            DisplayManager.addDriver('select', new SelectFieldDriver());
        };
        this.initWorkflow = () => {
            if (!!this.workflowData) {
                const workflow = JSON.parse(this.workflowData);
                if (!workflow.activities)
                    workflow.activities = [];
                if (!workflow.connections)
                    workflow.connections = [];
                this.designer.workflow = workflow;
            }
        };
        this.workflowChanged = createEvent(this, "workflowChanged", 7);
    }
    async newWorkflow() {
        await this.designer.newWorkflow();
    }
    async autoLayout() {
        await this.designer.autoLayout();
    }
    async getWorkflow() {
        return await this.designer.getWorkflow();
    }
    async showActivityPicker() {
        await this.activityPicker.show();
    }
    async export(formatDescriptor) {
        await this.importExport.export(this.designer, formatDescriptor);
    }
    async import() {
        await this.importExport.import();
    }
    async onActivityPicked(e) {
        await this.designer.addActivity(e.detail);
    }
    async onEditActivity(e) {
        this.activityEditor.activity = e.detail;
        this.activityEditor.show = true;
    }
    async onAddActivity() {
        await this.showActivityPicker();
    }
    async onUpdateActivity(e) {
        await this.designer.updateActivity(e.detail);
    }
    async onExportWorkflow(e) {
        if (!this.importExport)
            return;
        await this.importExport.export(this.designer, e.detail);
    }
    async onImportWorkflow(e) {
        this.designer.workflow = deepClone(e.detail);
    }
    componentWillLoad() {
        this.initActivityDefinitions();
        this.initFieldDrivers();
    }
    componentDidLoad() {
        this.initWorkflow();
    }
    render() {
        const activityDefinitions = this.activityDefinitions;
        return (h("host", null, h("wf-activity-picker", { activityDefinitions: activityDefinitions, ref: el => this.activityPicker = el }), h("wf-activity-editor", { activityDefinitions: activityDefinitions, ref: el => this.activityEditor = el }), h("wf-import-export", { ref: el => this.importExport = el }), h("div", { class: "workflow-designer-wrapper dragscroll" }, h("wf-designer", { activityDefinitions: activityDefinitions, ref: el => this.designer = el, canvasHeight: this.canvasHeight, workflow: this.workflow, readonly: this.readonly, onWorkflowChanged: this.onWorkflowChanged }))));
    }
    get el() { return getElement(this); }
    static get style() { return ".workflow-designer-wrapper {\n  height: 80vh;\n  overflow-y: auto;\n}"; }
}

export { DesignerHost as wf_designer_host };
