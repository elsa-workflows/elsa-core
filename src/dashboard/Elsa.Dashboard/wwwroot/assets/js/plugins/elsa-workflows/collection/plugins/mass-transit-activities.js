import { OutcomeNames } from "../models/outcome-names";
import pluginStore from '../services/workflow-plugin-store';
export class MassTransitActivities {
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
