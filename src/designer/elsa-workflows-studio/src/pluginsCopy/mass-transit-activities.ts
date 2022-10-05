import { OutcomeNames } from "../modelsCopy/outcome-names";
import { WorkflowPlugin } from "../modelsCopy";
import { ActivityDefinitionCopy } from "../modelsCopy";
import pluginStore from '../servicesCopy/workflow-plugin-store';

export class MassTransitActivities implements WorkflowPlugin {
  private static readonly Category: string = "MassTransit";

  getName = (): string => "MassTransitActivities";
  getActivityDefinitions = (): Array<ActivityDefinitionCopy> => ([
    this.receiveMassTransitMessage(),
    this.sendMassTransitMessage()
  ]);

  private receiveMassTransitMessage = (): ActivityDefinitionCopy => ({
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

  private sendMassTransitMessage = (): ActivityDefinitionCopy => ({
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

pluginStore.add(new MassTransitActivities());
