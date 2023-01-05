import {Component, Event, EventEmitter, h, Prop, State, Watch} from "@stencil/core";
import {DeleteIcon, EditIcon} from "../../icons/tooling";
import {StorageDriverDescriptor, Variable, WorkflowInstance} from "../../../models";
import descriptorsStore from "../../../data/descriptors-store";
import {ModalActionClickArgs, ModalActionDefinition, ModalActionType, ModalDialogInstance, ModalDialogService} from "../../shared/modal-dialog";
import {Container} from "typedi";
import {WorkflowDefinition} from "../../../modules/workflow-definitions/models/entities";

@Component({
  tag: 'elsa-variables-viewer',
  shadow: false
})
export class VariablesViewer {

  @Prop() variables?: Array<Variable>;
  @Prop() workflowDefinition: WorkflowDefinition;
  @Prop() workflowInstance: WorkflowInstance;

  render() {
    const variables = this.variables;
    const storageDrivers: Array<StorageDriverDescriptor> = descriptorsStore.storageDrivers;

    return (
      <div>
        <div class="align-middle inline-block min-w-full border-b border-gray-200">
          <table class="default-table">
            <thead>
            <tr>
              <th scope="col">Name</th>
              <th scope="col">Type</th>
              <th scope="col">Storage</th>
              <th scope="col">Value</th>
            </tr>
            </thead>
            <tbody>
            {variables.map(variable => {
                const storage = storageDrivers.find(x => x.typeName == variable.storageDriverTypeName);
                const storageName = storage?.displayName ?? '-';
                const descriptor = descriptorsStore.variableDescriptors.find(x => x.typeName == variable.typeName);
                const typeDisplayName = descriptor?.displayName ?? variable.typeName;
                const variableValue = this.getVariableValue(variable, storage);

                return (
                  <tr>
                    <td class="whitespace-nowrap">{variable.name}</td>
                    <td class="whitespace-nowrap">{typeDisplayName}</td>
                    <td>{storageName}</td>
                    <td class="pr-6">{variableValue}</td>
                  </tr>);
              }
            )}
            </tbody>
          </table>
        </div>
      </div>
    );
  }

  private getVariableValue(variable: Variable, storage: StorageDriverDescriptor) : any {
    if(storage.typeName !== 'Elsa.Workflows.Core.Implementations.WorkflowStorageDriver, Elsa.Workflows.Core')
      return null;

    const workflowInstance = this.workflowInstance;
    const persistentVariables = workflowInstance.properties.PersistentVariablesDictionary;
    const key = `${workflowInstance.id}:Workflow1:${variable.name}`;
    return persistentVariables[key];
  }
}
