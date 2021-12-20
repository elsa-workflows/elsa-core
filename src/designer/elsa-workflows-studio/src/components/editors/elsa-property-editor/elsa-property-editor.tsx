import {Component, h, Prop, State, Event, EventEmitter} from '@stencil/core';
import {
  ActivityDefinitionProperty,
  ActivityModel,
  ActivityPropertyDescriptor,
  SyntaxNames,
  IntellisenseContext
} from "../../../models";

@Component({
  tag: 'elsa-property-editor',
  styleUrl: 'elsa-property-editor.css',
  shadow: false,
})
export class ElsaPropertyEditor {

  @Event() defaultSyntaxValueChanged: EventEmitter<string>;
  @Prop() activityModel: ActivityModel;
  @Prop() propertyDescriptor: ActivityPropertyDescriptor;
  @Prop() propertyModel: ActivityDefinitionProperty;
  @Prop({attribute: 'editor-height', reflect: true}) editorHeight: string = '10em';
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
    const fieldHint = propertyDescriptor.hint;
    const fieldName = propertyDescriptor.name;
    const label = this.showLabel ? propertyDescriptor.label : null;

    const context: IntellisenseContext = {
      propertyName: propertyDescriptor.name,
      activityTypeName: this.activityModel.type
    }

    return <div>
      <elsa-multi-expression-editor
        onSyntaxChanged={e => this.onSyntaxChanged(e)}
        onExpressionChanged={e => this.onExpressionChanged(e)}
        fieldName={fieldName}
        label={label}
        syntax={propertyModel.syntax}
        defaultSyntax={propertyDescriptor.defaultSyntax}
        isReadOnly={propertyDescriptor.isReadOnly}
        expressions={propertyModel.expressions}
        supportedSyntaxes={propertyDescriptor.supportedSyntaxes}
        editor-height={this.editorHeight}
        context={context}>
        <slot/>
      </elsa-multi-expression-editor>
      {fieldHint ? <p class="elsa-mt-2 elsa-text-sm elsa-text-gray-500">{fieldHint}</p> : undefined}
    </div>
  }
}
