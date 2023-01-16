import {Component, h, Prop, Event, EventEmitter, Method} from "@stencil/core";
import {groupBy} from 'lodash';
import {StorageDriverDescriptor, Variable} from "../../../models";
import {FormEntry} from "../../shared/forms/form-entry";
import {isNullOrWhitespace} from "../../../utils";
import descriptorsStore from '../../../data/descriptors-store';
import {VariableDescriptor} from "../../../services/api-client/variable-descriptors-api";

@Component({
  tag: 'elsa-variable-editor-dialog-content',
  shadow: false
})
export class VariableEditorDialogContent {
  private formElement: HTMLFormElement;

  @Prop() variable: Variable;
  @Event() variableChanged: EventEmitter<Variable>;

  @Method()
  async getVariable(): Promise<Variable> {
    return this.getVariableInternal(this.formElement);
  }

  render() {
    const variable: Variable = this.variable ?? {name: '', typeName: 'Object'};
    const variableTypeName = variable.typeName;
    const availableTypes: Array<VariableDescriptor> = descriptorsStore.variableDescriptors;
    const groupedVariableTypes = groupBy(availableTypes, x => x.category);
    const storageDrivers: Array<StorageDriverDescriptor> = descriptorsStore.storageDrivers;

    return (
      <div>
        <form ref={el => this.formElement = el} class="h-full flex flex-col bg-white" onSubmit={e => this.onSubmit(e)} method="post">
          <div class="pt-4">
            <h2 class="text-lg font-medium ml-4 mb-2">Edit Variable</h2>
            <div class="align-middle inline-block min-w-full border-b border-gray-200">

              <FormEntry fieldId="variableName" label="Name" hint="The technical name of the variable.">
                <input type="text" name="variableName" id="variableName" value={variable.name}/>
              </FormEntry>

              <FormEntry fieldId="variableTypeName" label="Type" hint="The type of the variable.">
                <select id="variableTypeName" name="variableTypeName">
                  {Object.keys(groupedVariableTypes).map(category => {
                    const variableTypes = groupedVariableTypes[category] as Array<VariableDescriptor>;
                    return (<optgroup label={category}>
                      {variableTypes.map(descriptor => <option value={descriptor.typeName} selected={descriptor.typeName == variableTypeName}>{descriptor.displayName}</option>)}
                    </optgroup>);
                  })}
                </select>
              </FormEntry>

              <FormEntry fieldId="variableValue" label="Value" hint="The value of the variable.">
                <input type="text" name="variableValue" id="variableValue" value={variable.value}/>
              </FormEntry>

              <FormEntry fieldId="variableStorageDriverId" label="Storage" hint="The storage to use when persisting the variable.">
                <select id="variableStorageDriverTypeName" name="variableStorageDriverTypeName">
                  {storageDrivers.map(driver => {
                    const value = driver.typeName;
                    const text = driver.displayName;
                    const selected = value == variable.storageDriverTypeName;
                    return <option value={value} selected={selected}>{text}</option>;
                  })}
                </select>
              </FormEntry>

            </div>
          </div>
        </form>
      </div>
    );
  }

  private onSubmit = async (e: Event) => {
    e.preventDefault();
    const form = e.target as HTMLFormElement;
    const variable = this.getVariableInternal(form);
    this.variableChanged.emit(variable);
  };

  private getVariableInternal = (form: HTMLFormElement): Variable => {
    const formData = new FormData(form as HTMLFormElement);
    const name = formData.get('variableName') as string;
    const value = formData.get('variableValue') as string;
    const type = formData.get('variableTypeName') as string;
    const driverTypeName = formData.get('variableStorageDriverTypeName') as string;
    const variable = this.variable;

    variable.name = name;
    variable.typeName = type;
    variable.value = value;
    variable.storageDriverTypeName = isNullOrWhitespace(driverTypeName) ? null : driverTypeName;

    return variable;
  };

}
