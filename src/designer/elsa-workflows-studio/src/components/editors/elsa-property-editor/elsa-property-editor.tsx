import {Component, h, Prop, State, Event, EventEmitter} from '@stencil/core';
import {ActivityDefinitionProperty, ActivityPropertyDescriptor, SyntaxNames} from "../../../models";

@Component({
    tag: 'elsa-property-editor',
    styleUrl: 'elsa-property-editor.css',
    shadow: false,
})
export class ElsaPropertyEditor {

    @Event() defaultSyntaxValueChanged: EventEmitter<string>;
    @Prop() propertyDescriptor: ActivityPropertyDescriptor;
    @Prop() propertyModel: ActivityDefinitionProperty;
    @Prop({attribute: 'editor-height', reflect: true}) editorHeight: string = '6em';
    @Prop({attribute: 'single-line', reflect: true}) singleLineMode: boolean = false;
    @Prop({attribute: 'context', reflect: true}) context?: string;
    @Prop() showLabel: boolean = true;

    onSyntaxChanged(e: CustomEvent<string>) {
        this.propertyModel.syntax = e.detail;
    }

    onExpressionChanged(e: CustomEvent<string>) {
        const defaultSyntax = this.propertyDescriptor.defaultSyntax || SyntaxNames.Literal;
        const syntax = this.propertyModel.syntax || defaultSyntax;
        this.propertyModel.expressions[syntax] = e.detail;

        if (syntax != defaultSyntax)
            return;

        this.defaultSyntaxValueChanged.emit(e.detail);
    }

    render() {
        const propertyDescriptor = this.propertyDescriptor;
        const propertyModel = this.propertyModel;
        const defaultSyntax = propertyDescriptor.defaultSyntax || SyntaxNames.Literal;
        const fieldHint = propertyDescriptor.hint;
        const fieldName = propertyDescriptor.name;
        const label = this.showLabel ? fieldHint : null;

        return <div>
            <elsa-multi-expression-editor
                onSyntaxChanged={e => this.onSyntaxChanged(e)}
                onExpressionChanged={e => this.onExpressionChanged(e)}
                fieldName={fieldName}
                label={label}
                syntax={propertyModel.syntax}
                defaultSyntax={propertyDescriptor.defaultSyntax}
                expressions={propertyModel.expressions}
                supportedSyntaxes={propertyDescriptor.supportedSyntaxes}
                editor-height={this.editorHeight}>
                <slot/>
            </elsa-multi-expression-editor>
            {fieldHint ? <p class="mt-2 text-sm text-gray-500">{fieldHint}</p> : undefined}
        </div>
    }
}
