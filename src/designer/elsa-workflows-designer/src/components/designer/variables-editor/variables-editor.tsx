import {Component, h} from "@stencil/core";
import {DeleteIcon, EditIcon} from "../../icons/tooling";

@Component({
  tag: 'elsa-variables-editor',
  shadow: false
})
export class VariablesEditor {
  render() {
    return (
      <div>
        <div class="flex justify-end m-4">
          <button class="btn btn-primary">Add variable</button>
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
            <tr>
              <td class="whitespace-nowrap">CurrentValue</td>
              <td class="whitespace-nowrap">String</td>
              <td></td>
              <td class="pr-6">
                <elsa-context-menu
                  menuItems={[
                    {text: 'Edit', clickHandler: e => this.onEditClick(e), icon: <EditIcon/>},
                    {text: 'Delete', clickHandler: e => this.onDeleteClick(e), icon: <DeleteIcon/>},
                  ]}
                />
              </td>
            </tr>
            </tbody>
          </table>
        </div>
      </div>
    );
  }

  private onEditClick = (e: Event) => {
    e.preventDefault();
  };

  private onDeleteClick = (e: Event) => {
    e.preventDefault();
  };
}
