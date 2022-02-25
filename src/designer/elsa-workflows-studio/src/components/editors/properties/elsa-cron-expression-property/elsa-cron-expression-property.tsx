import { Component, h, Prop, State } from "@stencil/core";
import { ActivityDefinitionProperty, ActivityModel, ActivityPropertyDescriptor, SyntaxNames } from "../../../../models";

import { CronExpressionInput } from 'cron-expression-input';

@Component({
    tag: 'elsa-cron-expression-property'   
})

export class ElsaCronExpressionProperty {

    @Prop() activityModel: ActivityModel;
    @Prop() propertyDescriptor: ActivityPropertyDescriptor;
    @Prop() propertyModel: ActivityDefinitionProperty;
    @State() currentValue: string;

    onChange(e: Event) {
        const input = e.currentTarget as HTMLInputElement;
        const defaultSyntax = this.propertyDescriptor.defaultSyntax || SyntaxNames.Literal;
        this.propertyModel.expressions[defaultSyntax] = this.currentValue = input.value;
    }
    componentWillLoad() {
        const defaultSyntax = this.propertyDescriptor.defaultSyntax || SyntaxNames.Literal;
        this.currentValue = this.propertyModel.expressions[defaultSyntax] || undefined;
    }

    onDefaultSyntaxValueChanged(e: CustomEvent) {
        this.currentValue = e.detail;
    }

    render() {
        const propertyDescriptor = this.propertyDescriptor;
        const propertyModel = this.propertyModel;
        const propertyName = propertyDescriptor.name;
        const isReadOnly = propertyDescriptor.isReadOnly;
        const fieldId = propertyName;
        const fieldName = propertyName;
        let value = this.currentValue;

        if (value == undefined) {
            const defaultValue = this.propertyDescriptor.defaultValue;
            value = defaultValue ? defaultValue.toString() : undefined;
        }

        if (isReadOnly) {
            const defaultSyntax = this.propertyDescriptor.defaultSyntax || SyntaxNames.Literal;
            this.propertyModel.expressions[defaultSyntax] = value;
        }

        return (
            <elsa-property-editor
                activityModel={this.activityModel}
                propertyDescriptor={propertyDescriptor}
                propertyModel={propertyModel}
                onDefaultSyntaxValueChanged={e => this.onDefaultSyntaxValueChanged(e)}
                editor-height="5em"
                single-line={true}>
                <div>
                    <cron-expression-input height="34px" width="250px" value={value} color="d58512"></cron-expression-input>
                </div>
            </elsa-property-editor>
        );
    }

}