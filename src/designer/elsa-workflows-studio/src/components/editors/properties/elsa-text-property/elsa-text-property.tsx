import {Component, h, Prop, State} from '@stencil/core';
import {ActivityDefinitionProperty, ActivityPropertyDescriptor} from "../../../../models";
import {MonacoValueChangedArgs} from "../../monaco/elsa-monaco/elsa-monaco";

@Component({
  tag: 'elsa-text-property',
  styleUrl: 'elsa-text-property.css',
  shadow: false,
})
export class ElsaTextProperty {

  @Prop() propertyDescriptor: ActivityPropertyDescriptor;
  @Prop() propertyModel: ActivityDefinitionProperty;
  @State() currentValue?: string

  async componentWillLoad() {

    let currentValue = this.propertyModel.expression;

    if(currentValue == undefined) {
      const defaultValue = this.propertyDescriptor.defaultValue;
      currentValue = defaultValue ? defaultValue.toString() : undefined;
    }

    this.currentValue = currentValue;
  }

  onMonacoValueChanged(e: MonacoValueChangedArgs) {
    this.currentValue = e.value;
  }

  render() {
    const propertyDescriptor = this.propertyDescriptor;
    const propertyName = propertyDescriptor.name;
    const fieldId = propertyName;
    const fieldName = propertyName;
    const value = this.currentValue;

    return (
        <input type="text" id={fieldId} name={fieldName} value={value} class="focus:ring-blue-500 focus:border-blue-500 block w-full min-w-0 rounded-md sm:text-sm border-gray-300"/>
      );
  }
}
