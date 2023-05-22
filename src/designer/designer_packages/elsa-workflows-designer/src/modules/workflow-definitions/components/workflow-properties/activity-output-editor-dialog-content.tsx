import {Component, h, Prop, Event, EventEmitter, Method} from "@stencil/core";
import {groupBy} from 'lodash';
import descriptorsStore from '../../../../data/descriptors-store';
import {VariableDescriptor} from "../../../../services/api-client/variable-descriptors-api";
import {InputDefinition, OutputDefinition} from "../../models/entities";
import {FormEntry} from "../../../../components/shared/forms/form-entry";

@Component({
  tag: 'elsa-activity-output-editor-dialog-content',
  shadow: false
})
export class ActivityOutputEditorDialogContent {
  private formElement: HTMLFormElement;

  @Prop() output: OutputDefinition;
  @Event() outputChanged: EventEmitter<OutputDefinition>;

  @Method()
  async getOutput(): Promise<OutputDefinition> {
    return this.getOutputInternal(this.formElement);
  }

  render() {
    const output: OutputDefinition = this.output ?? {name: '', type: 'Object', isArray: false};
    const outputTypeName = output.type;
    const availableTypes: Array<VariableDescriptor> = descriptorsStore.variableDescriptors;
    const groupedTypes = groupBy(availableTypes, x => x.category);

    return (
      <div>
        <form ref={el => this.formElement = el} class="tw-h-full tw-flex tw-flex-col tw-bg-white" onSubmit={e => this.onSubmit(e)} method="post">
          <div class="tw-pt-4">
            <h2 class="tw-text-lg tw-font-medium tw-ml-4 tw-mb-2">Edit output definition</h2>
            <div class="tw-align-middle tw-inline-block tw-min-w-full tw-border-b tw-border-gray-200">

              <FormEntry fieldId="outputName" label="Name" hint="The technical name of the output.">
                <input type="text" name="outputName" id="outputName" value={output.name}/>
              </FormEntry>

              <FormEntry fieldId="outputTypeName" label="Type" hint="The type of the output.">
                <select id="outputTypeName" name="outputTypeName">
                  {Object.keys(groupedTypes).map(category => {
                    const types = groupedTypes[category] as Array<VariableDescriptor>;
                    return (<optgroup label={category}>
                      {types.map(descriptor => <option value={descriptor.typeName} selected={descriptor.typeName == outputTypeName}>{descriptor.displayName}</option>)}
                    </optgroup>);
                  })}
                </select>
              </FormEntry>

              <FormEntry fieldId="outputDisplayName" label="Display name" hint="The user friendly display name of the output.">
                <input type="text" name="outputDisplayName" id="outputDisplayName" value={output.displayName}/>
              </FormEntry>

              <FormEntry fieldId="outputDescription" label="Description" hint="A description of the output.">
                <input type="text" name="outputDescription" id="outputDescription" value={output.description}/>
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
    const output = this.getOutputInternal(form);
    this.outputChanged.emit(output);
  };

  private getOutputInternal = (form: HTMLFormElement): OutputDefinition => {
    const formData = new FormData(form as HTMLFormElement);
    const name = formData.get('outputName') as string;
    const displayName = formData.get('outputDisplayName') as string;
    const type = formData.get('outputTypeName') as string;
    const description = formData.get('outputDescription') as string;
    const output = this.output;

    output.name = name;
    output.type = type;
    output.displayName = displayName;
    output.description = description;

    return output;
  };

}
