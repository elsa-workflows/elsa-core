import {Component, h, Prop, Event, EventEmitter} from "@stencil/core";
import {DeleteIcon, EditIcon} from "../../icons/tooling";
import {Variable} from "../../../models";

@Component({
  tag: 'elsa-variables-editor',
  shadow: false
})
export class VariablesEditor {
  private variableEditorDialog: HTMLElsaVariableEditorDialogElement;
  @Prop() variables: Array<Variable> = [];
  @Event() variablesChanged: EventEmitter<Array<Variable>>;

  render() {
    const variables = this.variables;

    return (
      <div>
        <div class="flex justify-end m-4">
          <button class="btn btn-primary" onClick={e => this.onAddVariableClick()}>Add variable</button>
        </div>
        <div class="align-middle inline-block min-w-full border-b border-gray-200">
          <table>
            <thead>
            <tr>
              <th scope="col">Name</th>
              <th scope="col">Type</th>
              <th scope="col">Value</th>
              <th scope="col"/>
            </tr>
            </thead>
            <tbody>
            {variables.map(variable => {
                return (
                  <tr>
                    <td class="whitespace-nowrap">{variable.name}</td>
                    <td class="whitespace-nowrap">{variable.type}</td>
                    <td>{variable.defaultValue}</td>
                    <td class="pr-6">
                      <elsa-context-menu
                        menuItems={[
                          {text: 'Edit', clickHandler: e => this.onEditClick(e), icon: <EditIcon/>},
                          {text: 'Delete', clickHandler: e => this.onDeleteClick(e), icon: <DeleteIcon/>},
                        ]}
                      />
                    </td>
                  </tr>);
              }
            )}
            </tbody>
          </table>
        </div>
        <elsa-variable-editor-dialog ref={el => this.variableEditorDialog = el} />
      </div>
    );
  }

  private onAddVariableClick = async () => {
    await this.variableEditorDialog.show();
  };

  private onEditClick = (e: Event) => {
    e.preventDefault();
  };

  private onDeleteClick = (e: Event) => {
    e.preventDefault();
  };
}
