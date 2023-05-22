import {Component, h, Prop, Event, EventEmitter, Method} from "@stencil/core";
import {groupBy} from 'lodash';
import {StorageDriverDescriptor, Variable} from "../../../../models";
import {generateIdentity, isNullOrWhitespace} from "../../../../utils";
import descriptorsStore from '../../../../data/descriptors-store';
import {VariableDescriptor} from "../../../../services/api-client/variable-descriptors-api";
import {CheckboxFormEntry, FormEntry} from "../../../../components/shared/forms/form-entry";

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
    const variable: Variable = this.variable ?? {id: '', name: '', typeName: 'Object', isArray: false};
    const variableTypeName = variable.typeName;
    const availableTypes: Array<VariableDescriptor> = descriptorsStore.variableDescriptors;
    const groupedVariableTypes = groupBy(availableTypes, x => x.category);
    const storageDrivers: Array<StorageDriverDescriptor> = descriptorsStore.storageDrivers;

    return (
      <div>
        <form ref={el => this.formElement = el} class="tw-h-full tw-flex tw-flex-col tw-bg-white" onSubmit={e => this.onSubmit(e)} method="post">
          <div class="tw-pt-4">
            <h2 class="tw-text-lg tw-font-medium tw-ml-4 tw-mb-2">Edit Variable</h2>
            <div class="tw-align-middle tw-inline-block tw-min-w-full tw-border-b tw-border-gray-200">

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

              <CheckboxFormEntry fieldId="variableIsArray" label="This variable is an array" hint="Check if the variable holds an array of the selected type.">
                <input type="checkbox" name="variableIsArray" id="variableIsArray" value="true" checked={variable.isArray}/>
              </CheckboxFormEntry>

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
    const isArray = formData.get('variableIsArray') as string == 'true';
    const driverTypeName = formData.get('variableStorageDriverTypeName') as string;
    const variable = this.variable;

    if (isNullOrWhitespace(variable.id))
      variable.id = generateIdentity();

    variable.name = name;
    variable.typeName = type;
    variable.value = value;
    variable.isArray = isArray;
    variable.storageDriverTypeName = isNullOrWhitespace(driverTypeName) ? null : driverTypeName;

    return variable;
  };

}
