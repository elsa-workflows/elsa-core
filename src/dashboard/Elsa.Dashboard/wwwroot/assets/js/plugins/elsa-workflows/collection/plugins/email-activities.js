import { OutcomeNames } from "../models/outcome-names";
import pluginStore from '../services/workflow-plugin-store';
export class EmailActivities {
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
