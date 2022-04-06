import { Component, h, Prop, State } from "@stencil/core";
import cronstrue from "cronstrue";
import { ActivityDefinitionProperty, ActivityModel, ActivityPropertyDescriptor, SyntaxNames } from "../../../../models";

@Component({
    tag: 'elsa-cron-expression-property',
})
export class ElsaCronExpressionProperty {

    @Prop() activityModel: ActivityModel;
    @Prop() propertyDescriptor: ActivityPropertyDescriptor;
    @Prop() propertyModel: ActivityDefinitionProperty;
    @State() currentValue: string;
    @State() valueDescription: string;

    onChange(e: Event) {
        const input = e.currentTarget as HTMLInputElement;
        const defaultSyntax = this.propertyDescriptor.defaultSyntax || SyntaxNames.Literal;
        this.propertyModel.expressions[defaultSyntax] = this.currentValue = input.value;
        this.updateDescription();
    }

    componentWillLoad() {
        const defaultSyntax = this.propertyDescriptor.defaultSyntax || SyntaxNames.Literal;
        this.currentValue = this.propertyModel.expressions[defaultSyntax] || undefined;
        this.updateDescription();
    }

    onDefaultSyntaxValueChanged(e: CustomEvent) {
        this.currentValue = e.detail;
    }

    updateDescription() {
        this.valueDescription = cronstrue.toString(this.currentValue, { throwExceptionOnParseError: false });
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
                    <input type="text" id={fieldId} name={fieldName} value={value} onChange={e => this.onChange(e)}
                        class="disabled:elsa-opacity-50 disabled:elsa-cursor-not-allowed focus:elsa-ring-blue-500 focus:elsa-border-blue-500 elsa-block elsa-w-full elsa-min-w-0 elsa-rounded-md sm:elsa-text-sm elsa-border-gray-300"
                        disabled={isReadOnly} />
                    <p class="elsa-mt-2 elsa-text-sm elsa-text-gray-500">{this.valueDescription}</p>                    
                </div>

            </elsa-property-editor>
        );
    }
}