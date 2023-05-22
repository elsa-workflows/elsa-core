import {Component, h, Prop, Event, EventEmitter, Method} from "@stencil/core";
import {groupBy} from 'lodash';
import descriptorsStore from '../../../../data/descriptors-store';
import {VariableDescriptor} from "../../../../services/api-client/variable-descriptors-api";
import {InputDefinition} from "../../models/entities";
import {CheckboxFormEntry, FormEntry} from "../../../../components/shared/forms/form-entry";
import {StorageDriverDescriptor} from "../../../../models";
import {isNullOrWhitespace} from "../../../../utils";

@Component({
  tag: 'elsa-activity-input-editor-dialog-content',
  shadow: false
})
export class ActivityInputEditorDialogContent {
  private formElement: HTMLFormElement;

  @Prop() input: InputDefinition;
  @Event() inputChanged: EventEmitter<InputDefinition>;

  @Method()
  async getInput(): Promise<InputDefinition> {
    return this.getInputInternal(this.formElement);
  }

  render() {
    const input: InputDefinition = this.input ?? {name: '', type: 'Object', isArray: false};
    const inputTypeName = input.type;
    const availableTypes: Array<VariableDescriptor> = descriptorsStore.variableDescriptors;
    const storageDrivers: Array<StorageDriverDescriptor> = descriptorsStore.storageDrivers;
    const groupedTypes = groupBy(availableTypes, x => x.category);
    const selectedUIHint = input.uiHint;

    // TODO: Get this from configuration (API).
    const uiHints = [{
      name: 'Single line',
      value: 'single-line'
    }, {
      name: 'Multi line',
      value: 'multi-line'
    }, {
      name: 'Checkbox',
      value: 'checkbox'
    }, {
      name: 'Check list',
      value: 'check-list'
    }, {
      name: 'Radio list',
      value: 'radio-list'
    }, {
      name: 'Dropdown',
      value: 'dropdown'
    }, {
      name: 'Multi text',
      value: 'multi-text'
    }, {
      name: 'Code editor',
      value: 'code-editor'
    }, {
      name: 'Variable picker',
      value: 'variable-picker'
    }, {
      name: 'Type picker',
      value: 'type-picker'
    }
    ];

    return (
      <div>
        <form ref={el => this.formElement = el} class="tw-h-full tw-flex tw-flex-col tw-bg-white" onSubmit={e => this.onSubmit(e)} method="post">
          <div class="tw-pt-4">
            <h2 class="tw-text-lg tw-font-medium tw-ml-4 tw-mb-2">Edit input definition</h2>
            <div class="tw-align-middle tw-inline-block tw-min-w-full tw-border-b tw-border-gray-200">

              <FormEntry fieldId="inputName" label="Name" hint="The technical name of the input.">
                <input type="text" name="inputName" id="inputName" value={input.name}/>
              </FormEntry>

              <FormEntry fieldId="inputTypeName" label="Type" hint="The type of the input.">
                <select id="inputTypeName" name="inputTypeName">
                  {Object.keys(groupedTypes).map(category => {
                    const types = groupedTypes[category] as Array<VariableDescriptor>;
                    return (<optgroup label={category}>
                      {types.map(descriptor => <option value={descriptor.typeName} selected={descriptor.typeName == inputTypeName}>{descriptor.displayName}</option>)}
                    </optgroup>);
                  })}
                </select>
              </FormEntry>

              <CheckboxFormEntry fieldId="inputIsArray" label="This input is an array" hint="Check if the input holds an array of the selected type.">
                <input type="checkbox" name="inputIsArray" id="inputIsArray" value="true" checked={input.isArray}/>
              </CheckboxFormEntry>

              <FormEntry fieldId="inputDisplayName" label="Display name" hint="The user friendly display name of the input.">
                <input type="text" name="inputDisplayName" id="inputDisplayName" value={input.displayName}/>
              </FormEntry>

              <FormEntry fieldId="inputDescription" label="Description" hint="A description of the input.">
                <input type="text" name="inputDescription" id="inputDescription" value={input.description}/>
              </FormEntry>

              <FormEntry fieldId="inputCategory" label="Category" hint="A custom category.">
                <input type="text" name="inputCategory" id="inputCategory" value={input.category}/>
              </FormEntry>

              <FormEntry fieldId="inputUIHint" label="Control" hint="The control to use for this input.">
                <select name="inputUIHint" id="inputUIHint">
                  {uiHints.map(uiHint => {
                    const isSelected = uiHint.value == selectedUIHint;
                    return <option value={uiHint.value} selected={isSelected}>{uiHint.name}</option>
                  })}
                </select>
              </FormEntry>

              <FormEntry fieldId="inputStorageDriverType" label="Storage" hint="The storage to use when persisting the input.">
                <select id="inputStorageDriverType" name="inputStorageDriverType">
                  {storageDrivers.map(driver => {
                    const value = driver.typeName;
                    const text = driver.displayName;
                    const selected = value == input.storageDriverType;
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
    const input = this.getInputInternal(form);
    this.inputChanged.emit(input);
  };

  private getInputInternal = (form: HTMLFormElement): InputDefinition => {
    const formData = new FormData(form as HTMLFormElement);
    const name = formData.get('inputName') as string;
    const displayName = formData.get('inputDisplayName') as string;
    const type = formData.get('inputTypeName') as string;
    const description = formData.get('inputDescription') as string;
    const category = formData.get('inputCategory') as string;
    const uiHint = formData.get('inputUIHint') as string;
    const isArray = formData.get('inputIsArray') as string === 'true';
    const driverType = formData.get('inputStorageDriverType') as string;
    const input = this.input;

    input.name = name;
    input.type = type;
    input.displayName = displayName;
    input.category = category;
    input.description = description;
    input.uiHint = uiHint;
    input.isArray = isArray;
    input.storageDriverType = isNullOrWhitespace(driverType) ? null : driverType;

    return input;
  };

}
